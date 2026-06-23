# Plan — PR Analytics author filter

Implementation order is full-stack top-to-bottom. Each task group is independently shippable but later groups depend on earlier ones.

## 1. Backend DTOs

File: [GitBuddy.Api/DTOs/Analytics/AnalyticsDtos.cs](../../../GitBuddy.Api/DTOs/Analytics/AnalyticsDtos.cs)

1. Extend `ReviewerAnalytics` record with a second field:
   ```csharp
   public record ReviewerAnalytics(
       IReadOnlyList<ReviewerStat> Reviewers,
       IReadOnlyList<AuthorOption> Authors);
   ```
2. Add new record:
   ```csharp
   public record AuthorOption(string Username, string? AvatarUrl);
   ```

## 2. Backend service — `AnalyticsService`

File: [GitBuddy.Api/Services/AnalyticsService.cs](../../../GitBuddy.Api/Services/AnalyticsService.cs)

Also update `IAnalyticsService` interface in the same folder.

1. Add a `string[]? authors` parameter (last position, before `CancellationToken`) to:
   - `GetThroughputAsync`
   - `GetReviewerStatsAsync`
   - `GetHealthAsync`

2. Add a private helper that normalizes the input:
   ```csharp
   private static string[]? NormalizeAuthors(string[]? authors)
   {
       if (authors is null || authors.Length == 0) return null;
       var set = authors
           .Where(a => !string.IsNullOrWhiteSpace(a))
           .Select(a => a.Trim())
           .Distinct(StringComparer.OrdinalIgnoreCase)
           .ToArray();
       return set.Length == 0 ? null : set;
   }
   ```

3. **`GetThroughputAsync`**:
   - Normalize `authors` once at the top.
   - Add `.Where(pr => authors == null || authors.Contains(pr.Author))` to:
     - `openedPRs` query (currently line 16)
     - `mergedPRs` query (currently line 27)
   - The earliest-review subquery (currently line 46) is already scoped by `openedIds.Contains(r.PullRequestId)`, which derives from the now-filtered `openedPRs` — no change needed.

4. **`GetReviewerStatsAsync`**:
   - Normalize `authors`.
   - `reviews` query (currently line 75): add `&& (authors == null || authors.Contains(r.PullRequest.Author))` so reviewer counts reflect reviews given on the selected authors' PRs.
   - `authoredRaw` query (currently line 90): add the same author filter. When `authors` is set, this trivially returns only the selected authors' PRs.
   - Add a new query to populate the dropdown (run **without** the author filter, just the date window):
     ```csharp
     var windowAuthors = await db.PullRequests.AsNoTracking()
         .Where(pr => pr.CreatedAt >= utcFrom && pr.CreatedAt <= utcTo)
         .Select(pr => new { pr.Author, pr.AuthorAvatar })
         .ToListAsync(ct);
     var authorOptions = windowAuthors
         .GroupBy(a => a.Author)
         .Select(g => new AuthorOption(
             g.Key,
             g.FirstOrDefault(x => !string.IsNullOrEmpty(x.AuthorAvatar))?.AuthorAvatar))
         .OrderBy(a => a.Username, StringComparer.OrdinalIgnoreCase)
         .ToList();
     ```
   - Return `new ReviewerAnalytics(stats, authorOptions)`.

5. **`GetHealthAsync`**:
   - Normalize `authors`.
   - Apply the filter predicate to every `PullRequests` query:
     - stale (currently line 142)
     - stuckCount (currently line 153)
     - failingCount (currently line 156)
     - totalOpen (currently line 164)
   - For `unresolvedCount` (currently line 161) join to PR:
     ```csharp
     .CountAsync(rt => !rt.IsResolved && !rt.IsOutdated
         && (authors == null || authors.Contains(rt.PullRequest.Author)), ct);
     ```
     Verify the `ReviewThread → PullRequest` navigation exists. If not, add an explicit join on `PullRequestId`.

## 3. Backend controller

File: [GitBuddy.Api/Controllers/AdminController.cs](../../../GitBuddy.Api/Controllers/AdminController.cs) (~lines 201–232)

1. Add `[FromQuery] string[]? authors` to each of the three analytics actions.
2. Forward `authors` as the third positional argument to the matching service call.

Example:
```csharp
[HttpGet("analytics/throughput")]
public async Task<IActionResult> GetAnalyticsThroughput(
    [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string[]? authors)
{
    var (f, t) = NormalizeWindow(from, to);
    var result = await analyticsService.GetThroughputAsync(f, t, authors, HttpContext.RequestAborted);
    return Ok(result);
}
```

## 4. Frontend API client

File: [gitbuddy-vue/src/services/api.ts](../../../gitbuddy-vue/src/services/api.ts)

1. Update each of the three analytics methods to accept `authors?: string[]` and forward via axios `params`:
   ```ts
   getAnalyticsThroughput(from?: string, to?: string, authors?: string[]) {
     return api.get<ThroughputAnalytics>('/admin/analytics/throughput', {
       params: { from, to, authors },
     }).then(r => r.data);
   }
   ```
2. **Verify serialization** — ensure the configured `paramsSerializer` produces repeated `?authors=a&authors=b` (axios default). If a custom serializer is set to comma-join, either change it or pass `{ paramsSerializer: { indexes: null } }` per-call.

## 5. Frontend types

File: [gitbuddy-vue/src/types/index.ts](../../../gitbuddy-vue/src/types/index.ts)

1. Add `AuthorOption`:
   ```ts
   export interface AuthorOption {
     username: string;
     avatarUrl?: string;
   }
   ```
2. Extend `ReviewerAnalytics`:
   ```ts
   export interface ReviewerAnalytics {
     reviewers: ReviewerStat[];
     authors: AuthorOption[];   // NEW
   }
   ```

## 6. Frontend composable

File: [gitbuddy-vue/src/composables/useAnalytics.ts](../../../gitbuddy-vue/src/composables/useAnalytics.ts)

1. Add module-level state alongside `preset`:
   ```ts
   const selectedAuthors = ref<string[]>([]);
   ```
2. Expose a computed for the dropdown source:
   ```ts
   const availableAuthors = computed<AuthorOption[]>(
     () => reviewers.value?.authors ?? []
   );
   ```
3. In `refresh()`, pass `selectedAuthors.value` to all three `apiService` calls.
4. Extend the `watch` to also react to `selectedAuthors`:
   ```ts
   watch([activeRange, selectedAuthors], () => { void refresh(); }, { deep: true });
   ```
5. Add and export:
   ```ts
   function setSelectedAuthors(next: string[]) { selectedAuthors.value = next; }
   ```
6. Export `selectedAuthors`, `availableAuthors`, `setSelectedAuthors` from `useAnalytics()`.

## 7. New component `UserSelector.vue`

File: `gitbuddy-vue/src/components/analytics/UserSelector.vue` (new)

Props:
```ts
defineProps<{
  available: AuthorOption[];
  selected: string[];
  loading?: boolean;
}>();
defineEmits<{
  'update:selected': [next: string[]];
}>();
```

UI:
- Trigger button: shows `All users` when empty, `<n> selected` (or stacked avatar chips) otherwise. Dense, border-slate-800, text-slate-300 — matches `DateRangeSelector` styling.
- Dropdown panel:
  - Search input (filter by username, case-insensitive).
  - Checkbox list of `available`, each row: avatar + username + check.
  - Click row toggles membership in `selected` and emits `update:selected`.
  - Optional "Clear all" footer button when anything is selected.
- Click-outside closes the panel (use a `v-click-outside` directive or `@click.stop` + window listener — match whatever pattern already exists in the codebase).
- Keyboard: Escape closes; Enter toggles highlighted row. Mirror `SearchableDropdown.vue` if its keyboard code is easy to lift.

Keep it under ~200 lines. No new dependencies.

## 8. Wire into `AnalyticsTab.vue`

File: [gitbuddy-vue/src/components/analytics/AnalyticsTab.vue](../../../gitbuddy-vue/src/components/analytics/AnalyticsTab.vue)

1. Import `UserSelector`.
2. Pull `selectedAuthors`, `availableAuthors`, `setSelectedAuthors` out of `useAnalytics()`.
3. Render the selector right after `<DateRangeSelector>`:
   ```vue
   <UserSelector
     :available="availableAuthors"
     :selected="selectedAuthors"
     :loading="loading"
     @update:selected="setSelectedAuthors"
   />
   ```

## 9. Validation pass

Walk through [validation.md](./validation.md). Fix anything red before opening a PR.

## 10. Branch + commit

```
git checkout master && git pull
git checkout -b feature/pr-analytics-author-filter
```

Commit per task group (or in two-three logical commits: backend, frontend wiring, component). Do not push or open a PR without explicit user request.
