using GitBuddy.Domain.Classifier;

namespace GitBuddy.Api.Services;

public interface IPullRequestStatusService
{
    string DeterminePrStatus(string currentStatus,bool isMerged, bool isDraft, bool isMergeReady, List<GitHubReviewData> reviews);
}

public class PullRequestStatusService : IPullRequestStatusService
{
    public string DeterminePrStatus(string currentStatus,bool isMerged, bool isDraft, bool isMergeReady, List<GitHubReviewData> reviews)
    {
        if (isMerged) return PrStatus.Merged;
        
        if(currentStatus is PrStatus.Closed or PrStatus.Merged)
            return currentStatus;
        
        if (isDraft)
            return PrStatus.Draft;

        if (isMergeReady)
            return PrStatus.ReadyToMerge;

        var latestReviews = reviews
            .GroupBy(r => r.Reviewer)
            .Select(g => g.OrderByDescending(r => r.SubmittedAt).First())
            .ToList();

        var hasApproved = latestReviews.Any(r => r.State == PrStatus.Approved);
        var hasChangesRequested = latestReviews.Any(r => r.State == PrStatus.ChangesRequested);
        var hasComments = latestReviews.Any(r => r.State == "Commented");

        if (hasApproved && !hasChangesRequested)
            return PrStatus.Approved;

        if (hasChangesRequested)
            return PrStatus.ChangesRequested;

        if (hasComments)
            return PrStatus.Reviewed;

        return PrStatus.AwaitingReview;
    }
}
