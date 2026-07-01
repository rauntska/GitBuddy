# Implementation Plan: Backend-Scheduled Branches-Without-PRs Refresh

Execute top-to-bottom. Each numbered group is independently shippable.

## 1. Domain layer

1. Create `GitBuddy.Domain/Models/BranchWithoutPR.cs`:
   ```csharp
   public class BranchWithoutPR
   {
       public int Id { get; set; }
       public string Owner { get; set; } = string.Empty;
       public string Repo { get; set; } = string.Empty;
       public string RepoFullName { get; set; } = string.Empty;
       public string BranchName { get; set; } = string.Empty;
       public string DefaultBranch { get; set; } = string.Empty;
       public DateTime? LastActivityAt { get; set; }
       public DateTime LastRefreshedAt { get; set; }
   }
   ```
2. Add `BranchRefreshIntervalMinutes` (int, default 30) to `GitBuddy.Domain/Models/GitHubConfig.cs`, mirroring the existing `RefreshIntervalMinutes` property.
3. Register `DbSet<BranchWithoutPR> BranchWithoutPRs { get; set; }` in `GitBuddy.Domain/Data/AppDbContext.cs`. In `OnModelCreating`:
   - Composite index on `(Repo, BranchName)` — fast webhook-driven deletes.
   - Index on `LastRefreshedAt` — diagnostics.

## 2. Migration

1. From `GitBuddy.Api/`: `dotnet ef migrations add AddBranchWithoutPR`.
2. Verify the generated migration creates the `BranchWithoutPRs` table, the two indexes, and adds `BranchRefreshIntervalMinutes` to `GitHubConfigs` (with a default of 30 so existing rows pick it up).
3. `dotnet ef database update` to apply locally. (App auto-applies migrations on startup — `Program.cs` calls `dbContext.Database.Migrate()` per CLAUDE.md — but applying now confirms the migration is valid before going further.)

## 3. Service refactor — `BranchWithoutPRService`

File: `GitBuddy.Api/Services/BranchWithoutPRService.cs`.

1. Inject `IGitHubTokenService` and `AppDbContext` (or `IDbContextFactory<AppDbContext>` if the existing pattern in this repo prefers it — check `PRRefreshService` for precedent) into the constructor.
2. Keep `GetBranchesWithoutPRsAsync(...)` as-is — it is the fan-out primitive.
3. Add a new method:
   ```csharp
   public async Task RefreshAndPersistAsync(GitHubConfig config, CancellationToken ct = default)
   {
       var accessToken = await tokenService.GetAccessTokenAsync(config);
       var openPRBranches = await context.PullRequests
           .Where(pr => pr.Status != "Closed" && pr.Status != "Merged")
           .Select(pr => new { pr.Repository, pr.SourceBranch })
           .ToListAsync(ct);
       var openPRSet = openPRBranches
           .Select(x => (x.Repository, x.SourceBranch))
           .ToHashSet();

       var dtos = await GetBranchesWithoutPRsAsync(config.Organization, accessToken, openPRSet, recentDays: 7);

       var now = DateTime.UtcNow;
       var rows = dtos.Select(d => new BranchWithoutPR
       {
           Owner = d.Owner,
           Repo = d.Repo,
           RepoFullName = d.RepoFullName,
           BranchName = d.BranchName,
           DefaultBranch = d.DefaultBranch,
           LastActivityAt = d.LastActivityAt,
           LastRefreshedAt = now
       }).ToList();

       using var tx = await context.Database.BeginTransactionAsync(ct);
       await context.BranchesWithoutPR.ExecuteDeleteAsync(ct);
       await context.BranchesWithoutPR.AddRangeAsync(rows, ct);
       await context.SaveChangesAsync(ct);
       await tx.CommitAsync(ct);
   }
   ```
4. Add a method for single-row webhook-driven mutations (used by `WebhookService`):
   ```csharp
   public async Task AddBranchAsync(BranchWithoutPR row, CancellationToken ct = default);
   public async Task RemoveBranchAsync(string repo, string branch, CancellationToken ct = default);
   ```
   Both save immediately. `RemoveBranchAsync` uses `ExecuteDelete`. `AddBranchAsync` upserts on `(Repo, BranchName)` (delete-then-insert, or check existing first — simplest is delete-then-insert in a transaction).

## 4. Background worker — `BranchWithoutPRRefreshService`

New file: `GitBuddy.Api/BackgroundServices/BranchWithoutPRRefreshService.cs`.

1. Copy the skeleton from `PRRefreshService.cs`: primary constructor with `IServiceProvider` + `ILogger<...>`, `ExecuteAsync` with 10s initial delay, `while (!stoppingToken.IsCancellationRequested)` loop, try/catch with 5-minute retry delay.
2. Inside the loop:
   - Create a scope.
   - Resolve `ICacheService` to fetch the current `GitHubConfig`.
   - If config is null or `UseGitHubApp == false`, log a warning and sleep the retry delay (the worker is a no-op without a configured App).
   - Read `BranchRefreshIntervalMinutes`; clamp to `[5, 1440]`; default to 30 if zero/invalid.
   - Resolve `IBranchWithoutPRService`, call `RefreshAndPersistAsync(config, stoppingToken)`.
   - Log "Branches-without-PR refresh completed".
   - `await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken)`.
3. Manual trigger plumbing: expose a `TriggerRefreshAsync()` method on the worker that signals an `System.Threading.Channels.Channel<object>` (or a `ManualResetEventSlim`) consumed at the top of the loop to skip the next `Task.Delay`. Resolve the worker as `IHostedService` via `IServiceProvider` and call this from the controller in step 6.

## 5. Program.cs

File: `GitBuddy.Api/Program.cs`.

1. After line 91, add `builder.Services.AddHostedService<BranchWithoutPRRefreshService>();`.

## 6. Controller rewrite

File: `GitBuddy.Api/Controllers/RepositoriesController.cs:135-160`.

1. Replace `GetBranchesWithoutPRs` body with a pure DB read:
   ```csharp
   [HttpGet("branches-without-prs")]
   public async Task<IActionResult> GetBranchesWithoutPRs()
   {
       var rows = await context.BranchesWithoutPR
           .OrderByDescending(b => b.LastActivityAt ?? DateTime.MinValue)
           .Select(b => new BranchWithoutPRDto(
               b.Owner, b.Repo, b.RepoFullName, b.BranchName, b.DefaultBranch, b.LastActivityAt))
           .ToListAsync();
       return Ok(rows);
   }
   ```
2. Drop the `[FromQuery] int recentDays` parameter. Drop the user-token requirement (`validationService.GetRequiredUserWithTokenAsync(User)` is no longer needed for this endpoint).
3. Add a new endpoint:
   ```csharp
   [HttpPost("branches-without-prs/refresh")]
   public IActionResult TriggerRefresh([FromServices] IServiceProvider sp)
   {
       // Resolve the hosted service and signal it
       var worker = sp.GetServices<IHostedService>().OfType<BranchWithoutPRRefreshService>().First();
       worker.TriggerRefreshAsync();
       return Accepted();
   }
   ```

## 7. Webhook hooks

File: `GitBuddy.Api/Services/WebhookService.cs`.

### 7a. PR opened / reopened

In `ProcessPullRequestActionAsync`, inside the `"opened"` / `"reopened"` arm (currently calls `CreateNewPrAsync`), after the create succeeds:
```csharp
await branchWithoutPRService.RemoveBranchAsync(repo.Name, prData.Head.Ref);
await notificationService.BroadcastPendingBranchResolvedAsync(repo.Name, prData.Head.Ref);
```
(Inject `IBranchWithoutPRService` into `WebhookService` if not already present.)

### 7b. Branch push (create / delete)

Extend `HandlePushEventAsync` (currently lines 349-382). At the top, before the existing PR-update logic, add:
```csharp
if (pushEvent.RefType == Octokit.Webhooks.Models.RefType.Branch)
{
    var branch = pushEvent.Ref.Replace("refs/heads/", "");
    var repoName = pushEvent.Repository.Name;
    var repoFullName = pushEvent.Repository.FullName;
    var owner = pushEvent.Repository.Owner.Login;
    var defaultBranch = pushEvent.Repository.DefaultBranch ?? "main";

    if (pushEvent.Deleted == true)
    {
        await branchWithoutPRService.RemoveBranchAsync(repoName, branch);
        await notificationService.BroadcastPendingBranchResolvedAsync(repoName, branch);
        // fall through to existing PR-update logic (which will likely no-op since the branch is gone)
    }
    else if (pushEvent.Created == true)
    {
        if (!IsIgnoredBranch(branch) && branch != defaultBranch)
        {
            var lastActivityAt = pushEvent.HeadCommit?.Timestamp?.UtcDateTime;
            var row = new BranchWithoutPR
            {
                Owner = owner,
                Repo = repoName,
                RepoFullName = repoFullName,
                BranchName = branch,
                DefaultBranch = defaultBranch,
                LastActivityAt = lastActivityAt,
                LastRefreshedAt = DateTime.UtcNow
            };
            await branchWithoutPRService.AddBranchAsync(row);
            await notificationService.BroadcastPendingBranchAddedAsync(
                owner, repoName, repoFullName, branch, defaultBranch, lastActivityAt);
        }
    }
}
```
- `IsIgnoredBranch` is currently `private static` inside `BranchWithoutPRService`. Promote it to `internal static` (or move to a shared utility) so `WebhookService` can call the same filter.
- Keep the existing PR-update code path unchanged after this block.

## 8. SignalR — new events

File: `GitBuddy.Api/Services/SignalRNotificationService.cs`.

Add two methods mirroring the shape of `BroadcastPRCreatedAsync`:
```csharp
public async Task BroadcastPendingBranchResolvedAsync(string repo, string branch)
{
    await hubContext.Clients.All.SendAsync("PendingBranchResolved", new { repository = repo, branchName = branch });
}

public async Task BroadcastPendingBranchAddedAsync(
    string owner, string repo, string repoFullName, string branch, string defaultBranch, DateTime? lastActivityAt)
{
    await hubContext.Clients.All.SendAsync("PendingBranchAdded", new
    {
        owner, repo, repoFullName, branchName = branch, defaultBranch, lastActivityAt
    });
}
```
No changes to `PRHub.cs` itself — events are pushed via `IHubContext`, no new hub methods required.

## 9. Frontend types

File: `gitbuddy-vue/src/types/index.ts:480-487`.

`BranchWithoutPR` interface is already correct — no changes.

## 10. Frontend API client

File: `gitbuddy-vue/src/services/api.ts:458-463`.

1. Drop the `recentDays` parameter:
   ```typescript
   getBranchesWithoutPR: async (): Promise<BranchWithoutPR[]> => {
       const response = await api.get<BranchWithoutPR[]>('/repositories/branches-without-prs');
       return response.data;
   }
   ```
2. Add:
   ```typescript
   refreshBranchesWithoutPR: async (): Promise<void> => {
       await api.post('/repositories/branches-without-prs/refresh');
   }
   ```

## 11. Frontend composable

File: `gitbuddy-vue/src/composables/useBranchesWithoutPR.ts`.

1. Remove the `recentDays` argument from the `apiService.getBranchesWithoutPR()` call.
2. Expose a `manualRefresh` action that calls `apiService.refreshBranchesWithoutPR()` (fire-and-forget; the actual data refresh arrives via SignalR or the next 60s poll).

## 12. Frontend SignalR listeners

File: `gitbuddy-vue/src/composables/useSignalR.ts` (and wherever the composable exposes event subscriptions to components — follow the existing pattern used for `PRCreated`, `PRUpdated`, etc.).

1. Register handlers for the two new events:
   - `PendingBranchResolved` — receives `{ repository, branchName }`. Remove the matching entry from the local branches list (matched on `repo === repository && branchName === branchName`).
   - `PendingBranchAdded` — receives the full row payload. If an entry with the same `(repo, branchName)` exists, replace it; otherwise prepend.
2. Surface these handlers in `useBranchesWithoutPR` so the composable's `branches` ref stays in sync.

## 13. Frontend component

File: `gitbuddy-vue/src/components/BranchesWithoutPRSection.vue`.

1. Manual refresh button: change the click handler from `fetchBranches` (which called the GET) to `manualRefresh` (which calls the new POST). Optionally show a transient "refreshing…" state until the next poll or SignalR event arrives.

## 14. Validation pass

Follow `validation.md`. Build both projects, apply migration, walk through the manual test matrix.
