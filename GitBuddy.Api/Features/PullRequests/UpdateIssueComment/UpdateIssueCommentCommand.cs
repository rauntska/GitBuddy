using System.Security.Claims;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.UpdateIssueComment;

public record UpdateIssueCommentCommand(
    int PullRequestId,
    long CommentId,
    string Body,
    ClaimsPrincipal User) : IRequest<UpdateIssueCommentResult>;

public record UpdateIssueCommentResult(
    bool Success,
    string? Message,
    string? Error = null);
