using GitBuddy.Api.DTOs;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.GetReviewTimeline;

public record GetReviewTimelineQuery(int PullRequestId) : IRequest<ReviewTimelineDto>;
