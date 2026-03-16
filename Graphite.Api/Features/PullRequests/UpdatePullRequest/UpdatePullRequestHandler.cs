using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Features.PullRequests.UpdatePullRequest;

public class UpdatePullRequestHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<UpdatePullRequestCommand, UpdatePullRequestResult>
{
    public async Task<UpdatePullRequestResult> Handle(UpdatePullRequestCommand request, CancellationToken cancellationToken)
    {
        if (request.Title == null && request.Body == null)
            return new UpdatePullRequestResult(false, "No updates provided", null, null, null);

        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            return new UpdatePullRequestResult(false, "Pull request not found", null, null, null);

        if (pr.IsMerged)
            return new UpdatePullRequestResult(false, "Cannot update merged pull request", pr.Title, pr.Description, null);

        var config = await validationService.GetRequiredConfigAsync();

        try
        {
            await gitHubService.UpdatePullRequestAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                request.Title,
                request.Body,
                accessToken!
            );

            if (request.Title != null)
                pr.Title = request.Title;
            if (request.Body != null)
                pr.Description = request.Body;
            
            pr.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            return new UpdatePullRequestResult(true, "Pull request updated successfully", pr.Title, pr.Description, null);
        }
        catch (Exception ex)
        {
            return new UpdatePullRequestResult(false, "Failed to update pull request", pr.Title, pr.Description, ex.Message);
        }
    }
}
