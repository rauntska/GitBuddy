using Graphite.Api.DTOs;

namespace Graphite.Api.Services;

public interface INotificationService
{
    Task BroadcastPRCreatedAsync(PRListUpdateDto dto);
    Task BroadcastPRUpdatedAsync(PRListUpdateDto dto);
    Task BroadcastPRClosedAsync(int prId, long gitHubId, string repository, bool wasMerged);
    Task BroadcastReviewAddedAsync(int prId, string newStatus, ReviewDto review);
    Task BroadcastCommentChangedAsync(int prId, string action, CommentDto comment);
    Task BroadcastThreadChangedAsync(int prId, int threadId, bool isResolved);
    Task BroadcastCheckRunsUpdatedAsync(int prId, string checksStatus, List<CheckRunDto> checkRuns);
}
