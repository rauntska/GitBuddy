using Graphite.Domain.Models;

namespace Graphite.Api.Services;

public interface ICacheService
{
    Task RefreshPullRequestsAsync(GitHubConfig config);
    Task<Dictionary<string, List<PullRequest>>> GetCachedPullRequestsAsync();
    Task<PRStats> GetPullRequestStatsAsync();
    Task<GitHubConfig?> GetConfigAsync();
    Task SaveConfigAsync(string organization, string token, int refreshIntervalMinutes, string appId = "", string privateKey = "", string installationId = "", bool useGitHubApp = false, bool deleteOldPRs = false);
    Task UpdateLastRefreshAsync();
}

public record PRStats(
    int TotalOpen,
    int Draft,
    int Approved,
    int AwaitingReview,
    int TotalComments,
    int ResolvedComments,
    int PendingComments
);