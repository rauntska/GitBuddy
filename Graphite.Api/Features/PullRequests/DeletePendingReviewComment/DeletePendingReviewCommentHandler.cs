using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Features.PullRequests.DeletePendingReviewComment;

public class DeletePendingReviewCommentHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<DeletePendingReviewCommentCommand, DeletePendingReviewCommentResult>
{
    public async Task<DeletePendingReviewCommentResult> Handle(DeletePendingReviewCommentCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            throw new KeyNotFoundException("Pull request not found");

        var config = await validationService.GetRequiredConfigAsync();

        var success = await gitHubService.DeletePendingReviewCommentAsync(
            config.Organization,
            pr.Repository,
            request.CommentId,
            accessToken!
        );

        if (success)
        {
            var pendingComment = await context.PendingComments
                .FirstOrDefaultAsync(c => c.PendingReview.PullRequestId == request.PullRequestId && c.PendingReview.UserId == user.Id, cancellationToken);
            
            if (pendingComment != null)
            {
                context.PendingComments.Remove(pendingComment);
                await context.SaveChangesAsync(cancellationToken);
            }

            return new DeletePendingReviewCommentResult(true, "Comment deleted successfully");
        }

        return new DeletePendingReviewCommentResult(false, "Failed to delete comment");
    }
}
