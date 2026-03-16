using Graphite.Api.DTOs;
using MediatR;

namespace Graphite.Api.Features.PullRequests.GetReviewTimeline;

public record GetReviewTimelineQuery(int PullRequestId) : IRequest<ReviewTimelineDto>;
