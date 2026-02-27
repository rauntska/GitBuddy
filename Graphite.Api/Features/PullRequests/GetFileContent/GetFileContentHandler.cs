using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Features.PullRequests.GetFileContent;

public class GetFileContentHandler(
    AppDbContext context,
    IGitHubGraphQLService gitHubGraphQLService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<GetFileContentQuery, GetFileContentResult>
{
    public async Task<GetFileContentResult> Handle(GetFileContentQuery request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            throw new KeyNotFoundException("Pull request not found");

        var config = await validationService.GetRequiredConfigAsync();

        var oldLines = new List<LineContentDto>();
        var newLines = new List<LineContentDto>();

        if (request.OldStartLine.HasValue && request.OldEndLine.HasValue && !string.IsNullOrEmpty(pr.TargetBranch))
        {
            var oldContent = await gitHubGraphQLService.GetFileContentAsync(
                config.Organization,
                pr.Repository,
                pr.TargetBranch,
                request.Path,
                accessToken!
            );

            if (oldContent != null)
            {
                oldLines = ExtractLines(oldContent, request.OldStartLine.Value, request.OldEndLine.Value);
            }
        }

        if (request.NewStartLine.HasValue && request.NewEndLine.HasValue && !string.IsNullOrEmpty(pr.SourceBranch))
        {
            var newContent = await gitHubGraphQLService.GetFileContentAsync(
                config.Organization,
                pr.Repository,
                pr.SourceBranch,
                request.Path,
                accessToken!
            );

            if (newContent != null)
            {
                newLines = ExtractLines(newContent, request.NewStartLine.Value, request.NewEndLine.Value);
            }
        }

        return new GetFileContentResult(oldLines, newLines);
    }

    private static List<LineContentDto> ExtractLines(string content, int startLine, int endLine)
    {
        var lines = content.Split('\n');
        var result = new List<LineContentDto>();

        for (int i = startLine; i <= endLine && i <= lines.Length; i++)
        {
            var lineIndex = i - 1;
            if (lineIndex >= 0 && lineIndex < lines.Length)
            {
                result.Add(new LineContentDto(i, lines[lineIndex].TrimEnd('\r')));
            }
        }

        return result;
    }
}
