using System.Security.Claims;
using MediatR;

namespace Graphite.Api.Features.PullRequests.UpdateFileViewedState;

public record UpdateFileViewedStateCommand(
    int PullRequestId,
    string Path,
    bool Viewed,
    ClaimsPrincipal User) : IRequest<Unit>;
