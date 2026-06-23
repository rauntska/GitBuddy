using GitBuddy.Api.DTOs.Analytics;

namespace GitBuddy.Api.Services;

public interface IAnalyticsService
{
    Task<ThroughputAnalytics> GetThroughputAsync(DateTime from, DateTime to, string[]? authors = null, CancellationToken ct = default);
    Task<ReviewerAnalytics> GetReviewerStatsAsync(DateTime from, DateTime to, string[]? authors = null, CancellationToken ct = default);
    Task<HealthAnalytics> GetHealthAsync(DateTime from, DateTime to, string[]? authors = null, CancellationToken ct = default);
}
