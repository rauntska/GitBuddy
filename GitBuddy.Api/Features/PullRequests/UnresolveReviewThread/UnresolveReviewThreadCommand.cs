using System.Security.Claims;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.UnresolveReviewThread;

public record UnresolveReviewThreadCommand(
    int PullRequestId,
    string ThreadId,
    ClaimsPrincipal User) : IRequest<UnresolveReviewThreadResult>;

public record UnresolveReviewThreadResult(
    bool Success,
    string Message,
    string? Error);
