using System.Security.Claims;
using MediatR;

namespace Graphite.Api.Features.PullRequests.AddPendingReviewComment;

public record AddPendingReviewCommentCommand(
    int PullRequestId,
    string Body,
    string Path,
    int Line,
    ClaimsPrincipal User) : IRequest<AddPendingReviewCommentResult>;

public record AddPendingReviewCommentResult(
    string? CommentId,
    string? ReviewId,
    string? Path,
    int? Line,
    string Body,
    string? Author,
    string? AuthorAvatar,
    DateTime CreatedAt);
