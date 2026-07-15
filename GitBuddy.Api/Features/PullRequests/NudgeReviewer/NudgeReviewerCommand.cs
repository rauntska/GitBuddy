using MediatR;
using System.Security.Claims;

namespace GitBuddy.Api.Features.PullRequests.NudgeReviewer;

public record NudgeReviewerCommand(
    int PullRequestId,
    List<string> Reviewers,
    bool AlsoComment,
    ClaimsPrincipal User) : IRequest<NudgeReviewerResult>;

public record NudgeReviewerResult(
    bool Success,
    string Message,
    List<string> Reviewers,
    DateTime NudgedAt,
    string? Error = null);
