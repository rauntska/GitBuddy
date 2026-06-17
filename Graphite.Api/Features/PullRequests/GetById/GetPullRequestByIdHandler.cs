using Graphite.Api.DTOs;
using Graphite.Api.Extensions;
using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Features.PullRequests.GetById;

public class GetPullRequestByIdHandler(
    AppDbContext context,
    IPullRequestValidationService validationService)
    : IRequestHandler<GetPullRequestByIdQuery, PRDetailDto?>
{
    public async Task<PRDetailDto?> Handle(GetPullRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var pr = await context.PullRequests
            .Include(p => p.Reviews)
            .Include(p => p.ReviewThreads)
            .Include(p => p.CheckRuns)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (pr == null)
            return null;

        var files = await context.FileDiffs
            .Where(f => f.PullRequestId == request.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var comments = await context.Comments
            .Where(c => c.PullRequestId == request.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var user = await validationService.GetUserAsync(request.User);
        List<Domain.Models.UserFileViewedState>? viewedStates = null;

        if (user != null)
        {
            viewedStates = await context.UserFileViewedStates
                .Where(uvs => uvs.UserId == user.Id && files.Select(f => f.Id).Contains(uvs.FileDiffId))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        return pr.ToDetailDto(files, comments, viewedStates, null,
            pr.RequiredApprovingReviews, pr.CurrentApprovingReviews,
            pr.HasUnresolvedThreads, pr.IsMergeReady, pr.MergeBlockReason);
    }
}
