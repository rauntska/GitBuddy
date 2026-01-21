using Graphite.Domain.Models;

namespace Graphite.Api.Services;

public interface ICacheService
{
    Task RefreshPullRequestsAsync(string organization, string token);
    Task<Dictionary<string, List<PullRequest>>> GetCachedPullRequestsAsync();
    Task<PRStats> GetPullRequestStatsAsync();
    Task<GitHubConfig?> GetConfigAsync();
    Task SaveConfigAsync(string organization, string token, int refreshIntervalMinutes);
    Task UpdateLastRefreshAsync();
}

public record PRStats(
    int TotalOpen,
    int Draft,
    int Approved,
    int AwaitingReview
);