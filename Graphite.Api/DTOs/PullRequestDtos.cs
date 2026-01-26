namespace Graphite.Api.DTOs;

public record ReviewDto(
    int Id,
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
    int GitHubId,
    string Title,
    string Repository,
    string Author,
    string? AuthorAvatar,
    string Status,
    bool Draft,
    string Url,
    int Additions,
    int Deletions,
    int ChangedFiles,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime LastSyncedAt,
    List<ReviewDto> Reviews,
    List<ReviewThreadDto> ReviewThreads
);