# Validation: PR Analytics Dashboard

## Automated

- `dotnet build -c Release` from repo root — passes with no warnings or errors.
- `cd graphite-vue && npm run build` — passes with no TypeScript or Vue compile errors.
- No EF migration needed (no schema changes). Skip `dotnet ef database update`.

## Correctness

- All three analytics endpoints inherit `[Authorize(Roles = "Admin")]` from `AdminController` — non-admins get 403.
- All read queries use `.AsNoTracking()`.
- `from` / `to` query params normalized to UTC.
- Empty window (no PRs opened/merged in range) returns `medianTimeToMergeHours: null` / `medianTimeToFirstReviewHours: null` and empty arrays — does not throw.
- Single PR in window: median equals that PR's value.
- Reviewer with zero reviews in window excluded from response.
- `stalePRs` capped at 50, ordered by `daysStale` desc.

## Manual walkthrough

1. Start backend: `cd Graphite.Api && dotnet run`.
2. Start frontend: `cd graphite-vue && npm run dev`.
3. Sign in as an admin user.
4. Navigate to `/admin` → click the **Analytics** tab.
5. Confirm the three sections render with last-30-day defaults:
   - Throughput line chart shows open/merge trends over the last 30 days.
   - KPI cards show non-zero totals (assuming there's PR data) or "—" for medians when no merged PRs in window.
   - Reviewers table lists reviewers active in the window.
   - Health section shows current counts and a stale-PR list (possibly empty).
6. Click the **7d** preset — chart and tables re-fetch and re-render.
7. Click **All time** — confirm the daily series covers the full history.
8. Use the custom date pickers — pick a range known to contain a merged PR — confirm the median time-to-merge matches a manual calculation:
   ```sql
   SELECT julianday(MergedAt) - julianday(CreatedAt) FROM PullRequests WHERE MergedAt IS NOT NULL AND MergedAt BETWEEN '...' AND '...';
   ```
9. Sign out, sign in as a non-admin user — `GET /api/admin/analytics/throughput` returns 403 and the Analytics tab is not visible (the whole AdminPanel route is admin-gated).

## Edge cases

- Empty database (no PRs at all): all sections render with empty-state messaging, no crashes.
- Database with PRs but none in the 30-day window: throughput chart is empty, medians are `null` and rendered as "—".
- Reviewer with `null` `SubmittedAt`: excluded from reviewer stats (state PENDING reviews also excluded).
- Stale PR with `UpdatedAt == null` (shouldn't happen but defensive): treat as stale, `daysStale = 0`.

## Definition of done

- All automated checks pass.
- Manual walkthrough completes without errors.
- Admin-only access verified.
- Code committed on `feature/pr-analytics`; PR ready for review (or draft PR per workflow).
