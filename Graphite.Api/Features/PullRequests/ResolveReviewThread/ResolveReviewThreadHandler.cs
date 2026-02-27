using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;

namespace Graphite.Api.Features.PullRequests.ResolveReviewThread;

public class ResolveReviewThreadHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<ResolveReviewThreadCommand, ResolveReviewThreadResult>
{
    public async Task<ResolveReviewThreadResult> Handle(ResolveReviewThreadCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            return new ResolveReviewThreadResult(false, "Pull request not found", null);

        var config = await validationService.GetRequiredConfigAsync();

        try
        {
            await gitHubService.ResolveReviewThreadAsync(
                config.Organization,
                pr.Repository,
                request.ThreadId,
                request.Resolved,
                config,
                accessToken!
            );

            return new ResolveReviewThreadResult(true, "Thread resolved successfully", null);
        }
        catch (Exception ex)
        {
            return new ResolveReviewThreadResult(false, "Failed to resolve thread", ex.Message);
        }
    }
}
