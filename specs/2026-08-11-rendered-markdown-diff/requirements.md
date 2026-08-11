# Rendered Markdown Diff View

## Scope

### In scope

- For files whose detected language is `markdown` (`.md`), render the diff as a single
  **formatted markdown document** (the new version of the file) instead of the raw
  `+`/`-` line dump.
- Added lines/blocks are highlighted green (emerald) inside the rendered document so the
  reader can see what changed in the context of the formatted doc.
- A **Source / Rendered** segmented toggle in the file header, shown only for markdown
  files, switches back to the existing raw split diff.
- The chosen mode is persisted as a new global user preference `markdownDiffMode`, so it
  sticks across sessions and files (mirrors `diffViewMode`).
- The **complete new-version source file** is fetched from the existing
  `GET /pullrequests/{id}/files/content` endpoint (with a large line range, so the handler
  returns the whole file). The raw unified patch is parsed client-side only to learn *which*
  new line numbers are additions. Rendering the whole file (rather than reconstructing from
  the patch's hunks) avoids broken formatting — the patch only carries hunks with ~3 lines of
  context, so blocks split across hunk boundaries would render as fragments.
- **Mermaid diagrams** (`​```mermaid` fenced blocks) are rendered as SVG graphics (dark
  theme) via the `mermaid` library, lazy-loaded only when a rendered file contains a diagram.

### Out of scope

- Rendering deletions inside the rendered view (removed content is visible via the Source
  toggle / raw split diff).
- Inline per-line commenting inside the rendered markdown view (v1 is read-only; commenting
  remains available in Source view).
- Word-level inline highlighting within a changed markdown block — v1 highlights at block
  granularity (added paragraph / list item / code block / heading).
- Fetching the full before/after blob from the backend when the patch is truncated — a
  follow-up if truncated-patch docs become a real pain.
- Any backend rendering of markdown — all rendering stays client-side.

### API contract

No new endpoints. One existing endpoint gains a new field:

**`PATCH /api/userpreferences`** (`[Authorize]`)

Request body gains an optional field (alongside the existing preferences):

```json
{
  "markdownDiffMode": "rendered"
}
```

- `markdownDiffMode` — `"rendered" | "source"` (string). `null`/omitted means "leave
  unchanged". Invalid values are stored as-is by the current patch logic (no server-side
  validation today, matching `DiffViewMode` / `ListViewMode` behavior).

Response (`200 OK`) — `UserPreferencesDto` now includes:

```json
{
  "diffViewMode": "unified",
  "markdownDiffMode": "rendered",
  "...": "all other existing fields unchanged"
}
```

**`GET /api/userpreferences`** (`[Authorize]`) — response now includes `markdownDiffMode`
with the stored value (default `"rendered"` for rows created before the migration, via the
EF migration's `defaultValue`).

Error responses: unchanged — `401` if not authenticated.

## Decisions

| Decision | Choice | Why |
|---|---|---|
| Document scope | Render the **new version** of the doc, added lines highlighted green | Closest to how the doc will look post-merge; deletions remain visible via the raw toggle. |
| Activation | **Auto-rendered for `.md`**, with a Source/Rendered toggle | Best default UX; escape hatch preserved. |
| Persistence | Single global user preference `markdownDiffMode: 'rendered' \| 'source'` (default `'rendered'`) | Mirrors the existing `diffViewMode` pattern; "sticks" across sessions/files as requested. |
| New-file text source | **Fetch the full new-version file** via the existing `/files/content` endpoint (large line range, no backend changes); parse the patch client-side only for the added-line set | Reconstructing from the patch alone produced broken formatting (hunks only carry ~3 lines of context, so blocks split across hunk boundaries rendered as fragments). Fetching the complete source and rendering it whole gives clean formatting; the patch still tells us which lines are added. |
| Highlight granularity | Block-level (added paragraph/list-item/code-block/heading) | Markdown blocks are the natural attribution unit; word-level inline highlighting inside a block is noisy and left out of v1. |
| Commenting in rendered view | Read-only (no per-line anchors in v1) | Inline comments remain available in Source view; adding anchors into rendered markdown is a follow-up. |
| Dependencies | Added `@tailwindcss/typography` (dev dep, registered in `tailwind.config.js`) and `mermaid` (runtime dep, lazy-loaded) | Typography plugin makes `prose` actually apply (it was never registered before). Mermaid renders `​```mermaid` fenced blocks as SVG diagrams (common in design docs). Mermaid is code-split into its own chunk via dynamic `import()` so the ~800KB library only downloads when a rendered `.md` file actually contains a diagram. `marked`, `diff-match-patch`, `prismjs` were already present. |
| Layout | Responsive centered reading column — full-width on small screens, `64rem` at `lg`, `72rem` at `xl`, capped `90rem` at `2xl`; width enforced via scoped CSS (not Tailwind utilities) to defeat the typography plugin's built-in `max-width: 65ch` | Full-width rendered markdown is hard to read; a centered column matches GitHub/doc-site convention. Scoped CSS (`max-width: none !important` on the prose element) guarantees the column wins the cascade for every instance. |

## Context

### Existing code to mirror

- **Preference persistence (full-stack):** `PrioritySort` is the most recent precedent —
  see migration `20260716163459_AddPrioritySortAndShowContext`. End-to-end path:
  `GitBuddy.Domain/Models/UserPreferences.cs` → `GitBuddy.Api/DTOs/UserPreferencesDtos.cs`
  → `GitBuddy.Api/Services/UserService.cs` (2 DTO constructions + patch block; note mapping
  is inlined in `UserService.cs`, **not** in `MappingExtensions.cs`) →
  `dotnet ef migrations add` → frontend `types/index.ts` → `useUserPreferences.ts`.
- **Markdown rendering:** `marked` is already configured (`gfm: true, breaks: true`) in
  `gitbuddy-vue/src/composables/useProxiedHtml.ts`. Reuse the same options for consistency.
- **Toggle UI pattern:** `ReviewTimeline.vue` segmented toggle
  (`viewMode === 'x' ? 'bg-slate-700 text-slate-100' : 'text-slate-400 hover:text-slate-200'`).
- **Diff parsing:** `parsePatch` in `gitbuddy-vue/src/utils/diffHelpers.ts` already produces
  `DiffHunk[]` / `DiffLine[]` with `oldLineNumber` / `newLineNumber` — no new parser needed.
- **Markdown prose styling:** `prose prose-invert prose-sm` (Tailwind Typography), used by
  `DescriptionRenderer.vue`.
- **File-type detection:** `file.language === 'markdown'` is set by backend
  `LanguageDetectionService` (`.md → "markdown"`); frontend `detectLanguageFromPath` also
  maps `md → markdown` for fallback.

### Conventions to follow

- Backend: primary constructors, record DTOs with positional parameters, nullable reference
  types, no code comments unless requested.
- Frontend: `<script setup lang="ts">`, Composition API, `defineProps<{ ... }>()`,
  kebab-case component filenames, `import type` for type-only imports, Tailwind inline,
  slate/emerald/rose palette. Composables named `useXxx.ts`.
- No new dependencies without approval — none needed here.

### Open questions

None — all three scope decisions were resolved during the feature-spec interview.
