using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;

namespace Graphite.Api.Features.PullRequests.AddReviewers;

public class AddReviewersHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<AddReviewersCommand, AddReviewersResult>
{
    public async Task<AddReviewersResult> Handle(AddReviewersCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            return new AddReviewersResult(false, "Pull request not found", null);

        var config = await validationService.GetRequiredConfigAsync();

        try
        {
            await gitHubService.RequestReviewersAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                request.Reviewers,
                accessToken!
            );

            return new AddReviewersResult(true, "Reviewers added successfully", null);
        }
        catch (Exception ex)
        {
            return new AddReviewersResult(false, "Failed to add reviewers", ex.Message);
        }
    }
}
