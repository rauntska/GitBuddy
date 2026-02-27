using Graphite.Api.DTOs;
using Graphite.Api.Services;
using Graphite.Domain.Data;
using Graphite.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Features.PullRequests.AddCommentReply;

public class AddCommentReplyHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<AddCommentReplyCommand, AddCommentReplyResult>
{
    public async Task<AddCommentReplyResult> Handle(AddCommentReplyCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests
            .Include(p => p.ReviewThreads)
            .FirstOrDefaultAsync(p => p.Id == request.PullRequestId, cancellationToken);
        
        if (pr == null)
            throw new KeyNotFoundException("Pull request not found");

        var config = await validationService.GetRequiredConfigAsync();

        var comment = await gitHubService.AddPullRequestReviewThreadReplyAsync(
            config.Organization,
            pr.Repository,
            pr.GitHubId,
            request.ReviewThreadId,
            request.Body,
            config,
            accessToken!
        );

        if (comment.IsPending)
        {
            return new AddCommentReplyResult(
                true,
                null,
                new PendingReplyDto(
                    comment.PendingReviewId,
                    comment.CommentNodeId,
                    request.ReviewThreadId,
                    comment.Author,
                    comment.AuthorAvatar,
                    comment.Body,
                    comment.CreatedAt,
                    comment.UpdatedAt
                )
            );
        }

        var reviewThread = pr.ReviewThreads.FirstOrDefault(rt => rt.GitHubId == request.ReviewThreadId);
        if (reviewThread != null)
        {
            reviewThread.CommentCount++;
            reviewThread.UpdatedAt = comment.UpdatedAt;

            var dbComment = new Comment
            {
                PullRequestId = request.PullRequestId,
                ReviewThreadId = reviewThread.Id,
                GitHubId = comment.GitHubId,
                Author = comment.Author,
                AuthorAvatar = comment.AuthorAvatar,
                Body = comment.Body,
                Path = comment.Path,
                Line = comment.Line,
                IsOutdated = false,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };
            context.Comments.Add(dbComment);
            await context.SaveChangesAsync(cancellationToken);

            return new AddCommentReplyResult(
                false,
                new CommentDto(
                    dbComment.Id,
                    dbComment.GitHubId,
                    dbComment.ReviewThreadId,
                    dbComment.Author,
                    dbComment.AuthorAvatar,
                    dbComment.Body,
                    dbComment.CreatedAt,
                    dbComment.UpdatedAt,
                    dbComment.Path,
                    dbComment.Line,
                    dbComment.IsOutdated,
                    dbComment.EditedAt,
                    dbComment.EditCount,
                    dbComment.ReplyToCommentId,
                    null
                ),
                null
            );
        }

        return new AddCommentReplyResult(
            false,
            new CommentDto(
                0,
                comment.GitHubId,
                null,
                comment.Author,
                comment.AuthorAvatar,
                comment.Body,
                comment.CreatedAt,
                comment.UpdatedAt,
                comment.Path,
                comment.Line,
                comment.IsOutdated,
                null,
                0,
                null,
                null
            ),
            null
        );
    }
}
