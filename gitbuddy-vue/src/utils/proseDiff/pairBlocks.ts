import { computeInlineDiff } from '../diffHelpers';
import { normalizeForCompare } from './classifyChange';
import { pathKey } from './parseBlocks';
import type { Block, BlockDoc, PairedBlock } from './types';

/**
 * Matches blocks between two revisions by section path and position rather than by line
 * number. This is the whole point of the feature: a paragraph that was only re-wrapped pairs
 * with its old self and reports no change, where a line diff reports every touched line.
 */

/** Below this similarity, two blocks are treated as unrelated (add + remove, not a change). */
const SIMILARITY_THRESHOLD = 0.4;

/** Only consider candidates within this many positions — bounds cost on large documents. */
const POSITION_WINDOW = 6;

export function pairBlocks(oldDoc: BlockDoc, newDoc: BlockDoc): PairedBlock[] {
  const oldBySection = groupBySection(oldDoc.blocks);
  const newBySection = groupBySection(newDoc.blocks);

  const emitted = new Set<string>();
  const result: PairedBlock[] = [];

  const oldKeyOrder = [...oldBySection.keys()];

  for (const key of newBySection.keys()) {
    // Splice in any old-only sections that preceded this one in the old document,
    // so removed sections appear where they used to live.
    const oldIndex = oldKeyOrder.indexOf(key);
    if (oldIndex >= 0) {
      for (const priorKey of oldKeyOrder.slice(0, oldIndex)) {
        if (emitted.has(priorKey) || newBySection.has(priorKey)) continue;
        result.push(...allRemoved(oldBySection.get(priorKey)!));
        emitted.add(priorKey);
      }
    }

    const oldBlocks = oldBySection.get(key) ?? [];
    result.push(...pairWithinSection(oldBlocks, newBySection.get(key)!));
    emitted.add(key);
  }

  // Old-only sections that came after everything matched.
  for (const key of oldKeyOrder) {
    if (emitted.has(key) || newBySection.has(key)) continue;
    result.push(...allRemoved(oldBySection.get(key)!));
    emitted.add(key);
  }

  return result;
}

/** Fraction of blocks that found a partner. Consumers use it to warn when pairing was poor. */
export function pairingQuality(pairs: PairedBlock[]): number {
  if (pairs.length === 0) return 1;
  const matched = pairs.filter(p => p.old && p.new).length;
  return matched / pairs.length;
}

function groupBySection(blocks: Block[]): Map<string, Block[]> {
  const map = new Map<string, Block[]>();
  for (const block of blocks) {
    const key = pathKey(block.sectionPath);
    const bucket = map.get(key);
    if (bucket) bucket.push(block);
    else map.set(key, [block]);
  }
  return map;
}

function allRemoved(blocks: Block[]): PairedBlock[] {
  return blocks.map(block => ({ old: block, status: 'removed' as const }));
}

function pairWithinSection(oldBlocks: Block[], newBlocks: Block[]): PairedBlock[] {
  const partnerOf = new Map<number, Block>(); // new block id -> old block
  const usedOld = new Set<number>();

  // Phase A — exact normalized matches, earliest unmatched old block first so repeated
  // identical blocks pair in document order instead of all collapsing onto the first.
  const normalizedOld = oldBlocks.map(normalizeForCompare);
  newBlocks.forEach((newBlock, newIdx) => {
    const target = normalizeForCompare(newBlock);
    for (let i = 0; i < oldBlocks.length; i++) {
      const candidate = oldBlocks[i];
      if (!candidate) continue;
      if (usedOld.has(candidate.id)) continue;
      if (candidate.kind !== newBlock.kind) continue;
      if (normalizedOld[i] !== target) continue;
      if (Math.abs(i - newIdx) > POSITION_WINDOW * 2) continue;
      partnerOf.set(newBlock.id, candidate);
      usedOld.add(candidate.id);
      return;
    }
  });

  // Phase B — best similarity among same-kind, nearby, still-unmatched candidates.
  newBlocks.forEach((newBlock, newIdx) => {
    if (partnerOf.has(newBlock.id)) return;

    let best: { block: Block; score: number } | null = null;
    for (let i = 0; i < oldBlocks.length; i++) {
      const candidate = oldBlocks[i];
      if (!candidate) continue;
      if (usedOld.has(candidate.id)) continue;
      if (candidate.kind !== newBlock.kind) continue;
      if (Math.abs(i - newIdx) > POSITION_WINDOW) continue;

      const score = similarity(candidate.text || candidate.raw, newBlock.text || newBlock.raw);
      if (score >= SIMILARITY_THRESHOLD && (!best || score > best.score)) {
        best = { block: candidate, score };
      }
    }

    if (best) {
      partnerOf.set(newBlock.id, best.block);
      usedOld.add(best.block.id);
    }
  });

  // Emit following the new document, splicing removed blocks in at their old position.
  const pairs: PairedBlock[] = [];
  let oldCursor = 0;

  for (const newBlock of newBlocks) {
    const partner = partnerOf.get(newBlock.id);

    if (partner) {
      const partnerIdx = oldBlocks.indexOf(partner);
      // Everything skipped over on the old side is gone — emit it before the survivor.
      for (let i = oldCursor; i < partnerIdx; i++) {
        const skipped = oldBlocks[i];
        if (skipped && !usedOld.has(skipped.id)) {
          pairs.push({ old: skipped, status: 'removed' });
          usedOld.add(skipped.id);
        }
      }
      oldCursor = partnerIdx + 1;
      pairs.push({
        old: partner,
        new: newBlock,
        status: partner.raw === newBlock.raw ? 'unchanged' : 'changed',
      });
    } else {
      pairs.push({ new: newBlock, status: 'added' });
    }
  }

  // Trailing removals.
  for (let i = oldCursor; i < oldBlocks.length; i++) {
    const trailing = oldBlocks[i];
    if (trailing && !usedOld.has(trailing.id)) {
      pairs.push({ old: trailing, status: 'removed' });
    }
  }

  return pairs;
}

/** Ratio of text common to both sides, via the existing word-level differ. */
function similarity(a: string, b: string): number {
  if (!a && !b) return 1;
  const longest = Math.max(a.length, b.length);
  if (longest === 0) return 1;

  const { newSegments } = computeInlineDiff(a, b);
  const equal = newSegments
    .filter(s => s.type === 'equal')
    .reduce((n, s) => n + s.content.length, 0);

  return equal / longest;
}
