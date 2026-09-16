import { marked } from 'marked';

marked.setOptions({ breaks: true, gfm: true });

const ADDED_MARKER = '<span data-gb-added="1"></span>';
const ADDED_SELECTOR = 'span[data-gb-added="1"]';

const BLOCK_TAGS = new Set([
  'P', 'LI', 'PRE', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6',
  'BLOCKQUOTE', 'TR', 'TD', 'TH', 'DIV', 'SECTION', 'ARTICLE', 'DD', 'DT',
]);

export function renderMarkdownWithAddedHighlights(
  text: string,
  addedLineNumbers: Set<number>
): string {
  const normalized = text.replace(/\r\n/g, '\n');
  const tagged = tagAddedLines(normalized, addedLineNumbers);
  const renderedHtml = marked.parse(tagged) as string;

  if (addedLineNumbers.size === 0) return renderedHtml;

  return highlightWithDom(renderedHtml);
}

/** `| a | b |` — a GFM table row. */
const TABLE_ROW = /^\s{0,3}\|.*\|\s*$/;
/** `|---|:--:|` — the header delimiter, which renders no row of its own. */
const TABLE_DELIMITER = /^\s{0,3}\|[\s:|-]+\|\s*$/;

function tagAddedLines(src: string, addedLineNumbers: Set<number>): string {
  if (addedLineNumbers.size === 0) return src;
  const lines = src.split('\n');
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    if (!line) continue;
    const lineNumber = i + 1;
    if (addedLineNumbers.has(lineNumber) && line.trim().length > 0) {
      lines[i] = markLine(line);
    }
  }
  return lines.join('\n');
}

function markLine(line: string): string {
  // The delimiter row produces no output element, and injecting into it breaks table parsing.
  if (TABLE_DELIMITER.test(line)) return line;

  // For a table row the marker has to go *inside* the last cell. Appended after the closing
  // pipe, the GFM table parser drops it — which is why added table rows were never highlighted.
  if (TABLE_ROW.test(line)) {
    const lastPipe = line.lastIndexOf('|');
    return line.slice(0, lastPipe) + ADDED_MARKER + line.slice(lastPipe);
  }

  return `${line}${ADDED_MARKER}`;
}

function highlightWithDom(html: string): string {
  const parser = new DOMParser();
  const doc = parser.parseFromString(`<div id="__md-diff-root">${html}</div>`, 'text/html');
  const root = doc.getElementById('__md-diff-root');
  if (!root) return html;

  const markers = Array.from(root.querySelectorAll(ADDED_SELECTOR));
  const highlighted = new Set<HTMLElement>();

  for (const marker of markers) {
    const block = closestBlock(marker);
    if (block && block.id !== '__md-diff-root' && !highlighted.has(block)) {
      block.classList.add('md-added-block');
      highlighted.add(block);
    }
  }

  for (const marker of markers) {
    marker.remove();
  }

  return root.innerHTML;
}

function closestBlock(node: Node): HTMLElement | null {
  let el = node.parentElement;
  while (el && el.id !== '__md-diff-root') {
    if (BLOCK_TAGS.has(el.tagName)) {
      // A marker inside a cell means the whole row was added — highlight the row, not the
      // one cell that happened to contain the marker.
      if (el.tagName === 'TD' || el.tagName === 'TH') {
        const row = el.closest('tr');
        if (row) return row as HTMLElement;
      }
      return el;
    }
    el = el.parentElement;
  }
  return null;
}
