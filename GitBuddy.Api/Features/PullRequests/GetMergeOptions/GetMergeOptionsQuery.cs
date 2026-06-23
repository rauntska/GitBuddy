using System.Security.Claims;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.GetMergeOptions;

public record GetMergeOptionsQuery(
    int PullRequestId,
    ClaimsPrincipal User) : IRequest<GetMergeOptionsResult>;

public record GetMergeOptionsResult(
    bool MergeCommitAllowed,
    bool SquashMergeAllowed,
    bool RebaseMergeAllowed,
    string? DefaultMergeMethod,
    string? MergeableState,
    bool IsMerged,
    bool IsDraft);
