using GitBuddy.Api.DTOs;
using GitBuddy.Api.Services;
using GitBuddy.Domain.Data;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.GetPendingReview;

public class GetPendingReviewHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<GetPendingReviewQuery, PendingReviewDto>
{
    public async Task<PendingReviewDto> Handle(GetPendingReviewQuery request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            throw new KeyNotFoundException("Pull request not found");

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
            return new PendingReviewDto(string.Empty, "NONE", new List<PendingReviewCommentDto>());
        }

        return new PendingReviewDto(
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
                c.UpdatedAt
            )).ToList()
        );
    }
}
