using Graphite.Api.DTOs.Analytics;

namespace Graphite.Api.Services;

public interface IAnalyticsService
{
    Task<ThroughputAnalytics> GetThroughputAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<ReviewerAnalytics> GetReviewerStatsAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<HealthAnalytics> GetHealthAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
