# Validation: Backend-Scheduled Branches-Without-PRs Refresh

## Automated checks

- From repo root: `dotnet build -c Release` — must succeed with no errors.
- From `gitbuddy-vue/`: `npm run build` — must succeed with no errors.
- From `GitBuddy.Api/`: `dotnet ef migrations script --idempotent` — must render SQL that creates the `BranchWithoutPRs` table, its two indexes, and adds `BranchRefreshIntervalMinutes` to `GitHubConfigs` with a default of 30.
- `dotnet ef database update` — applies cleanly on a fresh `gitbuddy.db`.
- `dotnet test` — if tests exist for any touched service (none today, but if added during implementation they must pass).

## Correctness checks

- `GET /api/repositories/branches-without-prs`:
  - `[Authorize]` attribute present.
  - Returns `200` with an array (possibly empty before first worker cycle).
  - **Does not** call GitHub — verify by setting a breakpoint in `BranchWithoutPRService.GetBranchesWithoutPRsAsync` and confirming it is not hit during this request.
  - **Does not** require `validationService.GetRequiredUserWithTokenAsync` anymore (no user-PAT dependency).
- `POST /api/repositories/branches-without-prs/refresh`:
  - `[Authorize]` attribute present.
  - Returns `202 Accepted` with empty body.
  - Calling it twice in quick succession does not enqueue two cycles (idempotent — second call is a no-op if one is in flight).
- `BranchWithoutPRRefreshService`:
  - Registered in `Program.cs` via `AddHostedService`.
  - Authenticates via `IGitHubTokenService.GetAccessTokenAsync(config)` — not via per-user tokens.
  - Reads interval from `GitHubConfig.BranchRefreshIntervalMinutes`; falls back to 30 if missing/zero/outside `[5, 1440]`.
  - Logs "Branches-without-PR refresh completed" after each successful cycle.
  - On exception: logs the error and retries after 5 minutes (not the full interval).
- Webhook `pull_request` `opened` / `reopened`:
  - After `CreateNewPrAsync`, matching `BranchWithoutPR` rows are deleted and `PendingBranchResolved` is broadcast.
- Webhook `push` with `RefType == Branch`:
  - `Created == true` (non-ignored, non-default branch): row inserted, `PendingBranchAdded` broadcast.
  - `Deleted == true`: row deleted, `PendingBranchResolved` broadcast.
  - Existing PR-update logic in `HandlePushEventAsync` still runs after the new block.
- EF migration:
  - `BranchWithoutPRs` table exists with columns: `Id`, `Owner`, `Repo`, `RepoFullName`, `BranchName`, `DefaultBranch`, `LastActivityAt`, `LastRefreshedAt`.
  - Composite index `(Repo, BranchName)` present.
  - Index on `LastRefreshedAt` present.
  - `GitHubConfigs` has new `BranchRefreshIntervalMinutes` column with default 30.

## Manual walkthrough

Prerequisites: API running locally (`dotnet run` in `GitBuddy.Api/`), frontend running (`npm run dev` in `gitbuddy-vue/`), GitHub App configured and installed in the target org, push/pull-request webhooks delivering to `/api/webhooks/github`.

### Path 1 — worker refreshes on schedule

1. Tail the API logs.
2. On startup, within ~10 seconds, observe "Branches-without-PR refresh is starting" and then "Branches-without-PR refresh completed".
3. Open `http://localhost:5173/` and load the dashboard.
4. Confirm the "Branches without PRs" section shows rows. (If empty, verify there are unignored, non-default branches in the org with no open PR.)
5. Wait for the next 60s frontend poll. Confirm the section does not flicker and no GitHub calls fire from the GET endpoint.

### Path 2 — PR opened on a pending branch

1. Note a row currently shown in the section (e.g., `octocat/Hello-World` `feature/cool-thing`).
2. From GitHub, open a PR from `feature/cool-thing` against `main`.
3. Within seconds (webhook delivery time), the row should disappear from the dashboard without waiting for the 60s tick — `PendingBranchResolved` SignalR event.
4. Verify in DB: `SELECT * FROM BranchWithoutPRs WHERE Repo = 'Hello-World' AND BranchName = 'feature/cool-thing'` returns 0 rows.

### Path 3 — new branch pushed

1. From GitHub (or local git push), create a new branch `feature/webhook-add-test` on a tracked repo.
2. Within seconds, the row should appear in the dashboard — `PendingBranchAdded` SignalR event.
3. Verify in DB: row exists with `LastRefreshedAt` close to `now`.

### Path 4 — branch deleted

1. Delete the branch from Path 3.
2. Within seconds, the row should disappear — `PendingBranchResolved`.
3. Verify in DB: row removed.

### Path 5 — ignored branch push

1. Push a branch named `develop` or `release/1.0` to a tracked repo.
2. Confirm no row appears (filter applies on the webhook add-path).

### Path 6 — manual refresh

1. Click the manual refresh button in the section header.
2. The POST returns 202 quickly.
3. Within seconds, a worker cycle runs and the section is updated (next poll picks up the fresh rows; the button can show a transient "refreshing…" state).

### Path 7 — interval config

1. Update `GitHubConfig.BranchRefreshIntervalMinutes` (via the existing admin settings surface) to `5`.
2. Observe the worker now cycles every ~5 minutes (logs).

## Edge cases

- **First startup / empty DB:** GET returns `[]` until the first worker cycle completes. Frontend empty state must render cleanly.
- **Worker GitHub failure (rate limit, network):** logged, retried after 5 minutes, table contents remain from the last successful cycle (not wiped).
- **Concurrent refresh trigger spam:** many rapid POSTs to `/refresh` do not enqueue many cycles — the trigger is idempotent.
- **Webhook arrives during an in-flight refresh:** `AddBranchAsync` / `RemoveBranchAsync` run in their own transactions. The next refresh cycle reconciles everything. A row inserted by webhook mid-cycle may be wiped by the refresh's `ExecuteDelete` — acceptable, the next cycle or webhook event restores it.
- **Webhook for a repo outside the configured org:** filter applies on `config.Organization` only at refresh time. Webhook add-path does not filter by org (we trust GitHub only delivers subscribed events) — if the App is installed across multiple orgs this may surface rows that the next refresh would drop. Acceptable; documented in `requirements.md`.

## Definition of done

- All automated checks pass.
- All correctness checks pass.
- Manual walkthrough paths 1–7 verified end-to-end on the local environment.
- No per-request GitHub calls from `GET /api/repositories/branches-without-prs` (verified by breakpoint / log inspection).
- One GitHub fan-out per `BranchRefreshIntervalMinutes` cycle (plus minor per-event calls on webhook add/delete).
- PR-opened and branch-push/delete webhooks keep the table and connected clients in sync in near-real-time.
- Migration applies cleanly and the app starts up without manual intervention.
