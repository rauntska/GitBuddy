# Plan — Prose Diff

Frontend-only. No C# changes, no migration, no new dependencies.
Work top to bottom; groups 2–5 are pure utility modules and are independently testable in
isolation before any component consumes them.

---

## 1. Preference plumbing and the 3-way toggle

1.1 `src/types/index.ts` — widen `markdownDiffMode?: 'rendered' | 'source'` to
`'rendered' | 'source' | 'prose'`.

1.2 `src/composables/useUserPreferences.ts` — widen the `setMarkdownDiffMode` parameter type to
the same union. The default at line 7 stays `'rendered'`.

1.3 `src/components/FileDiffViewer.vue` — replace the binary `toggleMarkdownMode` with an
explicit `setMarkdownMode(mode)`, and widen `effectiveMarkdownMode` to the three-value union.
Guard: an unrecognised stored value falls back to `'source'`, matching the existing defensive
shape of `effectiveMarkdownMode`.

1.4 `src/components/FileDiffViewer.vue` template — replace the two-button Source/Rendered
toggle (around lines 26 and 34) with a three-button segmented control. Mirror the density
toggle in `Dashboard.vue`: `border border-slate-800 bg-slate-900/60 p-0.5`, `font-mono text-xs`,
active button `bg-slate-700 text-slate-100`.

**Shippable at this point:** selecting Prose persists and round-trips; it renders nothing yet.

---

## 2. Block model — parse a revision into a section/block tree

New file: `src/utils/proseDiff/parseBlocks.ts`

2.1 `parseBlocks(source: string): BlockDoc` — run `marked.lexer(source)` and map the token
stream to a flat, ordered `Block[]`, each carrying:
- `kind`: `'heading' | 'paragraph' | 'listItem' | 'code' | 'table' | 'blockquote' | 'html'`
- `depth` for headings (1–6)
- `raw` (the source text of the block) and `text` (inline text, no markup)
- `startLine` / `endLine` in the source
- `sectionPath`: the array of ancestor heading texts, e.g. `['Requirements']`

2.2 Lists expand to one block per item — a list that gained one bullet should report one
changed block, not a whole changed list.

2.3 `code`, `table`, and fenced `mermaid` blocks are captured whole, never split. Mark them
`wordDiffable: false`.

2.4 Build `sections: Section[]` from heading blocks — each with `path`, `depth`, `headingBlock`,
and the `Block[]` under it up to the next heading of equal or shallower depth.

---

## 3. Block pairing

New file: `src/utils/proseDiff/pairBlocks.ts`

3.1 `pairBlocks(oldDoc: BlockDoc, newDoc: BlockDoc): PairedBlock[]` where a `PairedBlock` is
`{ old?: Block; new?: Block; status: 'added' | 'removed' | 'changed' | 'unchanged' }`.

3.2 Match sections first, by `sectionPath` equality. A section present on one side only is
wholly added or removed.

3.3 Within a matched section, pair blocks by (a) exact `normalizedText` equality, then
(b) same `kind` at the same relative position, then (c) best similarity above a threshold
using `computeInlineDiff` equal-segment ratio. Leftovers on the new side are `added`, on the
old side `removed`.

3.4 Order the output to follow the **new** document, splicing `removed` blocks in at their
old position relative to their surviving neighbours.

3.5 `pairingQuality(paired): number` — the fraction of blocks that matched. Consumers use it
for the fallback notice in 7.6.

---

## 4. Change classification and formatting-only suppression

New file: `src/utils/proseDiff/classifyChange.ts`

4.1 `normalizeForCompare(block: Block): string` — join hard-wrapped lines within the block into
one line, collapse internal runs of whitespace to a single space, strip trailing whitespace.

4.2 `classify(pair: PairedBlock): ChangeClass` returning
`'unchanged' | 'formatting-only' | 'content'`:
- `unchanged` — `raw` identical
- `formatting-only` — `raw` differs but `normalizeForCompare` is identical (covers re-wrap and
  trailing whitespace), **or** the pair is an ordered-list item whose only difference is its
  leading number, **or** both are reference-link definitions with identical target sets
- `content` — anything else

4.3 **Conservative by construction:** `classify` returns `'content'` for any case it does not
positively recognise. Never infer "probably formatting".

4.4 `describeSuppressed(pair): string` — a one-line human description used by the disclosure
list, e.g. `L18–21 · paragraph re-wrapped, text identical`. Line numbers come from
`Block.startLine` / `endLine`.

---

## 5. Word-level rendering

New file: `src/utils/proseDiff/renderBlock.ts`

5.1 `renderPairedBlock(pair: PairedBlock): string` returning HTML.

5.2 For a `content`-changed pair where both sides are `wordDiffable`, run the **existing**
`computeInlineDiff(oldText, newText)` from `src/utils/diffHelpers.ts` over the blocks' `raw`
text. Do not reimplement the differ.

> **Implemented differently:** the plan said to render with `renderInlineDiffSegments`. That
> helper escapes HTML and emits `<span>`s — correct for code lines, but it would leave markdown
> syntax visible as literal text. Instead the changed runs are wrapped in private-use sentinels
> (``–``), passed through `marked`, and swapped for `<ins>`/`<del>` afterwards —
> the same trick `renderMarkdownDiff.ts` uses for its block markers. `computeInlineDiff` is
> still the differ; only the rendering step differs.

5.2a **Word-boundary snapping** (not in the original plan, added after seeing real output).
diff-match-patch is character-based, so `three → five` renders as
`del("thre") ins("fiv") equal("e …")`. `snapToWordBoundaries` widens each run of changes out to
whole words by pulling the partial word off the neighbouring equal segments. It must not fire
when the run already starts or ends on whitespace, or deleting `"big "` from `"a big dog"`
would swallow `"dog"`. Both cases are covered in validation.

5.3 Run the resulting text through `marked.parse` so markdown inside the block still renders.
Guard against the word diff splitting markup across a segment boundary: if the rendered output
contains unbalanced inline markers, fall back to block-level highlighting for that block.

5.4 `added` blocks render with the added treatment, `removed` blocks render struck through and
dimmed, `unchanged` render plain.

5.5 Blocks with `wordDiffable: false` render at block granularity regardless of status.

---

## 6. Section map component

New file: `src/components/prose-section-map.vue`

6.1 Props: `sections: SectionSummary[]`. Emits `select(sectionPath)`.

6.2 Header row: `SECTIONS` in `text-sm font-semibold uppercase tracking-wider text-slate-300`,
followed by the counts in `font-mono tabular-nums text-slate-500` —
`5 · 1 added · 1 removed · 1 changed · 2 unchanged`.

6.3 One chip per section: a mono glyph (`+` emerald / `−` red / `◐` amber / `–` slate-600), the
heading text, and a mono meta label (`new`, `removed`, `2 blocks`, `unchanged`).

6.4 Chip styling follows `docs/reference/visual-style.md`: `border border-slate-800 rounded`,
hover `bg-slate-800/40 border-slate-700 -translate-y-px transition-all duration-150`.
No colored glow.

---

## 7. Prose diff viewer component

New file: `src/components/prose-diff-viewer.vue`

7.1 Props `{ file: FileDiff; prId?: number }` — same shape as `MarkdownDiffViewer.vue`.

7.2 Fetch both revisions in one call to `GET /pull-requests/{id}/files/content` with all four
range params set to the full file. Reuse the loading / `fetchError` / empty states from
`MarkdownDiffViewer.vue` verbatim, including the "Try the Source view" error copy.

7.3 Pipeline on load: `parseBlocks` both sides → `pairBlocks` → `classify` each pair →
partition into rendered blocks and suppressed formatting-only entries → `renderPairedBlock`.

7.4 Render `prose-section-map` above the document. Clicking a chip scrolls its section into
view (`scrollIntoView({ behavior: 'smooth', block: 'center' })`, matching
`FileDiffViewer.vue:504`) and expands it if collapsed.

7.5 Sections whose every pair is `unchanged` render collapsed to a single
`unchanged — N blocks` line, expanding on click. Per-section state is local component state,
not persisted — matching the `viewMode` precedent in `ReviewTimeline.vue`.

7.5a **Drop empty sections** (not in the original plan, added after seeing real output). A
document title (`# Title` above `##` sections) owns no blocks of its own once content is
bucketed at `mapDepth`, and rendered as `unchanged — 0 blocks`. Sections with no body pairs and
an `unchanged` status are filtered out of both the document and the section map.

7.6 Per-section suppression disclosure: `≡ N formatting-only changes hidden in this section`
with a `show` / `hide` toggle revealing the `describeSuppressed` lines. Omit the row entirely
when the count is zero.

7.7 If `pairingQuality < 0.5`, render a notice above the document — pairing was unreliable,
suggest Source — and still render what paired.

7.8 After render, call `renderMermaidBlocks` on the content ref, matching
`MarkdownDiffViewer.vue`'s use of it.

---

## 8. Wire into FileDiffViewer

8.1 Import `prose-diff-viewer.vue` and render it when `effectiveMarkdownMode === 'prose'`,
alongside the existing `MarkdownDiffViewer` branch for `'rendered'`.

8.2 Confirm the existing `showMarkdownToggle` guard (`isMarkdownFile && canReconstruct`) gates
all three modes — a non-markdown file must never reach prose.

8.3 Reuse the `markdown-diff-content` prose styles where they apply; add `ins` / `del` /
removed-block rules to the same stylesheet rather than a second one.

---

## 9. Validation

Work `validation.md` end to end before committing.
