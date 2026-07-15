using GitBuddy.Domain.Classifier;
using GitBuddy.Domain.Models;

namespace GitBuddy.Api.Services;

public interface IPriorityService
{
    int GetEffectivePriority(PullRequest pr);
    bool IsOverridden(PullRequest pr);
}

public class PriorityService : IPriorityService
{
    private static readonly TimeSpan StaleAwaitingReviewThreshold = TimeSpan.FromDays(3);

    public int GetEffectivePriority(PullRequest pr) => GetEffectivePriorityStatic(pr);
    public bool IsOverridden(PullRequest pr) => pr.Priority is not null;

    public static int GetEffectivePriorityStatic(PullRequest pr)
    {
        if (pr.Priority is int manual)
            return Math.Clamp(manual, PrPriority.Low, PrPriority.Urgent);

        return DerivePriority(pr);
    }

    private static int DerivePriority(PullRequest pr)
    {
        var signals = 0;

        if (string.Equals(pr.MergeableState, "conflicting", StringComparison.OrdinalIgnoreCase))
            signals++;

        if (string.Equals(pr.ChecksStatus, "FAILURE", StringComparison.OrdinalIgnoreCase))
            signals++;

        if (pr.Status == PrStatus.AwaitingReview
            && (DateTime.UtcNow - pr.UpdatedAt) > StaleAwaitingReviewThreshold)
        {
            signals++;
        }

        return signals >= 2 ? PrPriority.Urgent : signals == 1 ? PrPriority.High : PrPriority.Normal;
    }
}
