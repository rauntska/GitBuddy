using System.Security.Claims;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.SubmitPendingReview;

public record SubmitPendingReviewCommand(
    int PullRequestId,
    string State,
    string? Body,
    ClaimsPrincipal User) : IRequest<SubmitPendingReviewResult>;

public record SubmitPendingReviewResult(
    bool Success,
    string Message,
    string? Error);
