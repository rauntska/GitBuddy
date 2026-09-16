# Prose Diff — Reviewing Markdown by Meaning, Not by Line

> Visual mockup: [`mockup.html`](mockup.html) — open in a browser. Switches between Source and
> Rendered (both shipped today) and the proposed Prose view over the same file and the same change.

## Problem

Markdown is now a large share of the diff in PRs across the whole org, not just this repo — specs, ADRs, runbooks, and design notes ride along with the code that implements them. Line-oriented diffing is a bad fit for prose, and the gap shows up in three specific ways.

PR #14 shipped `MarkdownDiffViewer` with a rendered/source toggle, defaulting to `rendered`. That fixed how markdown *reads*. What remains:

- **Highlighting is block-granular.** `renderMarkdownWithAddedHighlights` tags any block element containing an added line with `md-added-block`. Change one word in a paragraph and the whole paragraph lights up; the reviewer re-reads all of it to find what moved. The codebase already has the finer tool — `computeInlineDiff` in `diffHelpers.ts` runs `diff-match-patch` with semantic cleanup and `renderInlineDiffSegments` renders the result — but it is wired only to code line-pairs.
- **Deletions are invisible in rendered mode.** `getDiffLineNumbers` returns both `added` and `deleted` sets, but `renderMarkdownWithAddedHighlights` accepts only `addedLineNumbers`, and the viewer renders a single column of the new file. A PR that *removes* a requirement from a spec shows nothing at all in the default view. That is the most serious of the three: the reviewer cannot see what was taken away without switching to source.
- **Reflow produces phantom changes.** Hard-wrapped prose that gets re-wrapped reports every touched line as changed; soft-wrapped prose stores a paragraph as one long line, so a typo fix reports the entire paragraph as one deletion plus one addition. Either way the line diff overstates the change.
- **No orientation.** A 400-line spec with changes scattered across it offers no answer to "what actually changed here?" short of scrolling the whole document.

The deliberate constraint: this is the **deterministic** alternative to `ai-review-assistant`. Everything below is computed from the two file revisions — no model, no provider dependency, no per-PR cost, and identical output every time. It gives the orientation an AI summary would give without the summary being a guess.

## Rough Approach

### Section Map

- Parse headings with `marked` (already a dependency) into a section tree for both revisions, match sections by heading path, and classify each as added / removed / modified / unchanged with a change magnitude.
- Render as a compact strip above the document: `## Open Questions — new`, `## Rough Approach — 2 blocks changed`, `## Problem — unchanged`, each clickable to jump.
- Unchanged sections collapse by default. On a long spec with a small edit, this turns "scroll 400 lines" into "read two sections".
- The same tree can hang under the markdown file in `FileTree`, giving section-level navigation where file-level navigation exists today.

### Block Pairing and Word-Level Highlighting

- Pair blocks between revisions by section path and position rather than by line number — this is what makes reflow a non-event, since a re-wrapped paragraph pairs with its old self regardless of how many lines it occupies.
- Within a paired block, run the existing `computeInlineDiff` and render with `renderInlineDiffSegments`. Changed words get marked; the rest of the paragraph stays calm.
- Requires the base revision of the file. `MarkdownDiffViewer` already fetches full file content through the existing content endpoint, so this is the same call against a different ref rather than new plumbing.

### Show Removals

- With blocks paired, removed blocks render in place — struck through or tinted — instead of vanishing. Removed sections appear in the section map as `removed` even when their content is collapsed.

### Formatting-Only Noise

- Normalize before comparing: join hard-wrapped lines within a block, strip trailing whitespace, normalize list numbering and reference-link ordering.
- Report suppressed changes rather than hiding them — "6 formatting-only changes hidden" with a toggle. Silent suppression of a real edit would be worse than the noise it removes.

## Open Questions

- **Comment anchoring.** Review comments anchor to line numbers in the source. Rendered mode already has this tension; a section-and-block view sharpens it. Does commenting fall back to source view, or do blocks need to map back to line ranges — which is exactly the range model `multi-line-diff-comments` introduces?
- **Mixed content.** Fenced code blocks, tables, and Mermaid diagrams inside markdown should not be word-diffed — a word diff of a Mermaid graph is noise. Code blocks probably keep line diffing; tables likely want cell-level pairing. Where is the boundary?
- **Cost of the base revision.** Every rendered markdown file becomes two content fetches instead of one. Acceptable per file opened, but worth confirming against the concerns in `large-diff-performance`.
- **Pairing failures.** Renamed or reordered headings break path matching, and a wholesale restructure will pair badly. Is there a sane fallback — similarity matching, or degrade to the current view and say so?
- **Does the default change?** Once deletions are visible, `rendered` becomes a defensible default for review rather than just for reading. Until then, arguably `source` is the safer default and the current `rendered` default is hiding removals from reviewers.
- **Scope beyond markdown.** The same pairing applies to `.txt`, `.rst`, `.adoc`, and long string literals. Markdown first, but the block-pairing layer should not assume markdown.
- **Performance.** Word-diffing every block of a 2,000-line spec on open — measure before optimising, but it is the obvious hot spot.
