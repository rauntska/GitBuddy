using Graphite.Domain.Models;

namespace Graphite.Api.Services;

public interface IGitHubService
{
    Task<List<GitHubPRData>> GetOpenPullRequestsAsync(string organization, GitHubConfig config);
    Task<List<GitHubReviewData>> GetReviewsAsync(string organization, string repository, long pullRequestNumber,
        GitHubConfig config);
    Task<List<GitHubReviewThreadData>> GetReviewThreadsAsync(string organization, string repository,
        long pullRequestNumber, GitHubConfig config);
    Task<List<GitHubCommentData>> GetCommentsAsync(string organization, string repository, long pullRequestNumber,
        GitHubConfig config);
    Task<List<GitHubFileDiffData>> GetFileDiffsAsync(string organization, string repository, long pullRequestNumber,
        GitHubConfig config, string? userAccessToken = null);
    Task MarkFileAsViewedAsync(string organization, string repository, int pullRequestNumber, string path, GitHubConfig config, string? userAccessToken = null);
    Task UnmarkFileAsViewedAsync(string organization, string repository, int pullRequestNumber, string path, GitHubConfig config, string? userAccessToken = null);
}

public record GitHubPRData(
    long Id,
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
    List<GitHubReviewData>? Reviews,
    List<GitHubReviewThreadData>? ReviewThreads,
    string Description,
    string SourceBranch,
    string TargetBranch,
    string? MergeableState
);

public record GitHubReviewData(
    string Reviewer,
    string? ReviewerAvatar,
    string State,
    DateTime? SubmittedAt
);

public record GitHubReviewThreadData(
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

public record GitHubFileDiffData(
    string Path,
    string? OldPath,
    string Status,
    int Additions,
    int Deletions,
    int Changes,
    string? Patch,
    string ViewerViewedState
);

public record GitHubCommentData(
    long GitHubId,
    string ReviewThreadId,
    string Author,
    string? AuthorAvatar,
    string Body,
    string? Path,
    int? Line,
    bool IsOutdated,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);