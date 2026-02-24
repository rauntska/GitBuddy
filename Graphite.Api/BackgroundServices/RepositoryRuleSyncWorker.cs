using Graphite.Api.Services;

namespace Graphite.Api.BackgroundServices;

public class RepositoryRuleSyncWorker(
    IServiceProvider serviceProvider,
    ILogger<RepositoryRuleSyncWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Repository Rule Sync Worker started. Will sync every {Interval} hours", SyncInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var repositoryRuleService = scope.ServiceProvider.GetRequiredService<IRepositoryRuleService>();

                logger.LogInformation("Starting scheduled repository rules sync...");
                await repositoryRuleService.SyncRepositoryRulesAsync();
                logger.LogInformation("Repository rules sync completed. Next sync in {Interval} hours", SyncInterval.TotalHours);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during repository rules sync");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }
}
