using GitBuddy.Api.DTOs;
using GitBuddy.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Features.Repositories.GetBranchesWithoutPRs;

public class GetBranchesWithoutPRsHandler(AppDbContext context)
    : IRequestHandler<GetBranchesWithoutPRsQuery, List<BranchWithoutPRDto>>
{
    public async Task<List<BranchWithoutPRDto>> Handle(GetBranchesWithoutPRsQuery request, CancellationToken cancellationToken)
    {
        var query = context.BranchesWithoutPR.AsQueryable();
        if (request.HiddenRepositories is { Count: > 0 })
        {
            var hidden = request.HiddenRepositories.ToArray();
            query = query.Where(b => !hidden.Contains(b.Repo.ToLower()));
        }

        return await query
            .OrderByDescending(b => b.LastActivityAt ?? DateTime.MinValue)
            .Select(b => new BranchWithoutPRDto(
                b.Owner, b.Repo, b.RepoFullName, b.BranchName, b.DefaultBranch, b.LastActivityAt))
            .ToListAsync(cancellationToken);
    }
}
