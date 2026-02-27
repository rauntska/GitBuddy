using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;

namespace Graphite.Api.Features.PullRequests.UnresolveReviewThread;

public class UnresolveReviewThreadHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<UnresolveReviewThreadCommand, UnresolveReviewThreadResult>
{
    public async Task<UnresolveReviewThreadResult> Handle(UnresolveReviewThreadCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            return new UnresolveReviewThreadResult(false, "Pull request not found", null);

        var config = await validationService.GetRequiredConfigAsync();

        try
        {
            await gitHubService.UnresolveReviewThreadAsync(
                config.Organization,
                pr.Repository,
                request.ThreadId,
                config,
                accessToken!
            );

            return new UnresolveReviewThreadResult(true, "Thread unresolved successfully", null);
        }
        catch (Exception ex)
        {
            return new UnresolveReviewThreadResult(false, "Failed to unresolve thread", ex.Message);
        }
    }
}
