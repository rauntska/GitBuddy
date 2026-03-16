using System.Security.Claims;
using MediatR;

namespace Graphite.Api.Features.PullRequests.DeleteIssueComment;

public record DeleteIssueCommentCommand(
    int PullRequestId,
    long CommentId,
    ClaimsPrincipal User) : IRequest<DeleteIssueCommentResult>;

public record DeleteIssueCommentResult(
    bool Success,
    string? Message,
    string? Error = null);
