using System.Security.Claims;
using MediatR;

namespace Graphite.Api.Features.PullRequests.PublishDraftPR;

public record PublishDraftPRCommand(
    int PullRequestId,
    ClaimsPrincipal User) : IRequest<PublishDraftPRResult>;

public record PublishDraftPRResult(
    bool Success,
    string Message,
    bool Draft,
    string? Status,
    string? Error);
