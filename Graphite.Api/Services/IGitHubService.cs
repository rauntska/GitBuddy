using Graphite.Domain.Models;
using Octokit.GraphQL.Model;

namespace Graphite.Api.Services;

public interface IGitHubService
{
    Task<List<GitHubPRData>> GetOpenPullRequestsAsync(string organization, GitHubConfig config);
    Task<GitHubPRData?> GetPullRequestAsync(string organization, string repository, long pullRequestNumber, GitHubConfig config);
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
    Task<GitHubCommentData> AddPullRequestCommentAsync(string organization, string repository, long pullRequestNumber, string body, string? path, int? line, GitHubConfig config, string userAccessToken);
    Task ResolveReviewThreadAsync(string organization, string repository, string threadId, bool resolved, GitHubConfig config, string userAccessToken);
    Task UnresolveReviewThreadAsync(string organization, string repository, string threadId, GitHubConfig config, string userAccessToken);
    Task PublishDraftPullRequestAsync(string organization, string repository, long pullRequestNumber, GitHubConfig config, string userAccessToken);
    Task<GitHubRepoMergeOptions> GetRepositoryMergeOptionsAsync(string organization, string repository, string userAccessToken);
    Task MergePullRequestAsync(string organization, string repository, long pullRequestNumber, string? commitTitle, string? commitMessage, string mergeMethod, string userAccessToken);
    Task<GitHubPendingReviewData?> GetPendingReviewAsync(string organization, string repository, long pullRequestNumber, string userLogin, string userAccessToken);
    Task<GitHubPendingReviewCommentData> AddPendingReviewCommentAsync(string organization, string repository, long pullRequestNumber, string body, string path, int line, GitHubConfig config, string userAccessToken);
    Task<bool> DeletePendingReviewCommentAsync(string organization, string repository, string commentId, string userAccessToken);
    Task<bool> SubmitPendingReviewAsync(string organization, string repository, string reviewId, string state, string? body, string userAccessToken);
    Task<bool> DeletePendingReviewAsync(string organization, string repository, string reviewId, string userAccessToken);
    Task<bool> UpdateReviewCommentAsync(string organization, string repository, long commentId, string body, string userAccessToken);
    Task<bool> DeleteReviewCommentAsync(string organization, string repository, long commentId, string userAccessToken);
    Task<Dictionary<string, GitHubBranchProtectionData?>> GetRepositoryRulesetsAsync(string organization, string repository, GitHubConfig config);
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
    string? MergeableState,
    string? ChecksStatus,
    List<GitHubCheckRunData>? CheckRuns
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
    DateTime? UpdatedAt,
    bool IsPending = false,
    string? PendingReviewId = null,
    string? CommentNodeId = null
);

public record GitHubPRStatusData(
    bool IsMerged,
    bool IsClosed,
    bool IsOpen,
    PullRequestState State,
    DateTime? MergedAt,
    DateTime? ClosedAt);

public record GitHubCheckRunData(
    string Id,
    string Name,
    string Status,
    string? Conclusion,
    string? Url,
    DateTime StartedAt,
    DateTime? CompletedAt
);

public record GitHubRepoMergeOptions(
    bool MergeCommitAllowed,
    bool SquashMergeAllowed,
    bool RebaseMergeAllowed,
    string? DefaultMergeMethod
);

public record GitHubBranchProtectionData(
    bool RequiresApprovingReviews,
    int? RequiredApprovingReviewCount,
    bool RequiresStatusChecks,
    List<string> RequiredStatusChecks
);