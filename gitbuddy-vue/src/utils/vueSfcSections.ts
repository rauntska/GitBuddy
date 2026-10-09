/**
 * Works out which Prism grammar each line of a `.vue` diff should be highlighted with.
 *
 * A Single File Component mixes three languages, and the diff viewer highlights one line at a
 * time — so Prism never sees a whole `<script>…</script>` block and can't switch grammars by
 * itself. Everything inside `<script>` and `<style>` would fall through the markup grammar
 * untokenised, which is most of the code worth reviewing.
 *
 * We only have the patch, not the file, so a hunk often shows no block tag at all. Inference is
 * therefore scoped to a single hunk: the unseen lines in the gap between two hunks are exactly
 * where `</template>` and `<script setup>` tend to sit, so carrying state across a gap is wrong
 * far more often than it is right.
 *
 * Within a hunk we scan for the block tags it does show (exact, both forwards and backwards — a
 * hunk ending on `</script>` tells us the lines above it were script). A hunk showing no tag at
 * all is settled by one of two guesses: the bracket, when the nearest resolved hunks on either
 * side agree on a non-markup block, or otherwise a vote on the shape of the lines. A wrong guess
 * just highlights that hunk the way the viewer already did before any of this existed.
 */

export type SfcGrammar = 'markup' | 'typescript' | 'css' | 'scss' | 'less';

/** `<template>`, `<script setup lang="ts">`, `<style scoped>` — top level, so column 0. */
const OPEN_TAG = /^<(template|script|style)(\s[^>]*)?>\s*$/;
const CLOSE_TAG = /^<\/(template|script|style)>\s*$/;
const LANG_ATTR = /\blang\s*=\s*["']([^"']+)["']/;

type Block = 'template' | 'script' | 'style';

/** The lines before the first tag we see are unresolved until the backfill pass runs. */
const UNKNOWN = 'unknown';
type Resolved = SfcGrammar | typeof UNKNOWN;

function grammarForOpenTag(block: Block, attrs: string): SfcGrammar {
  if (block === 'template') return 'markup';
  // TypeScript's grammar is a superset of JavaScript's, so it covers `lang="js"` too.
  if (block === 'script') return 'typescript';
  switch (attrs.match(LANG_ATTR)?.[1]?.toLowerCase()) {
    case 'scss':
    case 'sass':
      return 'scss';
    case 'less':
      return 'less';
    default:
      return 'css';
  }
}

/**
 * A closing tag carries no `lang`, so a backfilled block gets the safe default: TypeScript is a
 * superset of JavaScript and plain CSS reads acceptably as SCSS or Less.
 */
function grammarForCloseTag(block: Block): SfcGrammar {
  if (block === 'script') return 'typescript';
  if (block === 'style') return 'css';
  return 'markup';
}

const TAG_OPENS_LINE = /^\s*<\/?[a-zA-Z]/;
const TAG_CLOSES_LINE = /\/?>\s*$/;
const DIRECTIVE = /(?:^|\s)(?:v-[\w:.-]+|[:@#][\w:.-]+)=/;
const CSS_DECL = /^\s*[-\w]+\s*:\s*[^;{]+;\s*$/;
const CSS_RULE = /^\s*[.#&@:[][^{};]*\{\s*$/;
const CODE_LIKE = /\b(?:const|let|var|function|import|export|return|await|async|interface|type|class)\b|=>/;
// `loading: boolean;` is a valid CSS declaration as far as CSS_DECL is concerned. A member of a
// `defineProps<{…}>()` body gives itself away by its type: a primitive, or something bracketed
// or capitalised. An optional `?:` is decisive on its own.
const TS_MEMBER =
  /^\s*(?:readonly\s+)?[\w$]+\s*:\s*(?:[A-Z[({]|(?:string|number|boolean|bigint|symbol|any|unknown|never|void|null|undefined)\b)/;
const TS_OPTIONAL_MEMBER = /^\s*[\w$]+\?\s*:/;

/** Last resort for a hunk with no tag and no usable bracket: judge the lines on their shape. */
function voteGrammar(lines: string[]): SfcGrammar {
  let markup = 0;
  let css = 0;
  let cssRules = 0;
  let code = 0;

  for (const line of lines) {
    // A line that opens with a tag is the strongest template signal there is; a Vue directive
    // or a bare `>` terminator is good evidence of a wrapped attribute list.
    if (TAG_OPENS_LINE.test(line)) markup += 2;
    else if (DIRECTIVE.test(line)) markup += 2;
    else if (line.trim() !== '' && TAG_CLOSES_LINE.test(line) && !line.includes('=>')) markup++;

    if (TS_MEMBER.test(line) || TS_OPTIONAL_MEMBER.test(line)) code++;
    else if (CSS_RULE.test(line)) { css++; cssRules++; }
    else if (CSS_DECL.test(line)) css++;

    if (CODE_LIKE.test(line)) code++;
  }

  // `status: string;` in a props interface is indistinguishable from a CSS declaration, so the
  // declaration count only counts for much once a selector or at-rule has anchored the hunk.
  const cssTrusted = cssRules > 0 || css > code * 3;

  if (markup > code && markup >= css) return 'markup';
  if (cssTrusted && css > code && css >= markup) return 'css';
  return 'typescript';
}

/** Resolves one hunk from the block tags it contains. Returns all-known, or all-UNKNOWN. */
function resolveHunk(lines: string[]): Resolved[] {
  const out: Resolved[] = new Array(lines.length).fill(UNKNOWN);
  let current: Resolved = UNKNOWN;
  let firstTagIndex = -1;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i] ?? '';

    const open = line.match(OPEN_TAG);
    if (open) {
      if (firstTagIndex === -1) firstTagIndex = i;
      out[i] = 'markup';
      current = grammarForOpenTag(open[1] as Block, open[2] ?? '');
      continue;
    }

    const close = line.match(CLOSE_TAG);
    if (close) {
      if (firstTagIndex === -1) firstTagIndex = i;
      out[i] = 'markup';
      // Back at the root of the file, where only blank lines and comments live.
      current = 'markup';
      continue;
    }

    out[i] = current;
  }

  // Backfill the lines above the first tag — the only part the forward scan can't reach.
  if (firstTagIndex > 0) {
    const close = (lines[firstTagIndex] ?? '').match(CLOSE_TAG);
    const prefix: SfcGrammar = close ? grammarForCloseTag(close[1] as Block) : 'markup';
    for (let i = 0; i < firstTagIndex; i++) out[i] = prefix;
  }

  return out;
}

/**
 * Resolves a grammar for every line of every hunk of one `.vue` file.
 *
 * Hunks must be in file order, and each inner array holds that hunk's line contents (both sides —
 * an added and a deleted line at the same spot share a section, so mixing them is harmless).
 * Returns one grammar per input line, in the same shape.
 */
export function resolveVueHunkGrammars(hunks: string[][]): SfcGrammar[][] {
  const resolved = hunks.map(resolveHunk);
  const isKnown = resolved.map(g => g.length > 0 && g[0] !== UNKNOWN);

  return resolved.map((grammars, i) => {
    if (isKnown[i]) return grammars as SfcGrammar[];

    const fallback = bracketGrammar(resolved, isKnown, i) ?? voteGrammar(hunks[i] ?? []);
    return grammars.map(() => fallback);
  });
}

/**
 * If the nearest resolved hunks either side of an unresolved one agree, the gap never left that
 * block. Only trusted for `<script>` and `<style>`: markup is ambiguous between a `<template>`
 * body and the root lines between two blocks, so agreement there proves nothing.
 */
function bracketGrammar(
  resolved: Resolved[][],
  isKnown: boolean[],
  index: number
): SfcGrammar | null {
  let before: Resolved | undefined;
  for (let i = index - 1; i >= 0; i--) {
    if (isKnown[i]) { const prev = resolved[i]!; before = prev[prev.length - 1]; break; }
  }

  let after: Resolved | undefined;
  for (let i = index + 1; i < resolved.length; i++) {
    if (isKnown[i]) { after = resolved[i]![0]; break; }
  }

  if (!before || before !== after || before === 'markup' || before === UNKNOWN) return null;
  return before;
}
