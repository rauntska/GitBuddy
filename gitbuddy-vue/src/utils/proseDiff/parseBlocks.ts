import { marked } from 'marked';
import type { Block, BlockDoc, BlockKind, Section } from './types';

/**
 * Parses one revision of a markdown file into an ordered block list plus a section index.
 *
 * Uses marked's block lexer rather than regex: it already yields the exact block boundaries
 * pairing needs (headings, paragraphs, list items, fenced code, tables), and marked guarantees
 * that concatenating token `raw` reproduces the source — which is what lets us derive line
 * numbers without a second pass over the text.
 */
export function parseBlocks(source: string): BlockDoc {
  const normalized = source.replace(/\r\n/g, '\n');
  const tokens = marked.lexer(normalized);

  const blocks: Block[] = [];
  const cursor = { line: 1 };
  const headingStack: { depth: number; title: string }[] = [];

  const push = (
    kind: BlockKind,
    raw: string,
    text: string,
    wordDiffable: boolean,
    depth?: number
  ) => {
    const startLine = cursor.line;
    const newlines = countNewlines(raw);
    cursor.line += newlines;
    const endLine = raw.endsWith('\n') ? Math.max(startLine, cursor.line - 1) : cursor.line;

    if (kind === 'heading' && depth !== undefined) {
      let top = headingStack[headingStack.length - 1];
      while (top && top.depth >= depth) {
        headingStack.pop();
        top = headingStack[headingStack.length - 1];
      }
      headingStack.push({ depth, title: text });
    }

    // Blank space between blocks carries no meaning for a prose diff.
    if (kind === 'other' && raw.trim() === '') return;

    blocks.push({
      id: blocks.length,
      kind,
      depth,
      raw,
      text,
      startLine,
      endLine,
      sectionPath: headingStack.map(h => h.title),
      wordDiffable,
    });
  };

  for (const token of tokens as any[]) {
    switch (token.type) {
      case 'heading':
        push('heading', token.raw, token.text ?? '', true, token.depth);
        break;

      case 'paragraph':
        push('paragraph', token.raw, token.text ?? '', true);
        break;

      case 'blockquote':
        push('blockquote', token.raw, token.text ?? '', true);
        break;

      case 'code':
        // Includes ```mermaid — captured whole and never word-diffed.
        push('code', token.raw, token.text ?? '', false);
        break;

      case 'table':
        push('table', token.raw, token.raw, false);
        break;

      case 'html':
        push('html', token.raw, token.raw, false);
        break;

      case 'list':
        // One block per item: adding a bullet should report one changed block,
        // not a wholly changed list.
        for (const item of token.items ?? []) {
          push('listItem', item.raw, item.text ?? '', true);
        }
        // Absorb any list-level raw the items did not account for (loose-list spacing).
        reconcile(cursor, token.raw, token.items ?? []);
        break;

      default:
        push('other', token.raw ?? '', token.raw ?? '', false);
        break;
    }
  }

  const sections = buildSections(blocks);
  return { blocks, sections, mapDepth: pickMapDepth(sections) };
}

/**
 * List item raws can sum to less than the list's own raw (blank lines between loose items).
 * Nudge the line cursor forward by the difference so subsequent blocks stay aligned.
 */
function reconcile(cursor: { line: number }, listRaw: string, items: { raw: string }[]): void {
  const itemNewlines = items.reduce((n, i) => n + countNewlines(i.raw ?? ''), 0);
  const delta = countNewlines(listRaw) - itemNewlines;
  if (delta > 0) cursor.line += delta;
}

function countNewlines(text: string): number {
  let n = 0;
  for (let i = 0; i < text.length; i++) if (text.charCodeAt(i) === 10) n++;
  return n;
}

function buildSections(blocks: Block[]): Section[] {
  const sections: Section[] = [];

  for (const block of blocks) {
    if (block.kind !== 'heading' || block.depth === undefined) continue;
    sections.push({
      path: block.sectionPath,
      title: block.text,
      depth: block.depth,
      headingBlockId: block.id,
      blockIds: [],
    });
  }

  // Assign every non-heading block to the innermost section it sits under.
  for (const block of blocks) {
    if (block.kind === 'heading') continue;
    const owner = sections.find(s => samePath(s.path, block.sectionPath));
    owner?.blockIds.push(block.id);
  }

  return sections;
}

/**
 * The shallowest heading depth that appears more than once. A document with a single `# Title`
 * followed by many `## Section`s maps at depth 2, so the title does not swallow everything.
 */
function pickMapDepth(sections: Section[]): number {
  if (sections.length === 0) return 1;
  const counts = new Map<number, number>();
  for (const s of sections) counts.set(s.depth, (counts.get(s.depth) ?? 0) + 1);

  const repeated = [...counts.entries()]
    .filter(([, count]) => count > 1)
    .map(([depth]) => depth)
    .sort((a, b) => a - b);

  const shallowestRepeated = repeated[0];
  if (shallowestRepeated !== undefined) return shallowestRepeated;
  return Math.min(...counts.keys());
}

export function samePath(a: string[], b: string[]): boolean {
  return a.length === b.length && a.every((v, i) => v === b[i]);
}

export function pathKey(path: string[]): string {
  return path.join(' › ');
}
