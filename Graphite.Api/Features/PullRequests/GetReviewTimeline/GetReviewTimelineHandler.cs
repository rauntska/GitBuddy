using Graphite.Api.DTOs;
using Graphite.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Features.PullRequests.GetReviewTimeline;

public class GetReviewTimelineHandler(AppDbContext context) 
    : IRequestHandler<GetReviewTimelineQuery, ReviewTimelineDto>
{
    public async Task<ReviewTimelineDto> Handle(GetReviewTimelineQuery request, CancellationToken cancellationToken)
    {
        var pr = await context.PullRequests
            .Include(p => p.Reviews)
            .Include(p => p.ReviewThreads)
            .FirstOrDefaultAsync(p => p.Id == request.PullRequestId, cancellationToken);

        if (pr == null)
            return new ReviewTimelineDto(new List<ReviewEventDto>());

        var events = new List<ReviewEventDto>();
        var eventId = 1;

        foreach (var review in pr.Reviews.Where(r => r.SubmittedAt.HasValue).OrderBy(r => r.SubmittedAt))
        {
            var summary = review.State.ToUpper() switch
            {
                "APPROVED" => $"{review.Reviewer} approved this pull request",
                "CHANGES_REQUESTED" => $"{review.Reviewer} requested changes",
                "COMMENTED" => $"{review.Reviewer} reviewed this pull request",
                _ => $"{review.Reviewer} submitted a review"
            };

            events.Add(new ReviewEventDto(
                eventId++,
                "REVIEW_SUBMITTED",
                review.Reviewer,
                review.ReviewerAvatar,
                review.SubmittedAt!.Value,
                review.State,
                summary,
                null,
                null
            ));
        }

        foreach (var thread in pr.ReviewThreads.OrderBy(t => t.CreatedAt))
        {
            events.Add(new ReviewEventDto(
                eventId++,
                "THREAD_CREATED",
                thread.FirstCommentAuthor,
                null,
                thread.CreatedAt,
                thread.State,
                $"Started a conversation on {thread.Path}",
                thread.Id,
                thread.Path
            ));

            if (thread.IsResolved && thread.UpdatedAt.HasValue && thread.UpdatedAt != thread.CreatedAt)
            {
                events.Add(new ReviewEventDto(
                    eventId++,
                    "THREAD_RESOLVED",
                    thread.FirstCommentAuthor,
                    null,
                    thread.UpdatedAt.Value,
                    "RESOLVED",
                    $"Resolved conversation on {thread.Path}",
                    thread.Id,
                    thread.Path
                ));
            }
        }

        var orderedEvents = events
            .OrderBy(e => e.Timestamp)
            .ToList();

        return new ReviewTimelineDto(orderedEvents);
    }
}
