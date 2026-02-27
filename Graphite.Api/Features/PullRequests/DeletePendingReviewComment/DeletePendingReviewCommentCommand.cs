using System.Security.Claims;
using MediatR;

namespace Graphite.Api.Features.PullRequests.DeletePendingReviewComment;

public record DeletePendingReviewCommentCommand(
    int PullRequestId,
    string CommentId,
    ClaimsPrincipal User) : IRequest<DeletePendingReviewCommentResult>;

public record DeletePendingReviewCommentResult(
    bool Success,
    string Message);
