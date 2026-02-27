using System.Security.Claims;
using Graphite.Api.DTOs;
using MediatR;

namespace Graphite.Api.Features.PullRequests.GetPendingReview;

public record GetPendingReviewQuery(
    int PullRequestId,
    ClaimsPrincipal User) : IRequest<PendingReviewDto>;
