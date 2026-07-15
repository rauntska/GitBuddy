using GitBuddy.Api.Services;
using GitBuddy.Domain.Data;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.NudgeReviewer;

public class NudgeReviewerHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    ITeamsNotificationService teamsNotificationService,
    INotificationService notificationService,
    IPullRequestValidationService validationService) : IRequestHandler<NudgeReviewerCommand, NudgeReviewerResult>
{
    private const int MinNudgeIntervalMinutes = 60;

    public async Task<NudgeReviewerResult> Handle(NudgeReviewerCommand request, CancellationToken cancellationToken)
    {
        if (request.Reviewers is null || request.Reviewers.Count == 0)
            return new NudgeReviewerResult(false, "No reviewers specified", [], DateTime.UtcNow);

        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);

        if (string.IsNullOrEmpty(accessToken))
            return new NudgeReviewerResult(false, "GitHub access token is required to nudge reviewers", [], DateTime.UtcNow);

        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            return new NudgeReviewerResult(false, "Pull request not found", [], DateTime.UtcNow);

        if (pr.LastNudgedAt is DateTime lastNudged
            && (DateTime.UtcNow - lastNudged).TotalMinutes < MinNudgeIntervalMinutes)
        {
            var waitMinutes = MinNudgeIntervalMinutes - (int)(DateTime.UtcNow - lastNudged).TotalMinutes;
            return new NudgeReviewerResult(
                false,
                $"This PR was nudged recently. Please wait ~{waitMinutes} minute(s) before nudging again.",
                [],
                lastNudged);
        }

        var config = await validationService.GetRequiredConfigAsync();

        try
        {
            if (request.AlsoComment)
            {
                var mentions = string.Join(" ", request.Reviewers.Select(r => $"@{r}"));
                var body = $"{mentions} — friendly nudge, could you take a look at this PR?";
                await gitHubService.AddPullRequestCommentAsync(
                    config.Organization,
                    pr.Repository,
                    pr.GitHubId,
                    body,
                    null,
                    null,
                    config,
                    accessToken);
            }

            await teamsNotificationService.SendNudgeAsync(pr, request.Reviewers, user.Username);

            var nudgedAt = DateTime.UtcNow;
            pr.LastNudgedAt = nudgedAt;
            await context.SaveChangesAsync(cancellationToken);

            await notificationService.BroadcastReviewerNudgedAsync(pr.Id, request.Reviewers, user.Username);

            return new NudgeReviewerResult(true, "Reviewers nudged", request.Reviewers, nudgedAt);
        }
        catch (Exception ex)
        {
            return new NudgeReviewerResult(false, "Failed to nudge reviewers", [], DateTime.UtcNow, ex.Message);
        }
    }
}
