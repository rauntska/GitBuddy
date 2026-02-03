using Graphite.Domain.Models;

namespace Graphite.Api.Services;

public interface IPullRequestStatusService
{
    string DeterminePrStatus(bool isDraft, List<GitHubReviewData> reviews);
}

public class PullRequestStatusService : IPullRequestStatusService
{
    public string DeterminePrStatus(bool isDraft, List<GitHubReviewData> reviews)
    {
        if (isDraft)
            return "Draft";

        var latestReviews = reviews
            .GroupBy(r => r.Reviewer)
            .Select(g => g.OrderByDescending(r => r.SubmittedAt).First())
            .ToList();

        var hasApproved = latestReviews.Any(r => r.State == "Approved");
        var hasChangesRequested = latestReviews.Any(r => r.State == "ChangesRequested");
        var hasComments = latestReviews.Any(r => r.State == "Commented");

        if (hasApproved && !hasChangesRequested)
            return "Approved";

        if (hasChangesRequested)
            return "ChangesRequested";

        if (hasComments)
            return "Reviewed";

        return "AwaitingReview";
    }
}
