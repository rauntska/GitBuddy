using MediatR;

namespace GitBuddy.Api.Features.Repositories.TriggerBranchesWithoutPRRefresh;

public record TriggerBranchesWithoutPRRefreshCommand : IRequest<Unit>;
