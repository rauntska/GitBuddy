using System.Security.Claims;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.SubmitReview;

public record SubmitReviewCommand(
    int PullRequestId,
    string State,
    string? Body,
    ClaimsPrincipal User) : IRequest<SubmitReviewResult>;

public record SubmitReviewResult(
    bool Success,
    string Message,
    string? Error);
