using GitBuddy.Api.DTOs;
using GitBuddy.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GitBuddy.Api.Services;

public class SignalRNotificationService(
    IHubContext<PRHub> hubContext,
    ILogger<SignalRNotificationService> logger)
    : INotificationService
{
    public async Task BroadcastPRCreatedAsync(PRListUpdateDto dto)
    {
        try
        {
            await hubContext.Clients.All.SendAsync("PRCreated", dto);
            logger.LogInformation("Broadcast PRCreated: PR #{GitHubId}", dto.GitHubId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error broadcasting PRCreated for PR #{GitHubId}", dto.GitHubId);
        }
    }

    public async Task BroadcastPRUpdatedAsync(PRListUpdateDto dto)
    {
        try
        {
            await hubContext.Clients.All.SendAsync("PRUpdated", dto);
            logger.LogInformation("Broadcast PRUpdated: PR #{GitHubId}", dto.GitHubId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error broadcasting PRUpdated for PR #{GitHubId}", dto.GitHubId);
        }
    }

    public async Task BroadcastPRClosedAsync(int prId, long gitHubId, string repository, bool wasMerged)
    {
        try
        {
            var dto = new PRClosedNotificationDto(prId, gitHubId, repository, wasMerged);
            await hubContext.Clients.All.SendAsync("PRClosed", dto);
            logger.LogInformation("Broadcast PRClosed: PR #{GitHubId} (Merged: {WasMerged})", gitHubId, wasMerged);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error broadcasting PRClosed for PR #{GitHubId}", gitHubId);
        }
    }

    public async Task BroadcastReviewAddedAsync(int prId, string newStatus, ReviewDto review)
    {
        try
        {
            var dto = new ReviewNotificationDto(prId, newStatus, review);
            await hubContext.Clients.All.SendAsync("ReviewAdded", dto);
            await hubContext.Clients.Group(PRHub.GetPRGroupName(prId)).SendAsync("ReviewAddedDetail", dto);
            logger.LogInformation("Broadcast ReviewAdded: PR #{PrId}", prId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error broadcasting ReviewAdded for PR #{PrId}", prId);
        }
    }

    public async Task BroadcastCommentChangedAsync(int prId, string action, CommentDto comment)
    {
        try
        {
            var dto = new CommentNotificationDto(prId, action, comment);
            await hubContext.Clients.All.SendAsync("CommentChanged", dto);
            await hubContext.Clients.Group(PRHub.GetPRGroupName(prId)).SendAsync("CommentChangedDetail", dto);
            logger.LogInformation("Broadcast CommentChanged: PR #{PrId} - {Action}", prId, action);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error broadcasting CommentChanged for PR #{PrId}", prId);
        }
    }

    public async Task BroadcastThreadChangedAsync(int prId, int threadId, bool isResolved)
    {
        try
        {
            var dto = new ThreadNotificationDto(prId, threadId, isResolved);
            await hubContext.Clients.All.SendAsync("ThreadChanged", dto);
            await hubContext.Clients.Group(PRHub.GetPRGroupName(prId)).SendAsync("ThreadChangedDetail", dto);
            logger.LogInformation("Broadcast ThreadChanged: PR #{PrId} - Thread #{ThreadId} - Resolved: {IsResolved}", prId, threadId, isResolved);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error broadcasting ThreadChanged for PR #{PrId}", prId);
        }
    }

    public async Task BroadcastCheckRunsUpdatedAsync(int prId, string checksStatus, List<CheckRunDto> checkRuns)
    {
        try
        {
            var dto = new CheckRunsNotificationDto(prId, checksStatus, checkRuns);
            await hubContext.Clients.All.SendAsync("CheckRunsUpdated", dto);
            await hubContext.Clients.Group(PRHub.GetPRGroupName(prId)).SendAsync("CheckRunsUpdatedDetail", dto);
            logger.LogInformation("Broadcast CheckRunsUpdated: PR #{PrId} - Status: {Status}", prId, checksStatus);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error broadcasting CheckRunsUpdated for PR #{PrId}", prId);
        }
    }

    public async Task BroadcastPendingBranchResolvedAsync(string repoFullName, string branchName)
    {
        try
        {
            await hubContext.Clients.All.SendAsync("PendingBranchResolved", new { repoFullName, branchName });
            logger.LogInformation("Broadcast PendingBranchResolved: {RepoFullName}/{Branch}", repoFullName, branchName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error broadcasting PendingBranchResolved for {RepoFullName}/{Branch}", repoFullName, branchName);
        }
    }

    public async Task BroadcastPendingBranchAddedAsync(BranchWithoutPRDto branch)
    {
        try
        {
            await hubContext.Clients.All.SendAsync("PendingBranchAdded", branch);
            logger.LogInformation("Broadcast PendingBranchAdded: {Repo}/{Branch}", branch.Repo, branch.BranchName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error broadcasting PendingBranchAdded for {Repo}/{Branch}", branch.Repo, branch.BranchName);
        }
    }
}
