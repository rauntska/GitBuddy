using Graphite.Api.Services;

namespace Graphite.Api.BackgroundServices;

public class RepositoryRuleSyncWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RepositoryRuleSyncWorker> _logger;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(1);

    public RepositoryRuleSyncWorker(
        IServiceProvider serviceProvider,
        ILogger<RepositoryRuleSyncWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Repository Rule Sync Worker started. Will sync every {Interval} hours", SyncInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repositoryRuleService = scope.ServiceProvider.GetRequiredService<IRepositoryRuleService>();

                _logger.LogInformation("Starting scheduled repository rules sync...");
                await repositoryRuleService.SyncRepositoryRulesAsync();
                _logger.LogInformation("Repository rules sync completed. Next sync in {Interval} hours", SyncInterval.TotalHours);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during repository rules sync");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }
}
