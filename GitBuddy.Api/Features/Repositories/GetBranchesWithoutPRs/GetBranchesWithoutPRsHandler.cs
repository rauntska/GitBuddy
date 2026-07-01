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
        return await context.BranchesWithoutPR
            .OrderByDescending(b => b.LastActivityAt ?? DateTime.MinValue)
            .Select(b => new BranchWithoutPRDto(
                b.Owner, b.Repo, b.RepoFullName, b.BranchName, b.DefaultBranch, b.LastActivityAt))
            .ToListAsync(cancellationToken);
    }
}
