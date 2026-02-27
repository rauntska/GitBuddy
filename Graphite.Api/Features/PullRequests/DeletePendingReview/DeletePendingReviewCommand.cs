using System.Security.Claims;
using MediatR;

namespace Graphite.Api.Features.PullRequests.DeletePendingReview;

public record DeletePendingReviewCommand(
    int PullRequestId,
    ClaimsPrincipal User) : IRequest<DeletePendingReviewResult>;

public record DeletePendingReviewResult(
    bool Success,
    string Message,
    string? Error);
