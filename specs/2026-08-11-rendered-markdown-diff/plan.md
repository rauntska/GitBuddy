# Implementation Plan: Rendered Markdown Diff View

Execute top-to-bottom. Each numbered group is independently shippable.

## 1. Backend — domain model

**File:** `GitBuddy.Domain/Models/UserPreferences.cs`

Add a new scalar property next to the other string preferences (e.g. after `ListViewMode`):

```csharp
public string MarkdownDiffMode { get; set; } = "rendered";
```

EF maps this to a `text` column by convention (same as `DiffViewMode` / `ListViewMode`).
No `AppDbContext.OnModelCreating` change required.

## 2. Backend — EF migration

From the `GitBuddy.Domain` project (Npgsql provider):

```bash
dotnet ef migrations add AddMarkdownDiffMode
```

This generates `*_AddMarkdownDiffMode.cs` + `.Designer.cs` and updates
`AppDbContextModelSnapshot.cs`. Verify the generated `Up` uses
`migrationBuilder.AddColumn<string>(name: "MarkdownDiffMode", table: "UserPreferences",
nullable: false, defaultValue: "rendered")`. If the default is missing, hand-edit the
migration before applying.

Apply: `dotnet ef database update` (migrations are auto-applied on startup too).

## 3. Backend — DTOs

**File:** `GitBuddy.Api/DTOs/UserPreferencesDtos.cs`

- `UserPreferencesDto` record — add positional parameter: `string MarkdownDiffMode`
  (place it consistently with the other fields).
- `UpdatePreferencesRequest` record — add nullable positional parameter:
  `string? MarkdownDiffMode`.

## 4. Backend — UserService wiring

**File:** `GitBuddy.Api/Services/UserService.cs`

The DTO mapping is **inlined** here (not in `MappingExtensions.cs`). Update all three spots:

1. **Default-row creation** in `GetPreferencesAsync` (the `new UserPreferences { ... }`
   block around lines 39–47): add `MarkdownDiffMode = "rendered"` (matches the model
   default; explicit for clarity).
2. **First DTO construction** in `GetPreferencesAsync` (the `new UserPreferencesDto(...)`
   around lines 52–65): pass through `MarkdownDiffMode: preferences.MarkdownDiffMode`.
3. **Patch application** in `UpdatePreferencesAsync` (the null-check block around lines
   79–113): add
   ```csharp
   if (request.MarkdownDiffMode is not null)
       preferences.MarkdownDiffMode = request.MarkdownDiffMode;
   ```
4. **Second DTO construction** in `UpdatePreferencesAsync` (the `new UserPreferencesDto(...)`
   around lines 118–131): pass through `MarkdownDiffMode: preferences.MarkdownDiffMode`.

No controller change — `UserPreferencesController` passes the DTO through unchanged.

## 5. Frontend — types

**File:** `gitbuddy-vue/src/types/index.ts`

Add to `UserPreferences` interface (around line 353, next to `diffViewMode`):

```ts
markdownDiffMode?: 'rendered' | 'source';
```

## 6. Frontend — preference composable

**File:** `gitbuddy-vue/src/composables/useUserPreferences.ts`

- Add `markdownDiffMode: 'rendered'` to the default `preferences` ref (around line 5–17).
- Add a setter mirroring `setDiffViewMode` (around line 83):

  ```ts
  const setMarkdownDiffMode = async (mode: 'rendered' | 'source') => {
    await updatePreference('markdownDiffMode', mode);
  };
  ```

- Export `setMarkdownDiffMode` from the returned object (around line 185).

No `apiService` change — `getUserPreferences` / `updateUserPreferences` already pass through
arbitrary fields.

## 7. Frontend utility — reconstruct new-file text from patch

**New file:** `gitbuddy-vue/src/utils/markdownDiffReconstruct.ts`

Exports:

- `isMarkdownFile(file: FileDiff): boolean` — `true` when `file.language === 'markdown'`,
  with a fallback extension check (`/\.(md|markdown|mdx)$/i`) on `file.path` for safety.
- `canReconstruct(file: FileDiff): boolean` — `true` when `file.patch` is present, non-empty,
  and the file is not purely deleted (`file.status !== 'deleted'`). Used to decide fallback
  to Source view.
- `reconstructNewFileText(hunks: DiffHunk[]): { text: string; addedLineNumbers: Set<number> }`
  — walks each hunk's `lines` in order; for `context` and `add` lines, appends
  `line.content` to the output and tracks the line's `newLineNumber` (for `add` lines, into
  `addedLineNumbers`; for `context` lines, the line is present but not flagged). Skips
  `delete` lines entirely (they aren't part of the new file). Joins with `\n`. Sparse hunks
  (with gaps between them) reconstruct faithfully because `parsePatch` already numbers
  context lines and the new version is just the context+add lines concatenated.

  **Truncation note:** GitHub truncates very large patches; if the patch is truncated the
  reconstructed text will be partial. `canReconstruct` doesn't detect this directly — the
  rendered view simply shows what the patch contains. Acceptable for v1.

## 8. Frontend utility — render markdown with added-line highlights

**New file:** `gitbuddy-vue/src/utils/renderMarkdownDiff.ts`

Exports:

- `renderMarkdownWithAddedHighlights(text: string, addedLineNumbers: Set<number>): string`

Strategy (line-attribution via sentinel):

1. Split `text` into source lines. Build a parallel boolean array `isAdded[i]` from
   `addedLineNumbers` (1-indexed source line → flag). For reconstructed text, source line
   N maps to `newLineNumber === N`.
2. Tag added lines with a zero-width sentinel prefix (e.g. `\x00ADD\x00`) before parsing.
   Non-added lines are left untouched.
3. Parse the tagged text with `marked` (`gfm: true, breaks: true`, matching
   `useProxiedHtml`).
4. Post-process the resulting HTML: any block-level element whose text contains the
   sentinel gets wrapped: replace the sentinel, add class `md-added-block` to the element
   (paragraph/`<li>`/`<pre><code>`/heading). For inline runs where only part of a block was
   added, wrap the added run in `<span class="md-added">`.
5. Strip any leftover sentinels.

Refine the token/HTML walk during implementation — the key invariant is: **added source
lines produce visually highlighted spans/blocks in the rendered output**, colored with the
emerald palette.

- `escapeHtml` helper reused from `diffHelpers` if exported, else a local copy.

## 9. Frontend component — MarkdownDiffViewer.vue

**New file:** `gitbuddy-vue/src/components/MarkdownDiffViewer.vue`

- `<script setup lang="ts">`.
- Props: `file: FileDiff`. (v1 is read-only — no comment props needed. Keep the prop list
  minimal; comment/thread props can be added in a follow-up that brings inline commenting
  to the rendered view.)
- Computes once (memoized on `file.patch`):
  - `hunks = parsePatch(file.patch)`
  - `{ text, addedLineNumbers } = reconstructNewFileText(hunks)`
  - `html = renderMarkdownWithAddedHighlights(text, addedLineNumbers)`
- Template: a single `<div class="prose prose-invert prose-sm max-w-none p-4" v-html="html">`.
- Empty state: if `!file.patch` or `text` is empty, show the same "No diff content available"
  panel as `FileDiffViewer.vue` (slate icon + text).
- No loading state needed (synchronous), but compute lazily — the parent only mounts this
  component when expanded and in rendered mode.
- Scoped styles for `.md-added` (inline: `bg-emerald-500/15 text-emerald-200 rounded px-0.5`)
  and `:deep(.md-added-block)` (block: `bg-emerald-500/5 border-l-2 border-emerald-500/50
  rounded-r pl-3 py-1 my-1`). Reuse the existing emerald-addition palette.

## 10. Frontend — toggle + integration in FileDiffViewer.vue

**File:** `gitbuddy-vue/src/components/FileDiffViewer.vue`

1. **Import** `MarkdownDiffViewer`, `isMarkdownFile`, `canReconstruct`. Pull
   `setMarkdownDiffMode` from `useUserPreferences()` alongside the existing `preferences`.
2. **Computed** `effectiveMode`:
   ```ts
   const markdownModePref = computed(() => preferences.value.markdownDiffMode ?? 'rendered');
   const showMarkdownToggle = computed(() => isMarkdownFile(props.file) && canReconstruct(props.file));
   const effectiveMode = computed<'source' | 'rendered'>(() =>
     showMarkdownToggle.value && markdownModePref.value === 'rendered' ? 'rendered' : 'source'
   );
   ```
3. **Toggle handler:**
   ```ts
   const toggleMarkdownMode = () => {
     void setMarkdownDiffMode(effectiveMode.value === 'rendered' ? 'source' : 'rendered');
   };
   ```
4. **Template** — add the segmented toggle. Place it inside the existing header row
   (`FileDiffHeader` is a separate component; the toggle is added as a sibling strip below
   the header, above the diff body, only when `showMarkdownToggle` is true). Mirror
   `ReviewTimeline.vue` styling:
   ```html
   <div v-if="showMarkdownToggle && expanded && !loading"
        class="flex items-center gap-1 px-3 py-1.5 bg-slate-900 border-b border-slate-800 text-xs">
     <button @click="effectiveMode !== 'source' && toggleMarkdownMode()"
             :class="effectiveMode === 'source' ? 'bg-slate-700 text-slate-100' : 'text-slate-400 hover:text-slate-200'"
             class="px-2 py-0.5 rounded transition-colors">Source</button>
     <button @click="effectiveMode !== 'rendered' && toggleMarkdownMode()"
             :class="effectiveMode === 'rendered' ? 'bg-slate-700 text-slate-100' : 'text-slate-400 hover:text-slate-200'"
             class="px-2 py-0.5 rounded transition-colors">Rendered</button>
   </div>
   ```
5. **Wrap the existing split-view** `<template v-if="hunks.length > 0">` block in
   `v-if="effectiveMode === 'source'"`, and add `<MarkdownDiffViewer v-else :file="file" />`
   as a sibling. Keep the empty-state (`hunks.length === 0`) panel outside this branch so it
   shows in both modes.
6. When `showMarkdownToggle` is false (non-markdown file, or un-reconstructable patch), the
   toggle is hidden and `effectiveMode` resolves to `'source'` — existing behavior is
   unchanged.

## 11. Validation pass

See `validation.md`. Run both builds, apply the migration, and walk the manual checklist.
