using GitBuddy.Api.Services;

namespace GitBuddy.Api.BackgroundServices;

public class PRRefreshService(IServiceProvider serviceProvider, ILogger<PRRefreshService> logger)
    : BackgroundService
{
    private const int InitialDelaySeconds = 10;
    private const int RetryDelayMinutes = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PR Refresh Service is starting");

        await Task.Delay(TimeSpan.FromSeconds(InitialDelaySeconds), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessRefreshCycleAsync(stoppingToken);
        }

        logger.LogInformation("PR Refresh Service is stopping");
    }

    private async Task ProcessRefreshCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var configValidationService = scope.ServiceProvider.GetRequiredService<IGitHubConfigValidationService>();

            var config = await cacheService.GetConfigAsync();

            if (configValidationService.IsValidConfiguration(config))
            {
                await RefreshPullRequestsAsync(cacheService, config!, stoppingToken);
            }
            else
            {
                await Task.Delay(TimeSpan.FromMinutes(RetryDelayMinutes), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing pull requests");
            await Task.Delay(TimeSpan.FromMinutes(RetryDelayMinutes), stoppingToken);
        }
    }

    private async Task RefreshPullRequestsAsync(ICacheService cacheService, Domain.Models.GitHubConfig config, CancellationToken stoppingToken)
    {
        logger.LogInformation("Refreshing pull requests for {Organization}", config.Organization);
        await cacheService.RefreshPullRequestsAsync(config);
        await cacheService.UpdateLastRefreshAsync();
        logger.LogInformation("Pull requests refreshed successfully");

        await Task.Delay(TimeSpan.FromMinutes(config.RefreshIntervalMinutes), stoppingToken);
    }
}