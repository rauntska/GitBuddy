namespace Graphite.Api.DTOs;

public record ReviewDto(
    int Id,
    string GitHubId,
    string Reviewer,
    string? ReviewerAvatar,
    string State,
    DateTime? SubmittedAt
);

public record ReviewThreadDto(
    int Id,
    string GitHubId,
    string Path,
    int? Line,
    string? DiffSide,
    string State,
    bool IsResolved,
    bool IsOutdated,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string FirstCommentAuthor,
    string FirstCommentBody,
    int CommentCount
);

public record PullRequestDto(
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
    DateTime LastSyncedAt,
    List<ReviewDto> Reviews,
    List<ReviewThreadDto> ReviewThreads,
    string? ChecksStatus,
    bool IsMergeReady = false,
    int? RequiredApprovingReviews = null,
    int CurrentApprovingReviews = 0,
    bool HasUnresolvedThreads = false,
    string? MergeBlockReason = null
);

public record CommentDto(
    int Id,
    long GitHubId,
    int? ReviewThreadId,
    string Author,
    string? AuthorAvatar,
    string Body,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? Path,
    int? Line,
    bool IsOutdated,
    DateTime? EditedAt = null,
    int EditCount = 0,
    int? ReplyToCommentId = null,
    List<CommentReactionDto>? Reactions = null
);

public record CommentReactionDto(
    int Id,
    string Username,
    string Reaction,
    DateTime CreatedAt
);

public record FileDiffDto(
    int Id,
    string Path,
    string? OldPath,
    string Status,
    int Additions,
    int Deletions,
    int Changes,
    string? Patch,
    string? Language,
    string? ViewedState,
    DateTime? ViewedAt
);

public record CheckRunDto(
    int Id,
    string GitHubId,
    string Name,
    string Status,
    string? Conclusion,
    string? Url,
    DateTime StartedAt,
    DateTime? CompletedAt
);

public record PRDetailDto(
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
    DateTime LastSyncedAt,
    string Description,
    string SourceBranch,
    string TargetBranch,
    string? MergeableState,
    string? ChecksStatus,
    bool IsMerged,
    DateTime? MergedAt,
    List<ReviewDto> Reviews,
    List<ReviewThreadDto> ReviewThreads,
    List<FileDiffDto> Files,
    List<CommentDto> AllComments,
    List<CheckRunDto> CheckRuns,
    PendingReviewDto? PendingReview = null,
    int? RequiredApprovingReviews = null,
    int CurrentApprovingReviews = 0,
    bool HasUnresolvedThreads = false,
    bool IsMergeReady = false,
    string? MergeBlockReason = null
);

public record AddCommentRequest(
    string Body,
    string? Path,
    int? Line
);

public record AddCommentRequestWithPath(
    string Body,
    string? Path,
    int? Line
);

public record SubmitReviewRequest(
    string State,
    string? Body
);

public record UpdateViewedStateRequest(
    string Path,
    bool Viewed
);

public record AddCommentReplyRequest(
    string ReviewThreadId,
    string Body
);

public record ResolveThreadRequest(
    bool Resolved
);

public record UnresolveThreadRequest(
);

public record MergePRRequest(
    string? MergeMethod,
    string? CommitTitle,
    string? CommitMessage
);

public record PendingReviewDto(
    string GitHubId,
    string State,
    List<PendingReviewCommentDto> Comments
);

public record PendingReviewCommentDto(
    string GitHubId,
    string Path,
    int? Line,
    string Body,
    string Author,
    string? AuthorAvatar,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? ThreadId = null
);

public record CreatePendingReviewCommentRequest(
    string Body,
    string Path,
    int Line
);

public record SubmitPendingReviewRequest(
    string State,
    string? Body
);

public record DeletePendingReviewCommentRequest(
    string CommentId
);
