using MediatR;
using System.Security.Claims;

namespace GitBuddy.Api.Features.PullRequests.SetPriority;

public record SetPriorityCommand(int PullRequestId, int? Priority, ClaimsPrincipal User)
    : IRequest<SetPriorityResult>;

public record SetPriorityResult(
    bool Success,
    string Message,
    int Priority,
    bool Overridden,
    string? Error = null);
