import { marked } from 'marked';
import { computeInlineDiff } from '../diffHelpers';
import type { ClassifiedPair, PairedBlock } from './types';

/**
 * Renders a paired block to HTML.
 *
 * Word-level marking uses the existing `computeInlineDiff`. It does NOT use
 * `renderInlineDiffSegments`, which escapes HTML and emits `<span>`s — correct for code lines,
 * but it would leave markdown syntax visible as literal text here. Instead the changed runs are
 * wrapped in private-use sentinels, the result is passed through `marked`, and the sentinels
 * are swapped for <ins>/<del> afterwards. Same trick `renderMarkdownDiff.ts` uses for its
 * block markers.
 */

const INS_OPEN = '\uE000';
const INS_CLOSE = '\uE001';
const DEL_OPEN = '\uE002';
const DEL_CLOSE = '\uE003';

marked.setOptions({ breaks: true, gfm: true });

export function renderPairedBlock(pair: ClassifiedPair): string {
  if (pair.status === 'removed' && pair.old) {
    return wrap('prose-block prose-removed', parse(pair.old.raw));
  }
  if (pair.status === 'added' && pair.new) {
    return wrap('prose-block prose-added', parse(pair.new.raw));
  }
  if (!pair.new) return '';

  if (pair.status === 'unchanged' || pair.changeClass !== 'content') {
    return wrap('prose-block', parse(pair.new.raw));
  }

  // Changed, and meaningfully so. A block we cannot word-diff (table, code, html) gets a
  // block-level treatment instead — otherwise it would change completely silently, which is
  // exactly what happens to a table gaining a row.
  if (!pair.old || !pair.new.wordDiffable || !pair.old.wordDiffable) {
    return wrap('prose-block prose-changed-opaque', parse(pair.new.raw));
  }

  const wordDiffHtml = renderWordDiff(pair.old.raw, pair.new.raw);
  return wordDiffHtml === null
    ? wrap('prose-block prose-changed-opaque', parse(pair.new.raw))
    : wrap('prose-block prose-changed', wordDiffHtml);
}

/**
 * Returns null when the sentinels did not survive markdown parsing intact — which happens when
 * a diff boundary lands inside inline markup or a code span. Callers fall back to block-level
 * highlighting rather than render broken output.
 */
function renderWordDiff(oldRaw: string, newRaw: string): string | null {
  const segments = mergeSegments(oldRaw, newRaw);

  let source = '';
  let opens = 0;
  for (const seg of segments) {
    if (seg.type === 'equal') {
      source += seg.content;
    } else if (seg.type === 'add') {
      source += `${INS_OPEN}${seg.content}${INS_CLOSE}`;
      opens++;
    } else {
      source += `${DEL_OPEN}${seg.content}${DEL_CLOSE}`;
      opens++;
    }
  }

  if (opens === 0) return null;

  const html = parse(source);

  const survived =
    count(html, INS_OPEN) + count(html, DEL_OPEN) === opens &&
    count(html, INS_CLOSE) + count(html, DEL_CLOSE) === opens;
  if (!survived) return null;

  return html
    .split(INS_OPEN).join('<ins class="prose-ins">')
    .split(INS_CLOSE).join('</ins>')
    .split(DEL_OPEN).join('<del class="prose-del">')
    .split(DEL_CLOSE).join('</del>');
}

type Segment = { type: 'equal' | 'add' | 'delete'; content: string };

/**
 * `computeInlineDiff` returns the old and new sides separately; both carry the same `equal`
 * segments in the same order, so walking them together reconstructs the original op sequence
 * as one stream with deletions and insertions in place.
 */
function mergeSegments(oldRaw: string, newRaw: string): Segment[] {
  return snapToWordBoundaries(rawMergeSegments(oldRaw, newRaw));
}

function rawMergeSegments(oldRaw: string, newRaw: string): Segment[] {
  const { oldSegments, newSegments } = computeInlineDiff(oldRaw, newRaw);
  const merged: Segment[] = [];
  let o = 0;
  let n = 0;

  while (o < oldSegments.length || n < newSegments.length) {
    const oldSeg = oldSegments[o];
    const newSeg = newSegments[n];

    if (oldSeg?.type === 'delete') {
      merged.push({ type: 'delete', content: oldSeg.content });
      o++;
    } else if (newSeg?.type === 'add') {
      merged.push({ type: 'add', content: newSeg.content });
      n++;
    } else if (oldSeg && newSeg) {
      merged.push({ type: 'equal', content: newSeg.content });
      o++;
      n++;
    } else if (oldSeg) {
      merged.push({ type: 'delete', content: oldSeg.content });
      o++;
    } else if (newSeg) {
      merged.push({ type: 'add', content: newSeg.content });
      n++;
    }
  }

  return merged;
}

/**
 * diff-match-patch works on characters, so "three" -> "five" comes back as
 * `del("thre") ins("fiv") equal("e ...")`. Rendering that is harder to read than the change it
 * describes. This widens each run of changes out to whole-word boundaries by pulling the
 * partial word off the neighbouring equal segments.
 *
 * Only applies when the change run actually abuts a word: a run that already starts or ends on
 * whitespace is left alone, so deleting "big " from "a big dog" does not swallow "dog".
 */
function snapToWordBoundaries(segments: Segment[]): Segment[] {
  const out = segments.map(s => ({ ...s }));

  for (let i = 0; i < out.length; i++) {
    const seg = out[i];
    if (!seg || seg.type === 'equal') continue;

    // Find the whole run of adjacent change segments.
    let end = i;
    while (end + 1 < out.length && out[end + 1]!.type !== 'equal') end++;
    const run = out.slice(i, end + 1);

    const prev = out[i - 1];
    if (prev?.type === 'equal' && run.every(s => !/^\s/.test(s.content))) {
      const partial = /(\S+)$/.exec(prev.content)?.[1] ?? '';
      if (partial && partial.length <= MAX_WORD_SNAP) {
        prev.content = prev.content.slice(0, prev.content.length - partial.length);
        for (const s of run) s.content = partial + s.content;
      }
    }

    const next = out[end + 1];
    if (next?.type === 'equal' && run.every(s => !/\s$/.test(s.content))) {
      const partial = /^(\S+)/.exec(next.content)?.[1] ?? '';
      if (partial && partial.length <= MAX_WORD_SNAP) {
        next.content = next.content.slice(partial.length);
        for (const s of run) s.content = s.content + partial;
      }
    }

    i = end;
  }

  return out.filter(s => s.content.length > 0);
}

/** Guard against pathological input with no whitespace (minified lines, long URLs). */
const MAX_WORD_SNAP = 60;

function parse(source: string): string {
  return marked.parse(source) as string;
}

function wrap(className: string, html: string): string {
  return `<div class="${className}">${html}</div>`;
}

function count(haystack: string, needle: string): number {
  return haystack.split(needle).length - 1;
}

/** True when a pair should appear in the document at all (suppressed ones are listed instead). */
export function isRenderable(pair: ClassifiedPair): boolean {
  return pair.changeClass !== 'formatting-only';
}

export function pairKey(pair: PairedBlock): string {
  return `${pair.old?.id ?? 'x'}-${pair.new?.id ?? 'x'}-${pair.status}`;
}
