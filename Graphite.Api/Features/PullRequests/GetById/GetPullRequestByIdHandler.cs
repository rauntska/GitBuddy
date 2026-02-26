using Graphite.Api.DTOs;
using Graphite.Api.Extensions;
using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Features.PullRequests.GetById;

public class GetPullRequestByIdHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<GetPullRequestByIdQuery, PRDetailDto?>
{
    public async Task<PRDetailDto?> Handle(GetPullRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var pr = await context.PullRequests
            .Include(p => p.Reviews)
            .Include(p => p.ReviewThreads)
            .Include(p => p.CheckRuns)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (pr == null)
            return null;

        var files = await context.FileDiffs
            .Where(f => f.PullRequestId == request.Id)
            .ToListAsync(cancellationToken);

        var comments = await context.Comments
            .Where(c => c.PullRequestId == request.Id)
            .ToListAsync(cancellationToken);

        var user = await validationService.GetUserAsync(request.User);
        List<Domain.Models.UserFileViewedState>? viewedStates = null;
        PendingReviewDto? pendingReviewDto = null;

        if (user != null)
        {
            viewedStates = await context.UserFileViewedStates
                .Where(uvs => uvs.UserId == user.Id && files.Select(f => f.Id).Contains(uvs.FileDiffId))
                .ToListAsync(cancellationToken);

            if (!string.IsNullOrEmpty(user.AccessToken))
            {
                var config = await validationService.GetConfigAsync();
                if (config != null)
                {
                    try
                    {
                        var pendingReview = await gitHubService.GetPendingReviewAsync(
                            config.Organization,
                            pr.Repository,
                            pr.GitHubId,
                            user.Username,
                            user.AccessToken
                        );

                        if (pendingReview != null)
                        {
                            pendingReviewDto = new PendingReviewDto(
                                pendingReview.GitHubId,
                                pendingReview.State,
                                pendingReview.Comments.Select(c => new PendingReviewCommentDto(
                                    c.GitHubId,
                                    c.Path ?? string.Empty,
                                    c.Line,
                                    c.Body,
                                    c.Author,
                                    c.AuthorAvatar,
                                    c.CreatedAt,
                                    c.UpdatedAt,
                                    c.ThreadId
                                )).ToList()
                            );
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        return pr.ToDetailDto(files, comments, viewedStates, pendingReviewDto, 
            pr.RequiredApprovingReviews, pr.CurrentApprovingReviews, 
            pr.HasUnresolvedThreads, pr.IsMergeReady, pr.MergeBlockReason);
    }
}
