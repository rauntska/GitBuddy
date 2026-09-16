import type { Block, ChangeClass, PairedBlock } from './types';

/**
 * Change classification and formatting-only suppression.
 *
 * Conservative by construction: `classify` only returns 'formatting-only' for a change it
 * positively recognises as semantically empty. Everything else is 'content'. Misclassifying a
 * real edit as noise would hide it from a reviewer, which is strictly worse than the noise.
 */

const PROSE_KINDS = new Set<Block['kind']>(['paragraph', 'heading', 'listItem', 'blockquote']);

/**
 * Collapses the differences that carry no meaning: hard-wrap position, repeated internal
 * whitespace, and trailing whitespace. Literal blocks (code, tables, html) keep their line
 * structure — a re-wrapped code block is a real change.
 */
export function normalizeForCompare(block: Block): string {
  const lines = block.raw.split('\n').map(l => l.replace(/[ \t]+$/, ''));

  if (!PROSE_KINDS.has(block.kind)) {
    return lines.join('\n').trim();
  }

  return lines.join(' ').replace(/\s+/g, ' ').trim();
}

export function classify(pair: PairedBlock): ChangeClass {
  if (!pair.old || !pair.new) return 'content';
  if (pair.old.raw === pair.new.raw) return 'unchanged';

  if (normalizeForCompare(pair.old) === normalizeForCompare(pair.new)) {
    return 'formatting-only';
  }
  if (isOrderedListRenumber(pair.old, pair.new)) return 'formatting-only';
  if (isReferenceLinkReorder(pair.old, pair.new)) return 'formatting-only';

  return 'content';
}

/** Human-readable reason shown in the per-section suppression disclosure. */
export function describeSuppressed(pair: PairedBlock): string {
  if (!pair.old || !pair.new) return '';

  const range =
    pair.new.startLine === pair.new.endLine
      ? `L${pair.new.startLine}`
      : `L${pair.new.startLine}–${pair.new.endLine}`;

  if (isOrderedListRenumber(pair.old, pair.new)) {
    return `${range} · ordered list renumbered, item text unchanged`;
  }
  if (isReferenceLinkReorder(pair.old, pair.new)) {
    return `${range} · reference links reordered, targets unchanged`;
  }
  if (trailingWhitespaceOnly(pair.old, pair.new)) {
    return `${range} · trailing whitespace removed`;
  }
  return `${range} · ${labelFor(pair.new.kind)} re-wrapped, text identical`;
}

function labelFor(kind: Block['kind']): string {
  switch (kind) {
    case 'listItem': return 'list item';
    case 'heading': return 'heading';
    case 'blockquote': return 'quote';
    default: return 'paragraph';
  }
}

function trailingWhitespaceOnly(oldBlock: Block, newBlock: Block): boolean {
  // Also normalises trailing blank lines: a block that gained or lost a following blank line
  // (common when a list item becomes the last one) is a whitespace change, not a re-wrap.
  const strip = (s: string) =>
    s.split('\n').map(l => l.replace(/[ \t]+$/, '')).join('\n').replace(/\n+$/, '');
  return strip(oldBlock.raw) === strip(newBlock.raw);
}

const ORDERED_ITEM = /^(\s*)(\d+)([.)]\s+)([\s\S]*)$/;

function isOrderedListRenumber(oldBlock: Block, newBlock: Block): boolean {
  if (oldBlock.kind !== 'listItem' || newBlock.kind !== 'listItem') return false;

  const a = ORDERED_ITEM.exec(oldBlock.raw);
  const b = ORDERED_ITEM.exec(newBlock.raw);
  if (!a || !b) return false;

  // Same indent, same delimiter style, same content — only the number moved.
  return a[1] === b[1] && a[3] === b[3] && (a[4] ?? '').trimEnd() === (b[4] ?? '').trimEnd();
}

const REFERENCE_DEF = /^\s{0,3}\[[^\]]+\]:\s*\S+/;

function isReferenceLinkReorder(oldBlock: Block, newBlock: Block): boolean {
  const defsOf = (block: Block): string[] | null => {
    const lines = block.raw.split('\n').map(l => l.trim()).filter(Boolean);
    if (lines.length === 0) return null;
    if (!lines.every(l => REFERENCE_DEF.test(l))) return null;
    return [...lines].sort();
  };

  const a = defsOf(oldBlock);
  const b = defsOf(newBlock);
  if (!a || !b || a.length !== b.length) return false;

  return a.every((line, i) => line === b[i]);
}
