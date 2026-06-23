using MediatR;
using System.Security.Claims;

namespace GitBuddy.Api.Features.PullRequests.RemoveReviewer;

public record RemoveReviewerCommand(int PullRequestId, string Username, ClaimsPrincipal User) 
    : IRequest<RemoveReviewerResult>;

public record RemoveReviewerResult(bool Success, string Message, string? Error = null);
