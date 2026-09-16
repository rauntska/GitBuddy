# Validation — Prose Diff

## Automated

```bash
cd gitbuddy-vue && npm run build
```

This is the real gate — `npm run build` runs `vue-tsc -b` first, so the widened
`markdownDiffMode` union is type-checked across every consumer.

```bash
dotnet build -c Release
```

Regression check only. This feature changes no C# file; if this fails, something was touched
that should not have been.

`dotnet test` is **not** applicable — the repository has no test project (see
`docs/ideas/automated-test-foundation/idea.md`). The utility modules in plan groups 2–5 are
pure functions with no Vue or DOM dependency, so they are the natural first candidates if a
Vitest setup lands; this spec does not add one.

## Correctness checks

- **No backend delta.** `git diff --stat master -- '*.cs' 'GitBuddy.Domain/Migrations'` returns
  empty. No new endpoint, no `[Authorize]` to audit, no migration to apply.
- **Preference round-trips.** `PATCH /api/userpreferences` with `{"markdownDiffMode":"prose"}`
  returns `200` and a body echoing `"prose"`; a `GET` afterwards returns the same. Confirms the
  unvalidated `text` column accepts the third value with no server change.
- **Unknown values degrade safely.** Set the column to a garbage string directly in Postgres;
  the file header must fall back to Source rather than render blank or throw.
- **Existing modes unchanged.** `MarkdownDiffViewer.vue` and `renderMarkdownDiff.ts` are
  untouched in the diff; Rendered and Source behave exactly as on `master`.
- **Non-markdown files never reach prose.** With `markdownDiffMode = 'prose'` stored, open a
  `.cs` file — it renders the normal split/unified diff and shows no markdown toggle.
- **No new dependencies.** `git diff master -- gitbuddy-vue/package.json` is empty.

## Manual walkthrough

Use a PR that modifies a markdown file with a mix of changes — the spec files in this very
branch work, or craft one.

1. Open a PR with a changed `.md` file → `PRDetail` → select the file in `FileTree`.
2. The file header shows a three-way `Source | Rendered | Prose` segmented control.
3. Select **Prose**. Expect:
   - a section map strip listing every top-level heading with state glyph and counts
   - changed paragraphs showing `<ins>`/`<del>` at **word** granularity, not whole-block tint
   - removed blocks and removed sections visible, struck through and dimmed
   - unchanged sections collapsed to `unchanged — N blocks`
4. Click a section chip → scrolls to that section and expands it if collapsed.
5. Click a collapsed `unchanged` section → expands in place.
6. Reload the page → still in Prose. Switch to Source → reload → still Source.

### Edge cases

| Case | Expected |
|---|---|
| Paragraph only re-wrapped, text identical | Reported as formatting-only and suppressed; appears in the disclosure list, not in the document |
| `three` → `five` mid-sentence | Marked as whole words, not `del("thre") ins("fiv")` — word-boundary snapping (plan 5.2a) |
| Deleting a whole word (`"a big dog"` → `"a dog"`) | Only `big ` is struck; `dog` must NOT be absorbed into the deletion |
| Document title (`# Title`) above `##` sections | Does not render an `unchanged — 0 blocks` section, and is absent from the section map (plan 7.5a) |
| List item that only gains/loses a trailing blank line | Suppressed, reason reads "trailing whitespace removed" — not "re-wrapped" |
| `show` on the suppression disclosure | Lists each suppressed change with line numbers and a reason |
| Section removed entirely | Appears in the section map as `removed` and renders struck through in place |
| Section added entirely | Appears as `new`; all blocks render as added |
| Heading renamed | Pairing falls back to positional matching; if quality drops below 0.5 a notice appears and Source is suggested |
| File contains a Mermaid block | Diagram still renders as SVG; the block is not word-diffed |
| File contains fenced code / a table | Marked changed at block granularity, never word-diffed |
| Newly added `.md` file | Every block renders as added; no removals; section map all `new` |
| Deleted `.md` file | Toggle hidden — `canReconstruct` already returns `false` for `status === 'deleted'` |
| Markdown file with no changes on either side | Empty/"no content" state, matching `MarkdownDiffViewer.vue` |
| Large spec (1,500+ lines) | Renders without locking the tab; note the timing — this is the known hot spot |
| Target branch moved since the PR branched | May show changes the PR did not make. Accepted and documented (requirements Decisions 5) — confirm it does not *crash*, only over-reports |

### Visual check

Against `docs/reference/visual-style.md`, and comparable to
`docs/ideas/prose-diff/mockup.html`:

- body text `text-slate-200`; counts and glyphs `font-mono tabular-nums`
- `border-slate-800` borders, hover `bg-slate-800/40` + `border-slate-700` + 1px lift
- emerald for insertions, red for deletions, on text and thin borders only — no saturated fills
- no colored glow on hover; motion limited to the existing transition tokens
- renders correctly at a narrow viewport (the comments panel open, file tree expanded)

## Definition of done

- `npm run build` and `dotnet build -c Release` both pass.
- All six walkthrough steps behave as described.
- Every edge-case row behaves as described, or is documented in the spec as a known limitation
  with a reason.
- No `.cs`, migration, or `package.json` changes in the diff.
- `Source` and `Rendered` modes are byte-identical in behaviour to `master`.
- Formatting-only suppression never hides a change that is not listed in the disclosure.
