using GitBuddy.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Features.PullRequests.GetUnreadCount;

public class GetUnreadCountHandler(AppDbContext context) 
    : IRequestHandler<GetUnreadCountQuery, int>
{
    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var allPRs = await context.PullRequests
            .Include(pr => pr.Reviews)
            .Where(pr => !pr.IsMerged && pr.Status != "Closed" && pr.Status != "Merged" && pr.Status != "Draft")
            .ToListAsync(cancellationToken);

        var userReviewedPrIds = allPRs
            .Where(pr => pr.Reviews.Any(r => r.Reviewer == request.Username && 
                r.State is "Approved" or "ChangesRequested" or "Commented"))
            .Select(pr => pr.Id)
            .ToHashSet();

        var unreadCount = 0;

        foreach (var pr in allPRs)
        {
            var isAuthor = pr.Author == request.Username;
            var hasReviewed = userReviewedPrIds.Contains(pr.Id);
            var isReviewer = pr.Reviews.Any(r => r.Reviewer == request.Username);
            var needsMoreReviewers = pr.RequiredApprovingReviews.HasValue && 
                                      pr.CurrentApprovingReviews < pr.RequiredApprovingReviews.Value;

            if (isAuthor && pr.Status == "ChangesRequested")
            {
                unreadCount++;
            }
            else if (!isAuthor && !hasReviewed)
            {
                if (isReviewer || needsMoreReviewers)
                {
                    unreadCount++;
                }
            }
        }

        return unreadCount;
    }
}
