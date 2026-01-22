namespace Graphite.Api.DTOs;

public record ReviewDto(
    int Id,
    string Reviewer,
    string? ReviewerAvatar,
    string State,
    DateTime? SubmittedAt
);

public record CommentDto(
    int Id,
    int Count,
    DateTime? LastUpdated
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
    CommentDto? Comment
);