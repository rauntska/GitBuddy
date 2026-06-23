using GitBuddy.Api.Services;
using GitBuddy.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Features.PullRequests.UpdateIssueComment;

public class UpdateIssueCommentHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService)
    : IRequestHandler<UpdateIssueCommentCommand, UpdateIssueCommentResult>
{
    public async Task<UpdateIssueCommentResult> Handle(UpdateIssueCommentCommand request, CancellationToken cancellationToken)
    {
        var (_, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests
            .FirstOrDefaultAsync(p => p.Id == request.PullRequestId, cancellationToken);
        
        if (pr == null)
            return new UpdateIssueCommentResult(false, "Pull request not found");

        var config = await validationService.GetRequiredConfigAsync();

        var success = await gitHubService.UpdateIssueCommentAsync(
            config.Organization,
            pr.Repository,
            request.CommentId,
            request.Body,
            accessToken!
        );

        if (!success)
            return new UpdateIssueCommentResult(false, "Failed to update comment");

        return new UpdateIssueCommentResult(true, "Comment updated successfully");
    }
}
