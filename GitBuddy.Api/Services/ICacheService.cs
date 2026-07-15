using GitBuddy.Domain.Models;

namespace GitBuddy.Api.Services;

public interface ICacheService
{
    Task RefreshPullRequestsAsync(GitHubConfig config, long? prNumber = null, string? repository = null);
    Task<Dictionary<string, List<PullRequest>>> GetCachedPullRequestsAsync();
    Task<Dictionary<string, object>> GetCachedPullRequestDtosAsync();
    void InvalidateDashboardCache();
    Task<PRStats> GetPullRequestStatsAsync();
    Task<GitHubConfig?> GetConfigAsync();
    Task SaveConfigAsync(string organization, string? token, int refreshIntervalMinutes, string? appId = "", string? privateKey = "", string? installationId = "", bool useGitHubApp = false, bool deleteOldPRs = false, string? teamsWebhookUrl = null, bool teamsEnabled = false);
    Task UpdateLastRefreshAsync();
}

public record PRStats(
    int TotalOpen,
    int Draft,
    int Approved,
    int ReadyToMerge,
    int AwaitingReview,
    int TotalComments,
    int ResolvedComments,
    int PendingComments
);