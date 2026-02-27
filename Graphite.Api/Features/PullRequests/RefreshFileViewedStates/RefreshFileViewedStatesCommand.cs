using System.Security.Claims;
using MediatR;

namespace Graphite.Api.Features.PullRequests.RefreshFileViewedStates;

public record RefreshFileViewedStatesCommand(
    int PullRequestId,
    ClaimsPrincipal User) : IRequest<RefreshFileViewedStatesResult>;

public record RefreshFileViewedStatesResult(
    List<FileViewedStateDto> Files);

public record FileViewedStateDto(
    int Id,
    string Path,
    string? OldPath,
    string Status,
    int Additions,
    int Deletions,
    int Changes,
    string? Patch,
    string? Language,
    string? ViewedState,
    DateTime? ViewedAt);
