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

        var hasApproved = reviews.Any(r => r.State == "Approved");
        var hasChangesRequested = reviews.Any(r => r.State == "ChangesRequested");
        var hasComments = reviews.Any(r => r.State == "Commented");

        if (hasApproved && !hasChangesRequested) 
            return "Approved";
        
        if (hasChangesRequested) 
            return "ChangesRequested";
        
        if (hasComments) 
            return "Reviewed";
        
        return "AwaitingReview";
    }
}
