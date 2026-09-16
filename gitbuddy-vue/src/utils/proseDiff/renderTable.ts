import { marked } from 'marked';
import { computeInlineDiff } from '../diffHelpers';

/**
 * Row-level diffing for markdown tables.
 *
 * `marked` parses a whole table as one block token, so without this a table that gained a
 * single row reports only "this table changed" — useless for a docs index that is mostly one
 * long table. Here the table is split into rows, rows are paired the same way blocks are, and
 * the table is re-emitted with per-row markers that become classes on the `<tr>`.
 *
 * Rendering has to go through the full table: a lone `| a | b |` line is not a table, so rows
 * cannot be parsed individually.
 */

const ROW_ADDED = '\uE010';
const ROW_REMOVED = '\uE011';
const ROW_CHANGED = '\uE012';

const MARKERS: Record<string, string> = {
  [ROW_ADDED]: 'prose-row-added',
  [ROW_REMOVED]: 'prose-row-removed',
  [ROW_CHANGED]: 'prose-row-changed',
};

const DELIMITER = /^\s{0,3}\|[\s:|-]+\|\s*$/;
const SIMILARITY_THRESHOLD = 0.5;

interface ParsedTable {
  header: string;
  delimiter: string;
  body: string[];
}

/** Returns null when either side isn't a well-formed table; the caller falls back. */
export function renderTableDiff(oldRaw: string, newRaw: string): string | null {
  const oldTable = parseTable(oldRaw);
  const newTable = parseTable(newRaw);
  if (!oldTable || !newTable) return null;

  const rows = pairRows(oldTable.body, newTable.body);

  const lines = [
    oldTable.header === newTable.header ? newTable.header : mark(newTable.header, ROW_CHANGED),
    newTable.delimiter,
    ...rows.map(row => {
      if (row.status === 'added') return mark(row.text, ROW_ADDED);
      if (row.status === 'removed') return mark(row.text, ROW_REMOVED);
      if (row.status === 'changed') return mark(row.text, ROW_CHANGED);
      return row.text;
    }),
  ];

  return decorate(marked.parse(lines.join('\n')) as string);
}

function parseTable(raw: string): ParsedTable | null {
  const lines = raw.split('\n').map(l => l.trimEnd()).filter(l => l.trim().length > 0);
  if (lines.length < 2) return null;

  const [header, delimiter, ...body] = lines;
  if (!header || !delimiter || !DELIMITER.test(delimiter)) return null;

  return { header, delimiter, body };
}

/** The marker must sit inside the last cell — after the closing pipe the parser drops it. */
function mark(line: string, marker: string): string {
  const lastPipe = line.lastIndexOf('|');
  if (lastPipe < 0) return line + marker;
  return line.slice(0, lastPipe) + marker + line.slice(lastPipe);
}

type RowStatus = 'added' | 'removed' | 'changed' | 'unchanged';

function pairRows(oldRows: string[], newRows: string[]): { text: string; status: RowStatus }[] {
  const partnerOf = new Map<number, number>(); // new index -> old index
  const usedOld = new Set<number>();

  // Exact matches first, so repeated identical rows pair in document order.
  newRows.forEach((newRow, n) => {
    const target = normalize(newRow);
    for (let o = 0; o < oldRows.length; o++) {
      if (usedOld.has(o)) continue;
      if (normalize(oldRows[o] ?? '') !== target) continue;
      partnerOf.set(n, o);
      usedOld.add(o);
      return;
    }
  });

  // Then best-similarity matches for rows that were edited rather than inserted.
  newRows.forEach((newRow, n) => {
    if (partnerOf.has(n)) return;
    let best: { index: number; score: number } | null = null;
    for (let o = 0; o < oldRows.length; o++) {
      if (usedOld.has(o)) continue;
      const score = similarity(oldRows[o] ?? '', newRow);
      if (score >= SIMILARITY_THRESHOLD && (!best || score > best.score)) {
        best = { index: o, score };
      }
    }
    if (best) {
      partnerOf.set(n, best.index);
      usedOld.add(best.index);
    }
  });

  // Emit following the new table, splicing removed rows back at their old position.
  const out: { text: string; status: RowStatus }[] = [];
  const emittedOld = new Set<number>();
  let cursor = 0;

  newRows.forEach((newRow, n) => {
    const partner = partnerOf.get(n);
    if (partner !== undefined) {
      for (let o = cursor; o < partner; o++) {
        if (!usedOld.has(o) && !emittedOld.has(o)) {
          out.push({ text: oldRows[o] ?? '', status: 'removed' });
          emittedOld.add(o);
        }
      }
      cursor = partner + 1;
      out.push({
        text: newRow,
        status: normalize(oldRows[partner] ?? '') === normalize(newRow) ? 'unchanged' : 'changed',
      });
    } else {
      out.push({ text: newRow, status: 'added' });
    }
  });

  for (let o = cursor; o < oldRows.length; o++) {
    if (!usedOld.has(o) && !emittedOld.has(o)) {
      out.push({ text: oldRows[o] ?? '', status: 'removed' });
    }
  }

  return out;
}

function normalize(row: string): string {
  return row.replace(/\s+/g, ' ').trim();
}

function similarity(a: string, b: string): number {
  const longest = Math.max(a.length, b.length);
  if (longest === 0) return 1;
  const { newSegments } = computeInlineDiff(a, b);
  const equal = newSegments
    .filter(s => s.type === 'equal')
    .reduce((n, s) => n + s.content.length, 0);
  return equal / longest;
}

/** Moves each row marker onto its `<tr>` as a class, then strips the marker from the text. */
function decorate(html: string): string {
  const doc = new DOMParser().parseFromString(`<div id="__t">${html}</div>`, 'text/html');
  const root = doc.getElementById('__t');
  if (!root) return html;

  const walker = doc.createTreeWalker(root, NodeFilter.SHOW_TEXT);
  const hits: { node: Text; className: string }[] = [];

  while (walker.nextNode()) {
    const node = walker.currentNode as Text;
    for (const [marker, className] of Object.entries(MARKERS)) {
      if (node.data.includes(marker)) hits.push({ node, className });
    }
  }

  for (const hit of hits) {
    hit.node.data = hit.node.data.replace(/[\uE010\uE011\uE012]/g, '');
    hit.node.parentElement?.closest('tr')?.classList.add(hit.className);
  }

  return root.innerHTML;
}
