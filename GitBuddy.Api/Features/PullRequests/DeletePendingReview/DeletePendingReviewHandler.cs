using GitBuddy.Api.Services;
using GitBuddy.Domain.Data;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.DeletePendingReview;

public class DeletePendingReviewHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<DeletePendingReviewCommand, DeletePendingReviewResult>
{
    public async Task<DeletePendingReviewResult> Handle(DeletePendingReviewCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            return new DeletePendingReviewResult(false, "Pull request not found", null);

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
            return new DeletePendingReviewResult(false, "No pending review found to delete", null);
        }

        try
        {
            await gitHubService.DeletePendingReviewAsync(
                config.Organization,
                pr.Repository,
                pendingReview.GitHubId,
                accessToken!
            );

            return new DeletePendingReviewResult(true, "Pending review deleted successfully", null);
        }
        catch (Exception ex)
        {
            return new DeletePendingReviewResult(false, "Failed to delete pending review", ex.Message);
        }
    }
}
