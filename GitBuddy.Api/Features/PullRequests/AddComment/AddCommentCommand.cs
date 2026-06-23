using System.Security.Claims;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.AddComment;

public record AddCommentCommand(
    int PullRequestId, 
    string Body, 
    string? Path, 
    int? Line,
    ClaimsPrincipal User) : IRequest<AddCommentResult>;

public record AddCommentResult(
    bool IsPending,
    int? Id,
    string? GitHubId,
    string? PendingReviewId,
    string? ReviewThreadId,
    string? Path,
    int? Line,
    string Body,
    string? Author,
    string? AuthorAvatar,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
