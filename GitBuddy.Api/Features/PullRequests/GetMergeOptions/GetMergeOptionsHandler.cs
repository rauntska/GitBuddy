using GitBuddy.Api.Services;
using GitBuddy.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Features.PullRequests.GetMergeOptions;

public class GetMergeOptionsHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<GetMergeOptionsQuery, GetMergeOptionsResult>
{
    public async Task<GetMergeOptionsResult> Handle(GetMergeOptionsQuery request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            throw new KeyNotFoundException("Pull request not found");

        var config = await validationService.GetRequiredConfigAsync();

        var mergeOptions = await gitHubService.GetRepositoryMergeOptionsAsync(
            config.Organization,
            pr.Repository,
            accessToken!
        );

        return new GetMergeOptionsResult(
            mergeOptions.MergeCommitAllowed,
            mergeOptions.SquashMergeAllowed,
            mergeOptions.RebaseMergeAllowed,
            mergeOptions.DefaultMergeMethod,
            pr.MergeableState,
            pr.IsMerged,
            pr.Draft
        );
    }
}
