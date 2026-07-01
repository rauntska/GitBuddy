# Requirements: Backend-Scheduled Branches-Without-PRs Refresh

## Scope

Move the GitHub fan-out for the **branches-without-prs** ("pending branches") view out of the per-user request path and into a single backend scheduled job. Today every dashboard client triggers ~60 GitHub REST calls per minute (list branches for each repo, fetch head commit for each candidate branch). With multiple users that exhausts the GitHub rate budget and eventually rate-limits the org.

The new design:
- A background worker authenticating with the **GitHub App installation token** fans out once per configurable interval, persists results to a new `BranchWithoutPR` table, and broadcasts updates.
- The user-facing endpoint becomes a cheap DB read.
- Webhooks keep the table fresh in near-real-time: opening a PR on a pending branch removes the row; creating or deleting a branch adds/removes the row.
- The frontend keeps its 60s poll (now cheap) and additionally reacts to SignalR push events for instant add/remove.

### In scope
- New domain entity `BranchWithoutPR` + EF migration.
- New `GitHubConfig.BranchRefreshIntervalMinutes` setting (independent of `RefreshIntervalMinutes`).
- New background worker `BranchWithoutPRRefreshService`.
- New `BranchWithoutPRService.RefreshAndPersistAsync` (reuses existing fan-out method).
- Rewrite of `RepositoriesController.GetBranchesWithoutPRs` to read from DB.
- New `POST /api/repositories/branches-without-prs/refresh` endpoint for manual trigger.
- Webhook hooks in `WebhookService`: PR-opened removes the row; branch-push (create/delete) adds/removes the row.
- Two new SignalR events: `PendingBranchResolved`, `PendingBranchAdded`.
- Frontend: drop `recentDays` param, add SignalR listeners, point manual refresh at the new endpoint.

### Out of scope
- Changing the frontend polling cadence or moving to a fully push-based model.
- Per-user / per-repo scoping of the pending-branches list (stays org-wide, same as today).
- Surfacing `LastRefreshedAt` in the UI (entity has it for diagnostics; not exposed to the user in this iteration).
- Historical tracking of branches over time (replace-on-refresh, not append-only).
- Adding the GitHub `create` / `delete` webhook subscriptions — the existing `push` event already carries `Created` / `Deleted` / `RefType`, which is sufficient.

### API contract

All routes `[Authorize]` (no role restriction, same as today).

**Unchanged:**
- `GET /api/repositories/branches-without-prs`
  - **Behavior change:** now reads from DB instead of fanning out to GitHub. Response shape unchanged.
  - **Contract change:** the `recentDays` query parameter is **ignored** (the recency cutoff is applied at refresh time by the worker using the value configured when the worker runs; see Decisions). Frontend callers should stop sending it.
  - Response (unchanged):
    ```json
    [
      {
        "owner": "octocat",
        "repo": "Hello-World",
        "repoFullName": "octocat/Hello-World",
        "branchName": "feature/cool-thing",
        "defaultBranch": "main",
        "lastActivityAt": "2026-06-28T14:22:01Z"
      }
    ]
    ```
  - Empty array `[]` when no pending branches are cached yet (e.g., before the first worker cycle completes). Frontend shows the existing empty state.

**New:**
- `POST /api/repositories/branches-without-prs/refresh`
  - Fire-and-forget trigger of an immediate worker cycle. Returns `202 Accepted` with an empty body.
  - Idempotent: if a refresh is already in flight, the call is a no-op.
  - Authenticated the same as the GET. Does **not** itself make GitHub calls — it just signals the worker.

### SignalR events (new)

Broadcast on `/hubs/pr` to `Clients.All`:

- `PendingBranchResolved` — `{ "repository": "Hello-World", "branchName": "feature/cool-thing" }`. Fired when a row is removed (PR opened on the branch, or branch deleted).
- `PendingBranchAdded` — `{ "owner": "...", "repo": "...", "repoFullName": "...", "branchName": "...", "defaultBranch": "...", "lastActivityAt": "..." | null }`. Fired when a row is inserted (branch created via push webhook).

## Decisions

| Decision | Choice | Why |
|---|---|---|
| Refresh interval source | New `GitHubConfig.BranchRefreshIntervalMinutes` (default 30) | Decouples branches-refresh cadence from the PR-refresh cadence. They have very different cost profiles. |
| Worker auth | GitHub App installation token via `IGitHubTokenService` | Same pattern as `PRRefreshService`. No per-user dependency. |
| Persistence strategy | **Replace** all rows each cycle in a single transaction | Simplest, idempotent, no row-by-row diffing. The table is always a fresh snapshot. |
| Webhook reaction timing | Immediate row mutation + SignalR push for both PR-opened and branch-push events | The user wants near-real-time. Webhook-driven rows get reconciled on the next refresh anyway, so worst case the table is briefly inconsistent (a row exists between webhook and next refresh even if the branch was deleted out-of-band) — acceptable. |
| Branch-create detection | Use the existing `push` webhook (`pushEvent.Created == true && pushEvent.RefType == Branch`) | No new webhook subscription needed — push events already arrive and are dispatched by `GitHubWebhookProcessor`. Octokit.Webhooks 3.2.1 exposes all required fields. |
| `recentDays` cutoff | Hardcoded to 7 inside the worker | The worker uses a fixed cutoff so the user-facing endpoint can stay parameterless. Today's default was 7 anyway; the recency filter just moves from request-time to refresh-time. |
| Frontend delivery | Keep 60s poll (now DB-backed) + SignalR listeners for instant add/remove | Minimum churn. The 60s poll becomes a cheap safety net. |
| Manual refresh | New `POST .../refresh` endpoint signals the worker | Keeps the UX of "refresh now" but routes through the worker so we don't pay the GitHub cost on demand. |
| `IsIgnoredBranch` filter | Reuse the static `IgnoredBranchNames` set + prefix rule from `BranchWithoutPRService` | Single source of truth. The webhook add-path applies the same filter so it doesn't insert rows that the next refresh would immediately drop. |

## Context

### Existing code to mirror
- Worker skeleton: `GitBuddy.Api/BackgroundServices/PRRefreshService.cs` — 10s initial delay, dynamic interval from `GitHubConfig`, 5-minute retry on error, `IServiceProvider` for scope creation. Copy this structure verbatim, swap the inner call.
- App token resolution: `IGitHubTokenService.GetAccessTokenAsync(GitHubConfig)` — already used by `GitHubService.GetOpenPullRequestsAsync`.
- Fan-out primitive: `BranchWithoutPRService.GetBranchesWithoutPRsAsync(organization, accessToken, openPRBranches, recentDays)` — keep as-is, call it from `RefreshAndPersistAsync`.
- SignalR broadcast shape: `SignalRNotificationService.BroadcastPRCreatedAsync(PRListUpdateDto)` — mirror for the two new events.
- Webhook dispatch: `GitBuddy.Api/Processors/GitHubWebhookProcessor.ProcessPushWebhookAsync` already routes push events to `WebhookService.HandlePushEventAsync`.

### Conventions to follow
- EF migration name format: `YYYYMMDDHHMMSS_PascalCaseDescription.cs` (EF generates the timestamp prefix automatically).
- Scoped service registration for new services: `builder.Services.AddScoped<...>()`.
- Hosted service registration: `builder.Services.AddHostedService<BranchWithoutPRRefreshService>();` next to the existing two workers (`Program.cs:89-91`).
- DbSet naming: plural (`BranchWithoutPRs`).
- All GitHub-calling code paths must go through `IGitHubService` / `IGitHubGraphQLService` / `IGitHubTokenService` — no raw Octokit clients.

### Open questions
- None blocking. If during implementation `HeadCommit.Timestamp` on the `PushEvent` turns out to be unreliable for the `lastActivityAt` field, fall back to a single `IGitHubService.GetCommitDateAsync` call per branch-create event.
