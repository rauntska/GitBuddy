using System.Security.Claims;
using MediatR;

namespace Graphite.Api.Features.PullRequests.GetFileContent;

public record GetFileContentQuery(
    int PullRequestId,
    string Path,
    int? OldStartLine,
    int? OldEndLine,
    int? NewStartLine,
    int? NewEndLine,
    ClaimsPrincipal User) : IRequest<GetFileContentResult>;

public record GetFileContentResult(
    List<LineContentDto> OldLines,
    List<LineContentDto> NewLines);

public record LineContentDto(
    int LineNumber,
    string Content);
