using Graphite.Domain.Models;
using Octokit.GraphQL.Model;

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
    Task SubmitPullRequestReviewAsync(string organization, string repository, long pullRequestNumber, string state, string? body, GitHubConfig config, string userAccessToken);
    Task<GitHubPRStatusData?> GetPullRequestStatusAsync(string organization, string repository, long pullRequestNumber, GitHubConfig config);
    Task<GitHubCommentData> AddPullRequestReviewThreadReplyAsync(string organization, string repository, long pullRequestNumber, string reviewThreadId, string body, GitHubConfig config, string userAccessToken);
    Task ResolveReviewThreadAsync(string organization, string repository, string threadId, bool resolved, GitHubConfig config, string userAccessToken);
    Task UnresolveReviewThreadAsync(string organization, string repository, string threadId, GitHubConfig config, string userAccessToken);
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
    string GitHubId,
    string Reviewer,
    string? ReviewerAvatar,
    string State,
    DateTime? SubmittedAt
);

public record GitHubReviewThreadData(
    string GitHubId,
    string Path,
    int? Line,
    Octokit.GraphQL.Model.DiffSide diffSide,
    string State,
    bool IsResolved,
    bool IsOutdated,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string FirstCommentAuthor,
    string FirstCommentBody,
    int CommentCount);

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

public record GitHubPRStatusData(
    bool IsMerged,
    bool IsClosed,
    bool IsOpen,
    PullRequestState State,
    DateTime? MergedAt,
    DateTime? ClosedAt);