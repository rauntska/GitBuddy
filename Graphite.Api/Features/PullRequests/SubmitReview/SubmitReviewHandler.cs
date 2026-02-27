using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;

namespace Graphite.Api.Features.PullRequests.SubmitReview;

public class SubmitReviewHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<SubmitReviewCommand, SubmitReviewResult>
{
    public async Task<SubmitReviewResult> Handle(SubmitReviewCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            return new SubmitReviewResult(false, "Pull request not found", null);

        var config = await validationService.GetRequiredConfigAsync();

        try
        {
            await gitHubService.SubmitPullRequestReviewAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                request.State,
                request.Body,
                config,
                accessToken!
            );

            return new SubmitReviewResult(true, "Review submitted successfully", null);
        }
        catch (Exception ex)
        {
            return new SubmitReviewResult(false, "Failed to submit review to GitHub", ex.Message);
        }
    }
}
