using GitBuddy.Api.Services;
using GitBuddy.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Features.PullRequests.DeleteIssueComment;

public class DeleteIssueCommentHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService)
    : IRequestHandler<DeleteIssueCommentCommand, DeleteIssueCommentResult>
{
    public async Task<DeleteIssueCommentResult> Handle(DeleteIssueCommentCommand request, CancellationToken cancellationToken)
    {
        var (_, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests
            .FirstOrDefaultAsync(p => p.Id == request.PullRequestId, cancellationToken);
        
        if (pr == null)
            return new DeleteIssueCommentResult(false, "Pull request not found");

        var config = await validationService.GetRequiredConfigAsync();

        var success = await gitHubService.DeleteIssueCommentAsync(
            config.Organization,
            pr.Repository,
            request.CommentId,
            accessToken!
        );

        if (!success)
            return new DeleteIssueCommentResult(false, "Failed to delete comment");

        return new DeleteIssueCommentResult(true, "Comment deleted successfully");
    }
}
