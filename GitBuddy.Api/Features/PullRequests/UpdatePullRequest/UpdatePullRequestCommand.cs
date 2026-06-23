using System.Security.Claims;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.UpdatePullRequest;

public record UpdatePullRequestCommand(
    int PullRequestId,
    string? Title,
    string? Body,
    ClaimsPrincipal User) : IRequest<UpdatePullRequestResult>;

public record UpdatePullRequestResult(
    bool Success,
    string Message,
    string? Title,
    string? Body,
    string? Error);
