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

function tagAddedLines(src: string, addedLineNumbers: Set<number>): string {
  if (addedLineNumbers.size === 0) return src;
  const lines = src.split('\n');
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    if (!line) continue;
    const lineNumber = i + 1;
    if (addedLineNumbers.has(lineNumber) && line.trim().length > 0) {
      lines[i] = `${line}${ADDED_MARKER}`;
    }
  }
  return lines.join('\n');
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
    if (BLOCK_TAGS.has(el.tagName)) return el;
    el = el.parentElement;
  }
  return null;
}
