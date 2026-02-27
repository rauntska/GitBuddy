using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;

namespace Graphite.Api.Features.PullRequests.AddPendingReviewComment;

public class AddPendingReviewCommentHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<AddPendingReviewCommentCommand, AddPendingReviewCommentResult>
{
    public async Task<AddPendingReviewCommentResult> Handle(AddPendingReviewCommentCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            throw new KeyNotFoundException("Pull request not found");

        var config = await validationService.GetRequiredConfigAsync();

        var comment = await gitHubService.AddPendingReviewCommentAsync(
            config.Organization,
            pr.Repository,
            pr.GitHubId,
            request.Body,
            request.Path,
            request.Line,
            config,
            accessToken!
        );

        return new AddPendingReviewCommentResult(
            comment.GitHubId,
            comment.ReviewId,
            comment.Path,
            comment.Line,
            comment.Body,
            comment.Author,
            comment.AuthorAvatar,
            comment.CreatedAt
        );
    }
}
