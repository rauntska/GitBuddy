using GitBuddy.Api.DTOs;
using MediatR;

namespace GitBuddy.Api.Features.Repositories.GetBranchesWithoutPRs;

public record GetBranchesWithoutPRsQuery : IRequest<List<BranchWithoutPRDto>>;
