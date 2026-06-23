namespace GitBuddy.Api.DTOs;

public enum NotificationType
{
    PRCreated,
    PROpened,
    PRUpdated,
    PRClosed,
    PRMerged,
    ReviewSubmitted,
    ReviewDismissed,
    CommentAdded,
    CommentUpdated,
    CommentDeleted,
    ThreadResolved,
    ThreadUnresolved,
    CheckRunsUpdated
}

public record PRNotificationDto(
    NotificationType Type,
    int PullRequestId,
    long GitHubId,
    string Repository
);

public record PRListUpdateDto(
    int Id,
    long GitHubId,
    string Title,
    string Repository,
    string Author,
    string? AuthorAvatar,
    string Status,
    bool Draft,
    string Url,
    long Additions,
    long Deletions,
    long ChangedFiles,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? ChecksStatus,
    List<ReviewDto> Reviews,
    List<ReviewThreadDto> ReviewThreads
);

public record ReviewNotificationDto(
    int PullRequestId,
    string NewStatus,
    ReviewDto Review
);

public record CommentNotificationDto(
    int PullRequestId,
    string Action,
    CommentDto Comment
);

public record ThreadNotificationDto(
    int PullRequestId,
    int ThreadId,
    bool IsResolved
);

public record PRClosedNotificationDto(
    int PullRequestId,
    long GitHubId,
    string Repository,
    bool WasMerged
);

public record CheckRunsNotificationDto(
    int PullRequestId,
    string ChecksStatus,
    List<CheckRunDto> CheckRuns
);
