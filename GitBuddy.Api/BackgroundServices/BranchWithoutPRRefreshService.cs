using GitBuddy.Api.Services;
using GitBuddy.Domain.Models;

namespace GitBuddy.Api.BackgroundServices;

public class BranchWithoutPRRefreshService(
    IServiceProvider serviceProvider,
    IBranchWithoutPRRefreshTrigger trigger,
    ILogger<BranchWithoutPRRefreshService> logger)
    : BackgroundService
{
    private const int InitialDelaySeconds = 10;
    private const int RetryDelayMinutes = 5;
    private const int DefaultIntervalMinutes = 30;
    private const int MinIntervalMinutes = 5;
    private const int MaxIntervalMinutes = 1440;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Branches-Without-PR Refresh Service is starting");

        await Task.Delay(TimeSpan.FromSeconds(InitialDelaySeconds), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextDelay = await ProcessRefreshCycleAsync(stoppingToken);
            await WaitForNextCycleAsync(nextDelay, stoppingToken);
        }

        logger.LogInformation("Branches-Without-PR Refresh Service is stopping");
    }

    private async Task<TimeSpan> ProcessRefreshCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var validationService = scope.ServiceProvider.GetRequiredService<IGitHubConfigValidationService>();
            var branchService = scope.ServiceProvider.GetRequiredService<IBranchWithoutPRService>();

            var config = await cacheService.GetConfigAsync();

            if (!validationService.IsValidConfiguration(config))
            {
                logger.LogDebug("GitHub configuration not valid; skipping branches-without-PR refresh");
                return TimeSpan.FromMinutes(RetryDelayMinutes);
            }

            await branchService.RefreshAndPersistAsync(config!, stoppingToken);
            logger.LogInformation("Branches-without-PR refresh completed");

            return TimeSpan.FromMinutes(GetInterval(config!));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing branches-without-PR");
            return TimeSpan.FromMinutes(RetryDelayMinutes);
        }
    }

    private async Task WaitForNextCycleAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        // Drain any pending manual triggers from a cycle that was already in flight.
        while (trigger.Reader.TryRead(out _)) { }

        using var delayCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var delayTask = Task.Delay(delay, delayCts.Token);
        var waitTask = trigger.Reader.WaitToReadAsync(stoppingToken).AsTask();

        await Task.WhenAny(delayTask, waitTask);

        // Cancel the delay if the trigger won (no-op otherwise). The wait task
        // resolves on its own when stoppingToken fires during shutdown.
        delayCts.Cancel();

        // Always drain — whether we exited due to delay or trigger.
        while (trigger.Reader.TryRead(out _)) { }
    }

    private static int GetInterval(GitHubConfig config)
    {
        var minutes = config.BranchRefreshIntervalMinutes;
        if (minutes <= 0)
        {
            return DefaultIntervalMinutes;
        }
        return Math.Clamp(minutes, MinIntervalMinutes, MaxIntervalMinutes);
    }
}
