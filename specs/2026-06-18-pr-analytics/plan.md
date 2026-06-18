# Plan: PR Analytics Dashboard

## 1. Backend — AnalyticsService

Create `Graphite.Api/Services/IAnalyticsService.cs`:

```csharp
public interface IAnalyticsService
{
    Task<ThroughputAnalytics> GetThroughputAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<ReviewerAnalytics> GetReviewerStatsAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<HealthAnalytics> GetHealthAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
```

Create `Graphite.Api/Services/AnalyticsService.cs`:
- Constructor injects `AppDbContext`.
- Constants: `StaleThresholdDays = 14`, `MaxStalePRs = 50`.
- Helper `DateTime NormalizeToUtc(DateTime)` — if `Kind == Unspecified`, treat as UTC.
- Helper `double? Median(IEnumerable<double?> values)` — order, pick middle (or average of two middles for even N); returns `null` for empty input. Frontend will serialize `null` and render "—".
- All queries use `.AsNoTracking()`.

### 1.1 GetThroughputAsync
- Query `PullRequests` where `CreatedAt >= from && CreatedAt <= to` (or `<= to.AddDays(1)` for inclusive end of day). Group by `CreatedAt.Date`. Materialize, then build `openedDaily`.
- Query merged PRs where `MergedAt != null && MergedAt >= from && MergedAt <= to`. Same bucketing for `mergedDaily`.
- For medians: load `(CreatedAt, MergedAt)` pairs in window where `MergedAt != null`, compute `(MergedAt - CreatedAt).TotalHours` for each; median.
- For time-to-first-review: for each PR opened in window, find earliest `Review.SubmittedAt` where `State != "PENDING"`; compute `(firstReview - CreatedAt).TotalHours`. Median.

### 1.2 GetReviewerStatsAsync
- Query `Reviews` where `SubmittedAt >= from && SubmittedAt <= to` and `State != "PENDING"`.
- Group by `Reviewer` (string) — also project `ReviewerAvatar` (first non-null).
- Per group: `totalReviews = Count()`, `approvals = Count(State == "APPROVED")`, `changesRequested = Count(State == "CHANGES_REQUESTED")`, `comments = Sum(Review.Comments)` if tracked, else count `Comments` on the PR... actually the simplest is to count `Reviews` of state `COMMENTED` for the comments field. Revisit in implementation: use `state == "COMMENTED"` count as the comment proxy to avoid cross-table joins. Document this in the response.
- For `medianReviewLatencyHours`: for each review in window, find the PR's `CreatedAt` and compute `(SubmittedAt - PR.CreatedAt).TotalHours`. Group per reviewer, median per group.

### 1.3 GetHealthAsync
- `totalOpenPRs`: `Count(pr => pr.Status != "Closed" && pr.Status != "Merged")` — verify the actual open-state convention against `PullRequestStatusService` enum; if needed use `!pr.IsMerged && string.IsNullOrEmpty(pr.MergedAt)`.
- `stalePRs`: open PRs where `UpdatedAt < DateTime.UtcNow.AddDays(-14)`, ordered by `UpdatedAt` asc, take 50, project `{ Id, Title, UpdatedAt, DaysStale = (UtcNow - UpdatedAt).Days }`.
- `stuckInReviewCount`: open PRs with `Status == "ChangesRequested"` (or the domain enum equivalent).
- `failingChecksCount`: open PRs where any `CheckRun.Conclusion == "FAILURE"` (or status==completed && conclusion in failure set) — query via `Any` subquery on `CheckRuns` collection.
- `unresolvedThreadsCount`: `ReviewThreads.Count(rt => !rt.IsResolved && !rt.IsOutdated)` — window param ignored.

## 2. Backend — DTOs

Define response DTOs as POCOs in `Graphite.Api/DTOs/Analytics/` (or inline in the service file — follow the dominant convention in `DTOs/`). Names: `ThroughputAnalytics`, `DailyCount`, `ReviewerAnalytics`, `ReviewerStat`, `HealthAnalytics`, `StalePR`.

## 3. Backend — AdminController routes

Edit `Graphite.Api/Controllers/AdminController.cs`:
- Add `IAnalyticsService analyticsService` to the primary constructor params.
- Add three routes:

```csharp
[HttpGet("analytics/throughput")]
public async Task<IActionResult> GetThroughput([FromQuery] DateTime? from, [FromQuery] DateTime? to)
{
    var (f, t) = NormalizeWindow(from, to);
    return Ok(await analyticsService.GetThroughputAsync(f, t));
}
```

(Repeat for `reviewers` and `health`.)

- Private helper `NormalizeWindow(DateTime? from, DateTime? to)` returns `(DateTime from, DateTime to)` with defaults: `to ??= DateTime.UtcNow; from ??= to.AddDays(-30)`. Coerce `from`/`to` to UTC if `Kind == Unspecified`.

## 4. Backend — DI registration

Edit `Graphite.Api/Program.cs` near the other `AddScoped` lines (around line 60-80):

```csharp
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
```

## 5. Backend — build & sanity check

`dotnet build -c Release` from repo root. Fix any compile errors before moving to frontend.

## 6. Frontend — install dependencies

```bash
cd graphite-vue
npm install chart.js vue-chartjs
```

Both go into `dependencies` in `package.json`.

## 7. Frontend — types

Create `graphite-vue/src/types/analytics.ts` exporting TypeScript interfaces matching each response DTO: `ThroughputAnalytics`, `DailyCount`, `ReviewerAnalytics`, `ReviewerStat`, `HealthAnalytics`, `StalePR`. Also export `AnalyticsPreset = '7d' | '30d' | '90d' | 'all'`.

## 8. Frontend — API client

Edit `graphite-vue/src/services/api.ts` — add three methods following existing axios conventions:

```ts
export const analyticsApi = {
  getThroughput: (from?: string, to?: string) =>
    api.get<ThroughputAnalytics>('/api/admin/analytics/throughput', { params: { from, to } }),
  getReviewerStats: (from?: string, to?: string) =>
    api.get<ReviewerAnalytics>('/api/admin/analytics/reviewers', { params: { from, to } }),
  getHealth: (from?: string, to?: string) =>
    api.get<HealthAnalytics>('/api/admin/analytics/health', { params: { from, to } }),
};
```

## 9. Frontend — composable

Create `graphite-vue/src/composables/useAnalytics.ts`:
- State: `preset`, `from`, `to`, `throughput`, `reviewers`, `health`, `loading`, `error`.
- `setPreset(p)`: computes `from`/`to` from preset (e.g., 7d → `to = today`, `from = today - 7 days`).
- `setCustomRange(from, to)`: sets preset to `custom` and applies.
- `refresh()`: fires all three API calls in parallel via `Promise.allSettled`, aggregates errors.
- Watches `from` + `to` and re-fetches on change.
- Initial state: `preset = '30d'`, triggers first fetch on mount.

## 10. Frontend — components

Create `graphite-vue/src/components/analytics/`:

- `DateRangeSelector.vue` — props: `preset`, `from`, `to`; emits `update:preset`, `update:from`, `update:to`, `apply`. Layout: four preset buttons + two date inputs + Apply button.
- `ThroughputSection.vue` — props: `data: ThroughputAnalytics | null`. Renders KPI cards (total opened, total merged, median TTM, median TTFR) and a `<Line>` chart (vue-chartjs) of `openedDaily` vs `mergedDaily`.
- `ReviewerSection.vue` — props: `data: ReviewerAnalytics | null`. Renders a table sorted by `totalReviews` desc with avatar, username, all counts, median latency.
- `HealthSection.vue` — props: `data: HealthAnalytics | null`. Renders KPI cards (stuck, failing, unresolved threads, total open) + table of stale PRs linking to `/#/pr/<id>` (verify existing PR detail route name).
- `AnalyticsTab.vue` — uses `useAnalytics`, lays out `<DateRangeSelector>` + the three sections in a vertical stack with loading and error states.

## 11. Frontend — wire into AdminPanel

Edit `graphite-vue/src/views/AdminPanel.vue`:
- Add to the `tabs` array: `{ id: 'analytics', label: 'Analytics' }`.
- Add lazy import: `const AnalyticsTab = defineAsyncComponent(() => import('@/components/analytics/AnalyticsTab.vue'))`.
- Add `<div v-else-if="activeTab === 'analytics'"><AnalyticsTab /></div>` to the panel body.

## 12. Validation

Run through `validation.md`.
