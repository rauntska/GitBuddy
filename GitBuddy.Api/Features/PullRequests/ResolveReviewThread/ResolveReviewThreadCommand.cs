using System.Security.Claims;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.ResolveReviewThread;

public record ResolveReviewThreadCommand(
    int PullRequestId,
    string ThreadId,
    bool Resolved,
    ClaimsPrincipal User) : IRequest<ResolveReviewThreadResult>;

public record ResolveReviewThreadResult(
    bool Success,
    string Message,
    string? Error);
