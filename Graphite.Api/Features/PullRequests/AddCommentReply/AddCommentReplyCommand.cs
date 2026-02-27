using System.Security.Claims;
using Graphite.Api.DTOs;
using MediatR;

namespace Graphite.Api.Features.PullRequests.AddCommentReply;

public record AddCommentReplyCommand(
    int PullRequestId,
    string ReviewThreadId,
    string Body,
    ClaimsPrincipal User) : IRequest<AddCommentReplyResult>;

public record AddCommentReplyResult(
    bool IsPending,
    CommentDto? Comment,
    PendingReplyDto? PendingReply);

public record PendingReplyDto(
    string? PendingReviewId,
    string? CommentNodeId,
    string ReviewThreadId,
    string? Author,
    string? AuthorAvatar,
    string Body,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
