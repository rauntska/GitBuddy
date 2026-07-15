using GitBuddy.Api.Services;
using GitBuddy.Domain.Data;
using GitBuddy.Domain.Classifier;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.SetPriority;

public class SetPriorityHandler(
    AppDbContext context,
    INotificationService notificationService) : IRequestHandler<SetPriorityCommand, SetPriorityResult>
{
    public async Task<SetPriorityResult> Handle(SetPriorityCommand request, CancellationToken cancellationToken)
    {
        if (request.Priority is int level && (level < PrPriority.Low || level > PrPriority.Urgent))
            return new SetPriorityResult(false, "Priority must be between 0 (Low) and 3 (Urgent)", 0, false);

        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        if (pr == null)
            return new SetPriorityResult(false, "Pull request not found", 0, false);

        pr.Priority = request.Priority;
        await context.SaveChangesAsync(cancellationToken);

        var effective = PriorityService.GetEffectivePriorityStatic(pr);
        var overridden = pr.Priority is not null;

        await notificationService.BroadcastPRPriorityChangedAsync(pr.Id, effective, overridden);

        return new SetPriorityResult(true, "Priority updated", effective, overridden);
    }
}
