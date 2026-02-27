using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;

namespace Graphite.Api.Features.PullRequests.SubmitPendingReview;

public class SubmitPendingReviewHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<SubmitPendingReviewCommand, SubmitPendingReviewResult>
{
    public async Task<SubmitPendingReviewResult> Handle(SubmitPendingReviewCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            return new SubmitPendingReviewResult(false, "Pull request not found", null);

        var config = await validationService.GetRequiredConfigAsync();

        var pendingReview = await gitHubService.GetPendingReviewAsync(
            config.Organization,
            pr.Repository,
            pr.GitHubId,
            user.Username,
            accessToken!
        );

        if (pendingReview == null)
        {
            return new SubmitPendingReviewResult(false, "No pending review found to submit", null);
        }

        try
        {
            await gitHubService.SubmitPendingReviewAsync(
                config.Organization,
                pr.Repository,
                pendingReview.GitHubId,
                request.State,
                request.Body,
                accessToken!
            );

            return new SubmitPendingReviewResult(true, "Pending review submitted successfully", null);
        }
        catch (Exception ex)
        {
            return new SubmitPendingReviewResult(false, "Failed to submit pending review", ex.Message);
        }
    }
}
