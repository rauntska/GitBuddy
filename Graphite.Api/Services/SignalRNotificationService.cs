using Graphite.Api.DTOs;
using Graphite.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Graphite.Api.Services;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<PRHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<PRHub> hubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastPRCreatedAsync(PRListUpdateDto dto)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("PRCreated", dto);
            _logger.LogInformation("Broadcast PRCreated: PR #{GitHubId}", dto.GitHubId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting PRCreated for PR #{GitHubId}", dto.GitHubId);
        }
    }

    public async Task BroadcastPRUpdatedAsync(PRListUpdateDto dto)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("PRUpdated", dto);
            _logger.LogInformation("Broadcast PRUpdated: PR #{GitHubId}", dto.GitHubId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting PRUpdated for PR #{GitHubId}", dto.GitHubId);
        }
    }

    public async Task BroadcastPRClosedAsync(int prId, long gitHubId, string repository, bool wasMerged)
    {
        try
        {
            var dto = new PRClosedNotificationDto(prId, gitHubId, repository, wasMerged);
            await _hubContext.Clients.All.SendAsync("PRClosed", dto);
            _logger.LogInformation("Broadcast PRClosed: PR #{GitHubId} (Merged: {WasMerged})", gitHubId, wasMerged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting PRClosed for PR #{GitHubId}", gitHubId);
        }
    }

    public async Task BroadcastReviewAddedAsync(int prId, string newStatus, ReviewDto review)
    {
        try
        {
            var dto = new ReviewNotificationDto(prId, newStatus, review);
            await _hubContext.Clients.All.SendAsync("ReviewAdded", dto);
            await _hubContext.Clients.Group(PRHub.GetPRGroupName(prId)).SendAsync("ReviewAddedDetail", dto);
            _logger.LogInformation("Broadcast ReviewAdded: PR #{PrId}", prId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting ReviewAdded for PR #{PrId}", prId);
        }
    }

    public async Task BroadcastCommentChangedAsync(int prId, string action, CommentDto comment)
    {
        try
        {
            var dto = new CommentNotificationDto(prId, action, comment);
            await _hubContext.Clients.All.SendAsync("CommentChanged", dto);
            await _hubContext.Clients.Group(PRHub.GetPRGroupName(prId)).SendAsync("CommentChangedDetail", dto);
            _logger.LogInformation("Broadcast CommentChanged: PR #{PrId} - {Action}", prId, action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting CommentChanged for PR #{PrId}", prId);
        }
    }

    public async Task BroadcastThreadChangedAsync(int prId, int threadId, bool isResolved)
    {
        try
        {
            var dto = new ThreadNotificationDto(prId, threadId, isResolved);
            await _hubContext.Clients.All.SendAsync("ThreadChanged", dto);
            await _hubContext.Clients.Group(PRHub.GetPRGroupName(prId)).SendAsync("ThreadChangedDetail", dto);
            _logger.LogInformation("Broadcast ThreadChanged: PR #{PrId} - Thread #{ThreadId} - Resolved: {IsResolved}", prId, threadId, isResolved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting ThreadChanged for PR #{PrId}", prId);
        }
    }

    public async Task BroadcastCheckRunsUpdatedAsync(int prId, string checksStatus, List<CheckRunDto> checkRuns)
    {
        try
        {
            var dto = new CheckRunsNotificationDto(prId, checksStatus, checkRuns);
            await _hubContext.Clients.All.SendAsync("CheckRunsUpdated", dto);
            await _hubContext.Clients.Group(PRHub.GetPRGroupName(prId)).SendAsync("CheckRunsUpdatedDetail", dto);
            _logger.LogInformation("Broadcast CheckRunsUpdated: PR #{PrId} - Status: {Status}", prId, checksStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting CheckRunsUpdated for PR #{PrId}", prId);
        }
    }
}
