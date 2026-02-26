using Graphite.Api.Services;
using Graphite.Domain.Data;
using Graphite.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Features.PullRequests.AddComment;

public class AddCommentHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<AddCommentCommand, AddCommentResult>
{
    public async Task<AddCommentResult> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests
            .Include(p => p.ReviewThreads)
            .FirstOrDefaultAsync(p => p.Id == request.PullRequestId, cancellationToken);
        
        if (pr == null)
            throw new KeyNotFoundException("Pull request not found");

        var config = await validationService.GetRequiredConfigAsync();

        if (!string.IsNullOrEmpty(request.Path) && request.Line.HasValue)
        {
            var pendingComment = await gitHubService.AddPendingReviewCommentAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                request.Body,
                request.Path,
                request.Line.Value,
                config,
                accessToken!
            );

            var pendingReview = await context.PendingReviews
                .FirstOrDefaultAsync(pr => pr.PullRequestId == request.PullRequestId && pr.UserId == user.Id, cancellationToken);

            if (pendingReview == null && !string.IsNullOrEmpty(pendingComment.ReviewId))
            {
                pendingReview = new PendingReview
                {
                    PullRequestId = request.PullRequestId,
                    UserId = user.Id,
                    GitHubReviewId = pendingComment.ReviewId,
                    CreatedAt = DateTime.UtcNow
                };
                context.PendingReviews.Add(pendingReview);
                await context.SaveChangesAsync(cancellationToken);
            }

            if (pendingReview != null)
            {
                var dbPendingComment = new PendingComment
                {
                    PendingReviewId = pendingReview.Id,
                    Body = pendingComment.Body,
                    Path = pendingComment.Path ?? request.Path,
                    Line = pendingComment.Line ?? request.Line.Value,
                    CreatedAt = pendingComment.CreatedAt
                };
                context.PendingComments.Add(dbPendingComment);
                await context.SaveChangesAsync(cancellationToken);

                return new AddCommentResult(
                    true,
                    dbPendingComment.Id,
                    pendingComment.GitHubId,
                    pendingComment.ReviewId,
                    pendingComment.ThreadId,
                    pendingComment.Path,
                    pendingComment.Line,
                    pendingComment.Body,
                    pendingComment.Author,
                    pendingComment.AuthorAvatar,
                    pendingComment.CreatedAt,
                    pendingComment.UpdatedAt
                );
            }

            return new AddCommentResult(
                true,
                null,
                pendingComment.GitHubId,
                pendingComment.ReviewId,
                pendingComment.ThreadId,
                pendingComment.Path,
                pendingComment.Line,
                pendingComment.Body,
                pendingComment.Author,
                pendingComment.AuthorAvatar,
                pendingComment.CreatedAt,
                pendingComment.UpdatedAt
            );
        }

        var comment = await gitHubService.AddPullRequestCommentAsync(
            config.Organization,
            pr.Repository,
            pr.GitHubId,
            request.Body,
            null,
            null,
            config,
            accessToken!
        );

        return new AddCommentResult(
            false,
            null,
            comment.GitHubId.ToString(),
            null,
            null,
            comment.Path,
            comment.Line,
            comment.Body,
            comment.Author,
            comment.AuthorAvatar,
            comment.CreatedAt,
            comment.UpdatedAt
        );
    }
}
