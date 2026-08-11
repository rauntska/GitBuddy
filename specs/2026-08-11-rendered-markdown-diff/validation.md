# Validation: Rendered Markdown Diff View

## Automated

- `dotnet build -c Release` from repo root — passes (model + DTO + UserService + migration
  compile).
- `cd gitbuddy-vue && npm run build` — passes (includes `vue-tsc` typecheck; the new
  utility/component/toggle typecheck against the updated `UserPreferences` interface).
- No test framework is configured in this repo — there are no unit tests to run.

## Correctness checks

- **`[Authorize]` preserved:** no auth attributes changed; `GET`/`PATCH
  /api/userpreferences` still require auth.
- **Preference round-trip:** `PATCH /api/userpreferences` with `{ "markdownDiffMode":
  "source" }` returns the updated value; a subsequent `GET` returns `"source"`. Omitting the
  field on a later `PATCH` leaves it unchanged (current patch semantics).
- **Migration applies cleanly:** `dotnet ef database update` succeeds; existing rows get
  `MarkdownDiffMode = "rendered"` via the `defaultValue`.
- **Default behavior:** for a user with no stored preference, `.md` files open in Rendered
  mode by default (frontend default `'rendered'`).
- **Non-markdown files unaffected:** no Source/Rendered toggle shown; raw split diff renders
  exactly as before.
- **Fallback:** a `.md` file with no `patch` (or `status === 'deleted'`) does not show the
  toggle and renders in Source mode (no crash, no empty rendered panel).
- **No new npm dependencies** added to `package.json`.
- **Typecheck:** `markdownDiffMode` is a typed field end-to-end (model → DTO → frontend
  type → composable).

## Manual walkthrough

1. **Default rendered view:** open a PR that modifies a `.md` file. The file renders as a
   formatted markdown document; added blocks (new paragraphs/headings/list items/code
   blocks) are highlighted green. Existing content renders normally.
2. **Toggle to Source:** click **Source**. The view switches to the existing raw split diff
   (additions emerald, deletions rose, Prism highlighting). Click **Rendered** to go back.
3. **Persistence:** toggle to Source, reload the page (or navigate away and back). The `.md`
   file opens in Source mode — preference stuck. Toggle back to Rendered, reload — opens
   Rendered.
4. **Non-markdown file:** open a `.ts` / `.vue` / `.cs` file in the same PR. No toggle
   visible; raw split diff as before.
5. **Pure-addition markdown file:** a newly-added `.md` file (`status === 'added'`) renders
   as a complete formatted document with everything highlighted green (since all lines are
   additions).
6. **Pure-deletion edge case:** a `.md` file with `status === 'deleted'` — toggle hidden,
   falls back to Source so the user can still see what was removed.
7. **Large/truncated patch:** a very large `.md` whose patch GitHub truncated — rendered
   view shows the portion present in the patch (best-effort); user can switch to Source for
   the raw diff. No crash.

## Definition of done

- `.md` diffs render as a formatted document with additions highlighted green by default.
- Source/Rendered toggle works and persists across sessions via `markdownDiffMode`.
- Non-`.md` files and fallback paths (no patch, deleted file) behave correctly.
- `dotnet build -c Release` and `npm run build` both pass.
- EF migration applies cleanly.

## Known v1 limitations

- **Additions inside fenced code blocks** are not highlighted in the rendered view:
  `marked` HTML-escapes content inside `<pre><code>`, so the added-line marker element
  does not survive as a real DOM node there. The code block itself renders normally;
  additions within it are visible via the Source toggle. (Tracked as a follow-up if it
  matters in practice.)
- **GitHub-hosted images** in the rendered view are not routed through the image proxy
  (unlike PR descriptions via `useProxiedHtml`). They render as raw `<img>` tags and may
  fail to load under hotlink protection. Follow-up: feed the highlighted HTML through
  the proxy pipeline.
- **Inline per-line commenting** is not available in the rendered view (read-only). Use
  Source view for inline comments.
- **Network dependency:** the rendered view fetches the full file from GitHub on each
  mount (via `/files/content`). On network/GitHub errors it shows a message and falls
  back gracefully to Source.

