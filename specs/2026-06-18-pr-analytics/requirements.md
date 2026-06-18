# Requirements: PR Analytics Dashboard

## Scope

Admin-only analytics dashboard surfacing three views of PR data already captured by the Graphite domain:

1. **Throughput & cycle time** — daily counts of PRs opened and merged over the selected window; median time-to-merge and median time-to-first-review across the window.
2. **User stats** — per-user breakdown of PRs authored, reviews submitted (approved / changes-requested / commented), median review latency, and approval rate. Includes both reviewers and PR authors in the window.
3. **Health & bottlenecks** — current counts of stale PRs (open, no activity for 14+ days), PRs stuck-in-review, PRs blocked by failing checks, and unresolved review threads, plus a list of the stalest open PRs.

### API contract

All routes are admin-only (reuse `[Authorize(Roles = "Admin")]` already applied at `AdminController` level). All parameters optional; default window is last 30 days when both `from` and `to` are omitted.

- `GET /api/admin/analytics/throughput?from=YYYY-MM-DD&to=YYYY-MM-DD`
  ```json
  {
    "openedDaily": [{ "date": "2026-05-20", "count": 3 }],
    "mergedDaily": [{ "date": "2026-05-20", "count": 2 }],
    "medianTimeToMergeHours": 18.5,
    "medianTimeToFirstReviewHours": 4.2,
    "totalOpened": 47,
    "totalMerged": 39
  }
  ```

- `GET /api/admin/analytics/reviewers?from=YYYY-MM-DD&to=YYYY-MM-DD`
  ```json
  {
    "reviewers": [
      {
        "username": "octocat",
        "avatarUrl": "https://...",
        "totalReviews": 12,
        "totalPRsAuthored": 5,
        "approvals": 8,
        "changesRequested": 2,
        "comments": 23,
        "medianReviewLatencyHours": 6.1
      }
    ]
  }
  ```
  The user list is the union of reviewers and PR authors in the window. A user who only authored PRs (no completed reviews) still appears, with zero review counts and a `null` median latency. Sorted by combined activity (`totalReviews + totalPRsAuthored`) descending.

- `GET /api/admin/analytics/health?from=YYYY-MM-DD&to=YYYY-MM-DD`
  ```json
  {
    "stalePRs": [
      { "id": 123, "title": "...", "updatedAt": "2026-05-01T...", "daysStale": 21 }
    ],
    "stuckInReviewCount": 4,
    "failingChecksCount": 2,
    "unresolvedThreadsCount": 11,
    "totalOpenPRs": 38
  }
  ```
  `stalePRs` is capped at 50 entries ordered by `daysStale` desc. The count fields reflect the current state (not the window); the window is accepted for consistency but ignored for these counts.

### Frontend

- Top-level entry in the main sidebar (`AppSidebar.vue`) with a `ChartBarIcon`, linking to `/settings/analytics`. Admin-only — hidden from non-admins, and the route guard (`requiresAdmin: true`) bounces non-admins who hand-navigate.
- Also surfaced as a sub-menu item under Administration in the settings left rail (`SettingsNav.vue`), mirroring the pattern used by Administration itself (icon in main nav + link in settings sub-nav).
- Rendered by `SettingsPage.vue` via a lazy-loaded `AnalyticsPanel` wrapper. The settings container widens to `max-w-[1600px]` on this route (other settings pages keep `max-w-4xl`).
- Date range selector: preset buttons (`7d`, `30d`, `90d`, `All time`) plus two `<input type="date">` fields for custom range. **"All time" sends an explicit early `from` date** (currently `2000-01-01`) rather than omitting params — the backend's 30-day default would otherwise silently clip the result.
- Three sections render below the selector: throughput (line chart with gradient fills + KPI cards), user stats (table), health (KPI cards + stale PR list).
- Charts rendered with `chart.js` + `vue-chartjs`.

### Out of scope

- Per-user personalized analytics (every admin sees the same org-wide view).
- CSV/PDF export of analytics.
- Real-time SignalR push of analytics updates (point-in-time queries only; user re-runs by changing the window).
- Historical trend data after PRs close (would require a materialized snapshot table; explicitly deferred).
- Non-admin access.

## Decisions

- **Server-side aggregation**: a new `AnalyticsService` computes everything via LINQ on `AppDbContext`. No client-side aggregation of the dashboard payload.
- **No new domain entities or migrations**: analytics read directly from existing `PullRequest`, `Review`, `ReviewThread`, `Comment`, `CheckRun` tables.
- **Chart.js + vue-chartjs** as the visualization library (approved as a new frontend dependency).
- **Admin-only**: reuses `AdminController`'s existing role policy. No new authorization policy or role.
- **Medians via sort**: small N per window, so median is computed by `OrderBy` + middle element. No percentile library. Empty input returns `null`, rendered as `—` in the UI.
- **UTC dates**: `from`/`to` parsed as UTC dates; daily buckets keyed by `DateTime.UtcNow.Date`.
- **Stale threshold**: 14 days without update (configurable later via `IOptions<AnalyticsOptions>` if needed; for now, a constant).
- **State string casing**: review states and PR status are matched using the domain's stored PascalCase form — `"Approved"`, `"ChangesRequested"`, `"Commented"` for `Review.State`; `"Closed"`, `"Merged"`, `"ChangesRequested"` for `PullRequest.Status`. PENDING reviews are always excluded. Check runs use lowercase GitHub form: `Status == "completed"`, `Conclusion == "failure"`.
- **Composable state is module-level**: `useAnalytics()` shares its state across callers (hero refresh button and tab body both bind to the same refs) rather than per-call instance state.
- **Endpoint name retained**: the URL stays `/api/admin/analytics/reviewers` and the client method stays `getAnalyticsReviewers` for churn reduction, even though the section is now broader "User Stats" including authored PR counts.

## Context

- Mirror the `AdminController` aggregation pattern (`GET /api/admin/stats` at `AdminController.cs:182`).
- Register the new service scoped in `Program.cs` following the existing `AddScoped<I*Service, *Service>()` style.
- API client additions go in `graphite-vue/src/services/api.ts` next to existing methods.
- Use `.AsNoTracking()` on all read queries (recent repo convention — see commit `0932dda`).
- The `nul` and `Graphite.Api/nul` files in the working tree are unrelated Windows-reserved-name artifacts from prior work; leave them alone.

## Open questions

None — all design decisions resolved during the planning interview.
