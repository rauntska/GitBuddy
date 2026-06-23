using System.Security.Claims;
using GitBuddy.Api.DTOs;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.GetPendingReview;

public record GetPendingReviewQuery(
    int PullRequestId,
    ClaimsPrincipal User) : IRequest<PendingReviewDto>;
