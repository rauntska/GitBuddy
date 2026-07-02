# Branches-Without-PRs: Backend-Scheduled Refresh

> **Status: Implemented** — see commit history and `specs/2026-07-01-branches-without-prs-backend-refresh/` for the execution spec that produced this feature.

The "branches without PRs" view (pending branches across the configured org) used to be a per-user, on-demand query. Every dashboard client fanned out to GitHub live every 60 seconds. With multiple users, that burned the GitHub rate budget fast. This doc describes the backend-scheduled design that replaced it.

---

## 1. Why

- **Rate-limit pressure.** Each dashboard client triggered ~60 GitHub REST calls per minute (1 branch-list call per repo + 1 commit-lookup per candidate branch). Multiplied by concurrent users, the org risked hitting GitHub's rate limit.
- **Per-user cost.** Each call used the user's OAuth PAT — but the data is identical for every user looking at the same org.
- **Latency.** The dashboard waited on a multi-call fan-out per page load.

## 2. Design

A single backend worker owns the GitHub fan-out. Results land in a DB table; the user-facing endpoint becomes a cheap DB read. Webhooks keep the table fresh in near-real-time so users don't wait for the next refresh cycle to see new/deleted/resolved branches.

```
                            GitHub
                              │
                  ┌───────────┴───────────┐
                  │ (push / pull_request) │
                  ▼                       ▼
         WebhookService           BranchWithoutPRRefreshService
         (PR-opened,             (BackgroundService, interval =
          branch-create/delete)   BranchRefreshIntervalMinutes)
                  │                       │
                  │  AddBranch /          │ RefreshAndPersistAsync
                  │  RemoveBranch         │ (replace-all in a txn)
                  ▼                       ▼
            BranchesWithoutPR  ◀── AppDbContext (Postgres)
                  │
                  │  GET /api/repositories/branches-without-prs  (DB read)
                  │  POST /api/repositories/branches-without-prs/refresh
                  ▼
             Dashboard client
             (60s poll + SignalR push for instant add/remove)
```

## 3. Key decisions

| Decision | Choice | Rationale |
|---|---|---|
| Refresh interval | New `GitHubConfig.BranchRefreshIntervalMinutes` (default 30, clamped to 5–1440) | Decouples from the PR-refresh cadence, which has a very different cost profile. |
| Worker auth | GitHub App installation token via `IGitHubTokenService` | Background services have no user context. Same pattern as `PRRefreshService`. |
| Persistence | **Replace** all rows each cycle in a single transaction | Simplest, idempotent. The table is always a fresh snapshot; no row-by-row diffing. |
| `recentDays` cutoff | Hardcoded to 7 inside the refresh path | Moves the filter from request-time to refresh-time. Today's default was 7 anyway. |
| Unique key | `(RepoFullName, BranchName)` — DB-enforced unique index | Two orgs can have repos with the same bare name; `repo.Name` would collide. Same key used by `AddBranchAsync` / `RemoveBranchAsync` and by the frontend's SignalR matchers. |
| Manual trigger | `POST .../refresh` signals the worker via a bounded-1 channel | Keeps the "refresh now" UX without paying the GitHub cost on demand. Idempotent — repeated triggers while one is pending are silently dropped. |
| Webhook reaction | Immediate row mutation + SignalR push on PR-opened and branch-push events | Near-real-time UX. Worst case: webhook-driven rows get reconciled on the next refresh anyway. |
| Branch-create detection | Use the existing `push` webhook (`Created == true && RefType == Branch`) | No new webhook subscription needed — push events already arrive and are dispatched by `GitHubWebhookProcessor`. |
| Frontend delivery | Keep 60s poll (now DB-backed, cheap) + SignalR listeners | Minimum churn. The 60s poll becomes a safety net; SignalR gives instant add/remove. |

## 4. Moving parts

### Backend

- **Domain entity**: `GitBuddy.Domain/Models/BranchWithoutPR.cs` — `Id`, `Owner`, `Repo`, `RepoFullName`, `BranchName`, `DefaultBranch`, `LastActivityAt`, `LastRefreshedAt`.
- **Config**: `GitHubConfig.BranchRefreshIntervalMinutes` (int, default 30).
- **Migration**: `20260701173157_AddBranchWithoutPR.cs` creates the table with non-unique index `(Repo, BranchName)`. `20260701191521_UniqueBranchWithoutPRByKey.cs` drops that and creates the unique `(RepoFullName, BranchName)` index.
- **Service**: `BranchWithoutPRService` exposes:
  - `GetBranchesWithoutPRsAsync(...)` — the existing GitHub fan-out, unchanged.
  - `RefreshAndPersistAsync(config, ct)` — clears + refills the table in a transaction.
  - `AddBranchAsync(row, ct)` / `RemoveBranchAsync(repoFullName, branch, ct)` — webhook-driven single-row mutations; keyed on `(RepoFullName, BranchName)`.
- **Worker**: `BackgroundServices/BranchWithoutPRRefreshService` — modeled on `PRRefreshService` (10s initial delay, dynamic interval, 5-min retry on error). Uses `IServiceProvider` for scope creation.
- **Manual trigger**: `Services/BranchWithoutPRRefreshTrigger` — singleton with a bounded-1 `DropWrite` channel. Worker waits on either `Task.Delay(interval)` or `WaitToReadAsync`, whichever fires first, then drains.
- **MediatR endpoints** (under `Features/Repositories/`):
  - `GetBranchesWithoutPRs/{Query,Handler}.cs` — DB read projects to `BranchWithoutPRDto`, ordered by `LastActivityAt` desc.
  - `TriggerBranchesWithoutPRRefresh/{Command,Handler}.cs` — calls `trigger.Trigger()`, returns `Unit`; controller returns `202 Accepted`.
- **Webhook hooks** (`Services/WebhookService.cs`):
  - PR `opened`/`reopened` arm: after `CreateNewPrAsync`, calls `RemoveBranchAsync(repo.FullName, head)` and broadcasts `PendingBranchResolved`.
  - `HandlePushEventAsync`: when `RefType` is branch and `Created == true`, applies the same `IsIgnoredBranch` + default-branch filter as the refresh path, then inserts a row and broadcasts `PendingBranchAdded`. When `Deleted == true`, removes the row and broadcasts `PendingBranchResolved`. Skips branches that already have an open PR.
- **SignalR events** (`Services/SignalRNotificationService.cs`):
  - `PendingBranchResolved` — payload `{ repoFullName, branchName }` — fired on PR-opened and branch-delete.
  - `PendingBranchAdded` — payload is the full `BranchWithoutPRDto` — fired on branch-create.

### Frontend

- **API client** (`services/api.ts`): `getBranchesWithoutPR()` (no params), `refreshBranchesWithoutPR()` (POST).
- **Composable** (`composables/useBranchesWithoutPR.ts`): module-level state (singleton). Exposes `fetchBranches`, `manualRefresh`, `applyBranchResolved`, `applyBranchAdded`. `manualRefresh` has a retry guard (`refreshing` ref) and surfaces errors via the shared `error` ref. Both apply helpers key on `repoFullName + branchName`.
- **SignalR** (`composables/useSignalR.ts`): `onPendingBranchResolved` and `onPendingBranchAdded` are module-level refs so any composable can register handlers regardless of which `useSignalR()` call ran `setupEventHandlers`. `Dashboard.vue` wires them to the composable's apply helpers.
- **Section component** (`components/BranchesWithoutPRSection.vue`): unchanged layout, adds a `refreshing` prop so the spinner reflects manual-refresh state.
- **Dashboard wiring** (`views/Dashboard.vue`): keeps the 60s poll; the manual refresh button calls `manualRefreshBranchesWithoutPR` (POST trigger), not the GET.

## 5. Concurrency & race notes

- The worker's `RefreshAndPersistAsync` runs in a transaction: `ExecuteDelete` → `AddRange` → `SaveChanges` → `Commit`. Mid-flight webhook writes via `AddBranchAsync` run in their own transactions and may be wiped by the next refresh cycle; the cycle or the next webhook event restores them.
- The trigger channel is bounded to 1 with `DropWrite`. Repeated manual refresh requests while one is already queued are silently dropped — same observable behavior as an idempotent no-op. (`ChannelReader.Count` would have thrown `NotSupportedException` on an unbounded channel; bounded-1 also avoids any flag-based race.)
- The unique `(RepoFullName, BranchName)` index guards against concurrent webhook inserts racing on the same key.

## 6. Observability

All key transitions log:
- Worker: `Branches-Without-PR Refresh Service is starting`, `Branches-without-PR refresh completed`, `Refreshed {Count} branches-without-PR rows`, error-level retry logs.
- Webhooks: per-event info logs in `WebhookService.HandlePushEventAsync` / `HandlePullRequestEventAsync`.
- SignalR: `Broadcast PendingBranchResolved: {RepoFullName}/{Branch}` and `Broadcast PendingBranchAdded: {Repo}/{Branch}`.

## 7. Open follow-ups

- Surface `LastRefreshedAt` in the UI (currently entity-only, used for diagnostics).
- If branch volume grows large enough that replace-all-in-one-transaction becomes expensive, switch to an upsert-with-tombstone diff strategy.
- The SignalR payload for `PendingBranchResolved` uses `repoFullName`; if multi-org Apps ever become a real deployment shape, this key still holds because GitHub `repo.FullName` is globally unique within an installation.

## 8. Pointers

- Execution spec: [`specs/2026-07-01-branches-without-prs-backend-refresh/`](../../../specs/2026-07-01-branches-without-prs-backend-refresh/)
- Plan file from this session: [`C:\Users\RaunoKaasik\.claude\plans\write-the-spec-fancy-church.md`](../../../../C:/Users/RaunoKaasik/.claude/plans/write-the-spec-fancy-church.md)
