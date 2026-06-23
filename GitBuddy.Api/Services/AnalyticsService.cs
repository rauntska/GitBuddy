using GitBuddy.Api.DTOs.Analytics;
using GitBuddy.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Services;

public class AnalyticsService(AppDbContext db) : IAnalyticsService
{
    private const int StaleThresholdDays = 14;
    private const int MaxStalePRs = 50;

    public async Task<ThroughputAnalytics> GetThroughputAsync(DateTime from, DateTime to, string[]? authors = null, CancellationToken ct = default)
    {
        var (utcFrom, utcTo) = NormalizeWindow(from, to);
        var authorFilter = NormalizeAuthors(authors);

        var openedPRs = await db.PullRequests.AsNoTracking()
            .Where(pr => pr.CreatedAt >= utcFrom && pr.CreatedAt <= utcTo
                && (authorFilter == null || authorFilter.Contains(pr.Author)))
            .Select(pr => new { pr.Id, pr.CreatedAt })
            .ToListAsync(ct);

        var openedDaily = openedPRs
            .GroupBy(pr => pr.CreatedAt.Date)
            .Select(g => new DailyCount(g.Key, g.Count()))
            .OrderBy(x => x.Date)
            .ToList();

        var mergedPRs = await db.PullRequests.AsNoTracking()
            .Where(pr => pr.MergedAt.HasValue && pr.MergedAt >= utcFrom && pr.MergedAt <= utcTo
                && (authorFilter == null || authorFilter.Contains(pr.Author)))
            .Select(pr => new { pr.Id, pr.CreatedAt, pr.MergedAt!.Value })
            .ToListAsync(ct);

        var mergedDaily = mergedPRs
            .GroupBy(pr => pr.Value.Date)
            .Select(g => new DailyCount(g.Key, g.Count()))
            .OrderBy(x => x.Date)
            .ToList();

        var medianMerge = Median(mergedPRs
            .Select(pr => (double?)(pr.Value - pr.CreatedAt).TotalHours)
            .Where(h => h >= 0)
            .ToList());

        var openedIds = openedPRs.Select(pr => pr.Id).ToList();
        var createdAtById = openedPRs.ToDictionary(pr => pr.Id, pr => pr.CreatedAt);

        var earliestReviews = await db.Reviews.AsNoTracking()
            .Where(r => openedIds.Contains(r.PullRequestId)
                && r.SubmittedAt.HasValue
                && r.State != "PENDING")
            .GroupBy(r => r.PullRequestId)
            .Select(g => new { PullRequestId = g.Key, FirstReview = g.Min(r => r.SubmittedAt!.Value) })
            .ToListAsync(ct);

        var reviewLatencies = earliestReviews
            .Select(er => (er.FirstReview - createdAtById[er.PullRequestId]).TotalHours)
            .Where(h => h >= 0)
            .Cast<double?>()
            .ToList();

        var medianTTFR = Median(reviewLatencies);

        return new ThroughputAnalytics(
            openedDaily,
            mergedDaily,
            medianMerge,
            medianTTFR,
            openedPRs.Count,
            mergedPRs.Count);
    }

    public async Task<ReviewerAnalytics> GetReviewerStatsAsync(DateTime from, DateTime to, string[]? authors = null, CancellationToken ct = default)
    {
        var (utcFrom, utcTo) = NormalizeWindow(from, to);
        var authorFilter = NormalizeAuthors(authors);

        var reviews = await db.Reviews.AsNoTracking()
            .Where(r => r.SubmittedAt.HasValue
                && r.SubmittedAt >= utcFrom
                && r.SubmittedAt <= utcTo
                && r.State != "PENDING"
                && (authorFilter == null || authorFilter.Contains(r.PullRequest.Author)))
            .Select(r => new
            {
                r.Reviewer,
                r.ReviewerAvatar,
                r.State,
                r.SubmittedAt,
                r.PullRequest.CreatedAt
            })
            .ToListAsync(ct);

        var authoredRaw = await db.PullRequests.AsNoTracking()
            .Where(pr => pr.CreatedAt >= utcFrom && pr.CreatedAt <= utcTo
                && (authorFilter == null || authorFilter.Contains(pr.Author)))
            .Select(pr => new { pr.Author, pr.AuthorAvatar })
            .ToListAsync(ct);

        var windowAuthorsRaw = await db.PullRequests.AsNoTracking()
            .Where(pr => pr.CreatedAt >= utcFrom && pr.CreatedAt <= utcTo)
            .Select(pr => new { pr.Author, pr.AuthorAvatar })
            .ToListAsync(ct);

        var authoredByUser = authoredRaw
            .GroupBy(x => x.Author)
            .Select(g => new
            {
                Username = g.Key,
                Count = g.Count(),
                Avatar = g.FirstOrDefault(x => !string.IsNullOrEmpty(x.AuthorAvatar))?.AuthorAvatar
            })
            .ToDictionary(x => x.Username);

        var users = new HashSet<string>(reviews.Select(r => r.Reviewer), StringComparer.OrdinalIgnoreCase);
        foreach (var author in authoredByUser.Keys) users.Add(author);

        var stats = users
            .Select(user =>
            {
                var userReviews = reviews.Where(r => string.Equals(r.Reviewer, user, StringComparison.OrdinalIgnoreCase)).ToList();
                var authored = authoredByUser.TryGetValue(user, out var a) ? a.Count : 0;
                var avatar = userReviews.FirstOrDefault(r => !string.IsNullOrEmpty(r.ReviewerAvatar))?.ReviewerAvatar
                    ?? (authoredByUser.TryGetValue(user, out var a2) ? a2.Avatar : null);
                var latencies = userReviews
                    .Where(r => r.SubmittedAt!.Value >= r.CreatedAt)
                    .Select(r => (double?)(r.SubmittedAt!.Value - r.CreatedAt).TotalHours)
                    .Where(h => h >= 0)
                    .ToList();
                return new ReviewerStat(
                    user,
                    avatar,
                    userReviews.Count,
                    authored,
                    userReviews.Count(r => r.State == "Approved"),
                    userReviews.Count(r => r.State == "ChangesRequested"),
                    userReviews.Count(r => r.State == "Commented"),
                    Median(latencies));
            })
            .OrderByDescending(r => r.TotalReviews + r.TotalPRsAuthored)
            .ThenByDescending(r => r.TotalReviews)
            .ToList();

        var authorOptions = windowAuthorsRaw
            .GroupBy(a => a.Author)
            .Select(g => new AuthorOption(
                g.Key,
                g.FirstOrDefault(x => !string.IsNullOrEmpty(x.AuthorAvatar))?.AuthorAvatar))
            .OrderBy(a => a.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ReviewerAnalytics(stats, authorOptions);
    }

    public async Task<HealthAnalytics> GetHealthAsync(DateTime from, DateTime to, string[]? authors = null, CancellationToken ct = default)
    {
        var authorFilter = NormalizeAuthors(authors);
        var staleCutoff = DateTime.UtcNow.AddDays(-StaleThresholdDays);
        var now = DateTime.UtcNow;

        var staleRows = await db.PullRequests.AsNoTracking()
            .Where(pr => pr.Status != "Closed" && pr.Status != "Merged" && pr.UpdatedAt < staleCutoff
                && (authorFilter == null || authorFilter.Contains(pr.Author)))
            .OrderBy(pr => pr.UpdatedAt)
            .Take(MaxStalePRs)
            .Select(pr => new { pr.Id, pr.Title, pr.UpdatedAt })
            .ToListAsync(ct);

        var staleDtos = staleRows
            .Select(pr => new StalePR(pr.Id, pr.Title, pr.UpdatedAt, (now - pr.UpdatedAt).Days))
            .ToList();

        var stuckCount = await db.PullRequests.AsNoTracking()
            .CountAsync(pr => pr.Status != "Closed" && pr.Status != "Merged" && pr.Status == "ChangesRequested"
                && (authorFilter == null || authorFilter.Contains(pr.Author)), ct);

        var failingCount = await db.PullRequests.AsNoTracking()
            .Where(pr => pr.Status != "Closed" && pr.Status != "Merged"
                && pr.CheckRuns.Any(c => c.Status == "completed" && c.Conclusion == "failure")
                && (authorFilter == null || authorFilter.Contains(pr.Author)))
            .CountAsync(ct);

        var unresolvedCount = await db.ReviewThreads.AsNoTracking()
            .CountAsync(rt => !rt.IsResolved && !rt.IsOutdated
                && (authorFilter == null || authorFilter.Contains(rt.PullRequest.Author)), ct);

        var totalOpen = await db.PullRequests.AsNoTracking()
            .CountAsync(pr => pr.Status != "Closed" && pr.Status != "Merged"
                && (authorFilter == null || authorFilter.Contains(pr.Author)), ct);

        return new HealthAnalytics(staleDtos, stuckCount, failingCount, unresolvedCount, totalOpen);
    }

    private static (DateTime from, DateTime to) NormalizeWindow(DateTime from, DateTime to)
    {
        var utcFrom = from.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(from, DateTimeKind.Utc)
            : from.ToUniversalTime();
        var utcTo = to.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(to, DateTimeKind.Utc)
            : to.ToUniversalTime();
        if (utcFrom > utcTo)
        {
            (utcFrom, utcTo) = (utcTo, utcFrom);
        }
        return (utcFrom, utcTo);
    }

    private static string[]? NormalizeAuthors(string[]? authors)
    {
        if (authors is null || authors.Length == 0) return null;
        var set = authors
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return set.Length == 0 ? null : set;
    }

    private static double? Median(List<double?> values)
    {
        if (values.Count == 0) return null;
        var sorted = values.Where(v => v.HasValue).Select(v => v!.Value).OrderBy(v => v).ToList();
        if (sorted.Count == 0) return null;
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }
}
