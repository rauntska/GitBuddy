using GitBuddy.Api.Services;
using MediatR;

namespace GitBuddy.Api.Features.Repositories.TriggerBranchesWithoutPRRefresh;

public class TriggerBranchesWithoutPRRefreshHandler(IBranchWithoutPRRefreshTrigger trigger)
    : IRequestHandler<TriggerBranchesWithoutPRRefreshCommand, Unit>
{
    public Task<Unit> Handle(TriggerBranchesWithoutPRRefreshCommand request, CancellationToken cancellationToken)
    {
        trigger.Trigger();
        return Task.FromResult(Unit.Value);
    }
}
