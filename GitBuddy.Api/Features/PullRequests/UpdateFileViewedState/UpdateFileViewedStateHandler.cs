using GitBuddy.Api.Services;
using GitBuddy.Domain.Data;
using GitBuddy.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Features.PullRequests.UpdateFileViewedState;

public class UpdateFileViewedStateHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<UpdateFileViewedStateCommand, Unit>
{
    public async Task<Unit> Handle(UpdateFileViewedStateCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            throw new KeyNotFoundException("Pull request not found");

        var config = await validationService.GetRequiredConfigAsync();

        if (request.Viewed)
        {
            await gitHubService.MarkFileAsViewedAsync(
                config.Organization, pr.Repository, (int)pr.GitHubId, request.Path, config, accessToken!);
        }
        else
        {
            await gitHubService.UnmarkFileAsViewedAsync(
                config.Organization, pr.Repository, (int)pr.GitHubId, request.Path, config, accessToken!);
        }

        var fileDiff = await context.FileDiffs
            .FirstOrDefaultAsync(f => f.PullRequestId == request.PullRequestId && f.Path == request.Path, cancellationToken);
        
        if (fileDiff != null)
        {
            var viewedState = await context.UserFileViewedStates
                .FirstOrDefaultAsync(vs => vs.FileDiffId == fileDiff.Id && vs.UserId == user.Id, cancellationToken);

            if (viewedState == null)
            {
                context.UserFileViewedStates.Add(new UserFileViewedState
                {
                    FileDiffId = fileDiff.Id,
                    UserId = user.Id,
                    ViewedState = request.Viewed ? "VIEWED" : "UNVIEWED",
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                viewedState.ViewedState = request.Viewed ? "VIEWED" : "UNVIEWED";
                viewedState.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
