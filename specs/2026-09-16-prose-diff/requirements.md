# Prose Diff — Section and Word Level Markdown Diff

Derived from `docs/ideas/prose-diff/idea.md`. Visual reference: `docs/ideas/prose-diff/mockup.html`.

Direct follow-up to `specs/2026-08-11-rendered-markdown-diff/`, which listed "rendering deletions
inside the rendered view" and "word-level inline highlighting within a changed markdown block"
as explicit v1 out-of-scope items.

## Scope

### In scope

- A third markdown diff mode, **Prose**, alongside the shipped `Source` and `Rendered` modes,
  selected from the existing segmented control in the file header (markdown files only).
- **Block pairing** — both revisions of the file are parsed into a block tree and blocks are
  matched between them by section path and position, instead of by line number. A paragraph
  that was only re-wrapped pairs with its old self and reports no change.
- **Word-level highlighting** — within a paired, changed block, changed words are marked using
  the existing `computeInlineDiff` / `renderInlineDiffSegments` helpers in
  `src/utils/diffHelpers.ts`.
- **Removals are rendered.** Removed blocks and removed sections appear in place, struck
  through and dimmed. This closes the gap where the shipped `rendered` mode passes only
  `addedLineNumbers` to `renderMarkdownWithAddedHighlights` and therefore shows nothing at all
  when content is deleted.
- **Section map** — a strip above the document listing every top-level section with its state
  (`added` / `removed` / `changed` / `unchanged`) and a change count, each clickable to scroll
  to that section. Unchanged sections render collapsed to a single "unchanged — N blocks" line
  and expand on click.
- **Formatting-only noise suppression** — changes with no semantic delta are classified as
  formatting-only, suppressed from the rendered output, and reported in a per-section
  disclosure ("6 formatting-only changes hidden" → `show`). Suppressed classes:
  - paragraph re-wrap (block text identical after joining hard-wrapped lines)
  - trailing whitespace changes
  - ordered-list renumbering where item content is unchanged
  - reference-link definition reordering
- Mermaid rendering continues to work in prose mode, reusing `renderMermaidBlocks` from
  `src/composables/useMermaid.ts`.

### Out of scope

- **Any backend or database change.** See the API contract below — there is none.
- Changing the shipped `rendered` mode. `MarkdownDiffViewer.vue` and
  `renderMarkdownDiff.ts` are left exactly as they are; prose mode is a separate component.
- Changing the default mode. New and existing users keep `rendered` until prose proves out
  (see Decisions 4).
- Inline commenting inside prose mode — it remains available in `Source`, matching the
  constraint the rendered view already has.
- Prose diffing for non-markdown files (`.txt`, `.rst`, `.adoc`), even though the block-pairing
  layer is deliberately written not to assume markdown.
- Word-level diffing inside fenced code blocks, tables, and Mermaid blocks — see Decisions 3.
- Diffing against the PR's true merge base — see Decisions 5.

### API contract

**No new endpoints, no changed endpoints, no migration.**

`GET /api/pull-requests/{id}/files/content` (`[Authorize]`) already returns both revisions.
`GetFileContentHandler` fetches the old side from `pr.TargetBranch` and the new side from
`pr.SourceBranch` via `IGitHubGraphQLService.GetFileContentAsync`, and returns:

```json
{ "oldLines": [{ "lineNumber": 1, "content": "..." }],
  "newLines": [{ "lineNumber": 1, "content": "..." }] }
```

Prose mode calls it once per file with all four range parameters set to the full file
(`oldStartLine=1&oldEndLine=1000000&newStartLine=1&newEndLine=1000000`), mirroring the
`FULL_FILE_END_LINE` constant already used by `MarkdownDiffViewer.vue`. The shipped rendered
view requests only the new side; prose requests both. Same endpoint, same call count.

`PATCH /api/userpreferences` (`[Authorize]`) accepts `markdownDiffMode: "prose"` **with no
server change**: `UserPreferences.MarkdownDiffMode` is a `text` column with
`defaultValue: "rendered"` and no CHECK constraint, `UserPreferencesDto` and
`UpdateUserPreferencesRequest` both type it as a plain `string`, and `UserService` assigns the
incoming value without validation. Only the TypeScript union widens.

## Decisions

1. **Prose is a third mode, not a replacement.** `markdownDiffMode` becomes
   `'rendered' | 'source' | 'prose'`. The block-highlight renderer stays available as a
   fallback if pairing misbehaves on real-world documents. Cost: a mode that hides deletions
   remains reachable, and remains the default.

2. **No migration.** Adding a third allowed value to an unvalidated `text` column requires no
   schema change. (An earlier draft of this spec assumed a migration was needed; it is not.)

3. **Word diff applies to prose blocks only.** Fenced code blocks, tables, and Mermaid blocks
   are paired like any other block, but a changed one is marked changed at block granularity
   rather than word-diffed — a word diff of a Mermaid graph or a code block is noise. Code
   blocks keep the existing line-oriented treatment inside the block.

4. **Default stays `rendered`.** Flipping the default is a separate decision that should follow
   evidence from real use, not ship blind with the feature.

5. **The old side is the target branch tip, not the merge base.** This is how the existing
   endpoint already behaves and this feature does not change it. Known consequence: if the
   target branch has moved since the PR branched, prose mode may show changes the PR did not
   make. Documented, accepted, and called out in the UI only if it proves confusing in use.

6. **New component, kebab-case.** `prose-diff-viewer.vue` and `prose-section-map.vue` follow
   the newer convention (`comment-thread.vue`, `file-diff-header.vue`, `diff-line-row.vue`)
   rather than the older PascalCase files.

7. **Parse with `marked`'s lexer, not regex.** `marked` is already a dependency and
   `marked.lexer()` yields a typed block token stream — headings, paragraphs, lists, code,
   tables — which is exactly the block boundary set pairing needs. No new dependency.

## Context

### Existing code to mirror

| Concern | Follow |
|---|---|
| Markdown file detection | `isMarkdownFile` / `canReconstruct` in `src/utils/markdownDiffReconstruct.ts` |
| Full-file fetch + render lifecycle | `MarkdownDiffViewer.vue` (loading / fetchError / empty states) |
| Word-level diff | `computeInlineDiff`, `renderInlineDiffSegments` in `src/utils/diffHelpers.ts` |
| Mode toggle + persistence | `showMarkdownToggle`, `markdownModePref`, `toggleMarkdownMode` in `FileDiffViewer.vue`; `setMarkdownDiffMode` in `useUserPreferences.ts` |
| Mermaid | `renderMermaidBlocks` in `src/composables/useMermaid.ts` |
| Visual language | `docs/reference/visual-style.md` — slate-900 base, `text-slate-200` body, mono tabular numerics, `border-slate-800`, emerald/red accents on glyphs and numerals only |

### Conventions

- `toggleMarkdownMode` is currently a binary flip and must become a 3-way segmented control.
  The density toggle in `Dashboard.vue` is the reference pattern for a segmented control.
- All rendering stays client-side, consistent with the prior spec's decision.
- No new dependencies. `marked`, `diff-match-patch`, and `mermaid` are all already present.

### Open questions

- **Pairing failures.** Renamed or reordered headings break section-path matching. v1 falls
  back to positional matching within the parent section, and if a document pairs badly
  (heuristic: more than half its blocks unmatched) prose mode shows a notice and suggests
  Source. Whether that threshold is right is unknown until it meets real documents.
- **Large documents.** Word-diffing every changed block of a 2,000-line spec on open is the
  obvious hot spot. Measure before optimising; `computeInlineDiff` runs
  `diff_cleanupSemantic`, which is not free.
- **Noise suppression is the risky part.** Misclassifying a real edit as formatting-only hides
  it from a reviewer. The disclosure list exists so suppression is never silent, and the
  classifier must be conservative: anything it is unsure about is a real change.
- **Comment anchoring.** Prose mode is read-only in v1, deferring the block↔line-range mapping
  that `docs/ideas/multi-line-diff-comments/idea.md` would introduce.
