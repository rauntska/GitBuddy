using GitBuddy.Api.DTOs;
using GitBuddy.Api.Services;
using GitBuddy.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Features.PullRequests.GetReviewers;

public class GetReviewersHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<GetReviewersQuery, List<ReviewerStatusDto>>
{
    public async Task<List<ReviewerStatusDto>> Handle(GetReviewersQuery request, CancellationToken cancellationToken)
    {
        var pr = await context.PullRequests
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == request.PullRequestId, cancellationToken);

        if (pr == null)
            return new List<ReviewerStatusDto>();

        var config = await validationService.GetRequiredConfigAsync();
        
        var requestedReviewers = await gitHubService.GetRequestedReviewersAsync(
            config.Organization,
            pr.Repository,
            pr.GitHubId,
            config
        );

        var reviewersDict = new Dictionary<string, ReviewerStatusDto>();

        foreach (var user in requestedReviewers.Users)
        {
            reviewersDict[user.Username] = new ReviewerStatusDto(
                user.Username,
                user.Avatar,
                null,
                null,
                true,
                null
            );
        }

        foreach (var review in pr.Reviews.Where(r => r.SubmittedAt.HasValue))
        {
            var existingReview = reviewersDict.GetValueOrDefault(review.Reviewer);
            if (existingReview != null)
            {
                reviewersDict[review.Reviewer] = existingReview with
                {
                    ReviewState = review.State,
                    ReviewedAt = review.SubmittedAt
                };
            }
            else
            {
                reviewersDict[review.Reviewer] = new ReviewerStatusDto(
                    review.Reviewer,
                    review.ReviewerAvatar,
                    review.State,
                    review.SubmittedAt,
                    false,
                    null
                );
            }
        }

        return reviewersDict.Values
            .OrderByDescending(r => r.IsRequested)
            .ThenByDescending(r => r.ReviewedAt)
            .ToList();
    }
}
