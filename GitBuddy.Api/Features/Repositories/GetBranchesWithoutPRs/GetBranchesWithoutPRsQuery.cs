using GitBuddy.Api.DTOs;
using MediatR;

namespace GitBuddy.Api.Features.Repositories.GetBranchesWithoutPRs;

/// <param name="HiddenRepositories">Lower-cased bare repo names to leave out (per-user preference).</param>
public record GetBranchesWithoutPRsQuery(IReadOnlyCollection<string>? HiddenRepositories = null)
    : IRequest<List<BranchWithoutPRDto>>;
