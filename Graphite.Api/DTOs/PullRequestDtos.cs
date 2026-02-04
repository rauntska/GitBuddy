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
    List<ReviewThreadDto> ReviewThreads
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
    bool IsOutdated
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
    List<ReviewDto> Reviews,
    List<ReviewThreadDto> ReviewThreads,
    List<FileDiffDto> Files,
    List<CommentDto> AllComments
);

public record AddCommentRequest(
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