namespace Graphite.Api.DTOs.Analytics;

public record DailyCount(DateTime Date, int Count);

public record ThroughputAnalytics(
    IReadOnlyList<DailyCount> OpenedDaily,
    IReadOnlyList<DailyCount> MergedDaily,
    double? MedianTimeToMergeHours,
    double? MedianTimeToFirstReviewHours,
    int TotalOpened,
    int TotalMerged);

public record ReviewerStat(
    string Username,
    string? AvatarUrl,
    int TotalReviews,
    int TotalPRsAuthored,
    int Approvals,
    int ChangesRequested,
    int Comments,
    double? MedianReviewLatencyHours);

public record ReviewerAnalytics(
    IReadOnlyList<ReviewerStat> Reviewers,
    IReadOnlyList<AuthorOption> Authors);

public record AuthorOption(string Username, string? AvatarUrl);

public record StalePR(int Id, string Title, DateTime UpdatedAt, int DaysStale);

public record HealthAnalytics(
    IReadOnlyList<StalePR> StalePRs,
    int StuckInReviewCount,
    int FailingChecksCount,
    int UnresolvedThreadsCount,
    int TotalOpenPRs);
