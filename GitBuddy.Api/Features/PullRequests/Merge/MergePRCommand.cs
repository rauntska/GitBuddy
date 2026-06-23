using System.Security.Claims;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.Merge;

public record MergePRCommand(
    int PullRequestId,
    string? MergeMethod,
    string? CommitTitle,
    string? CommitMessage,
    ClaimsPrincipal User) : IRequest<MergePRResult>;

public record MergePRResult(
    bool Success,
    string Message,
    bool IsMerged,
    DateTime? MergedAt,
    int? StatusCode,
    string? Error);
