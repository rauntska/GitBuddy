using Graphite.Api.Extensions;
using Graphite.Api.Services;
using Graphite.Domain.Data;
using Graphite.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Features.PullRequests.RefreshFileViewedStates;

public class RefreshFileViewedStatesHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService,
    ICacheService cacheService) 
    : IRequestHandler<RefreshFileViewedStatesCommand, RefreshFileViewedStatesResult>
{
    public async Task<RefreshFileViewedStatesResult> Handle(RefreshFileViewedStatesCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests
            .FirstOrDefaultAsync(p => p.Id == request.PullRequestId, cancellationToken);

        if (pr == null)
            throw new KeyNotFoundException("Pull request not found");

        var config = await cacheService.GetConfigAsync();
        if (config == null)
            throw new InvalidOperationException("GitHub configuration not found");

        var fileDiffs = await gitHubService.GetFileDiffsAsync(
            config.Organization,
            pr.Repository,
            pr.GitHubId,
            config,
            accessToken!
        );

        var dbFileDiffs = await context.FileDiffs
            .Where(f => f.PullRequestId == request.PullRequestId)
            .ToListAsync(cancellationToken);

        foreach (var fileDiff in fileDiffs)
        {
            var dbFileDiff = dbFileDiffs.FirstOrDefault(f => f.Path == fileDiff.Path);
            if (dbFileDiff != null)
            {
                var existingState = await context.UserFileViewedStates
                    .FirstOrDefaultAsync(uvs => uvs.UserId == user.Id && uvs.FileDiffId == dbFileDiff.Id, cancellationToken);

                if (existingState != null)
                {
                    existingState.ViewedState = fileDiff.ViewerViewedState;
                    existingState.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    context.UserFileViewedStates.Add(new UserFileViewedState
                    {
                        UserId = user.Id,
                        FileDiffId = dbFileDiff.Id,
                        ViewedState = fileDiff.ViewerViewedState,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var viewedStates = await context.UserFileViewedStates
            .Where(uvs => uvs.UserId == user.Id && dbFileDiffs.Select(f => f.Id).Contains(uvs.FileDiffId))
            .ToListAsync(cancellationToken);

        var result = dbFileDiffs.Select(file =>
        {
            var viewedState = viewedStates.FirstOrDefault(vs => vs.FileDiffId == file.Id);
            var dto = file.ToDto(viewedState?.ViewedState, viewedState?.UpdatedAt);
            return new FileViewedStateDto(
                dto.Id,
                dto.Path,
                dto.OldPath,
                dto.Status,
                dto.Additions,
                dto.Deletions,
                dto.Changes,
                dto.Patch,
                dto.Language,
                dto.ViewedState,
                dto.ViewedAt
            );
        }).ToList();

        return new RefreshFileViewedStatesResult(result);
    }
}
