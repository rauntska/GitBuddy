using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Features.PullRequests.PublishDraftPR;

public class PublishDraftPRHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<PublishDraftPRCommand, PublishDraftPRResult>
{
    public async Task<PublishDraftPRResult> Handle(PublishDraftPRCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            return new PublishDraftPRResult(false, "Pull request not found", pr?.Draft ?? false, null, null);

        if (!pr.Draft)
            return new PublishDraftPRResult(false, "Pull request is not a draft", pr.Draft, pr.Status, null);

        var config = await validationService.GetRequiredConfigAsync();

        try
        {
            await gitHubService.PublishDraftPullRequestAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                config,
                accessToken!
            );

            pr.Draft = false;
            pr.Status = "AwaitingReview";
            await context.SaveChangesAsync(cancellationToken);

            return new PublishDraftPRResult(true, "Draft PR published successfully", false, "AwaitingReview", null);
        }
        catch (Exception ex)
        {
            return new PublishDraftPRResult(false, "Failed to publish draft PR", pr.Draft, pr.Status, ex.Message);
        }
    }
}
