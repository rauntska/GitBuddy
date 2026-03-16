using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;

namespace Graphite.Api.Features.PullRequests.RemoveReviewer;

public class RemoveReviewerHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<RemoveReviewerCommand, RemoveReviewerResult>
{
    public async Task<RemoveReviewerResult> Handle(RemoveReviewerCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            return new RemoveReviewerResult(false, "Pull request not found", null);

        var config = await validationService.GetRequiredConfigAsync();

        try
        {
            await gitHubService.RemoveReviewersAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                request.Username,
                accessToken!
            );

            return new RemoveReviewerResult(true, "Reviewer removed successfully", null);
        }
        catch (Exception ex)
        {
            return new RemoveReviewerResult(false, "Failed to remove reviewer", ex.Message);
        }
    }
}
