using Graphite.Api.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Graphite.Api.BackgroundServices;

public class PRRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PRRefreshService> _logger;
    private const int InitialDelaySeconds = 10;
    private const int RetryDelayMinutes = 5;

    public PRRefreshService(IServiceProvider serviceProvider, ILogger<PRRefreshService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PR Refresh Service is starting");

        await Task.Delay(TimeSpan.FromSeconds(InitialDelaySeconds), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessRefreshCycleAsync(stoppingToken);
        }

        _logger.LogInformation("PR Refresh Service is stopping");
    }

    private async Task ProcessRefreshCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
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
            _logger.LogError(ex, "Error refreshing pull requests");
            await Task.Delay(TimeSpan.FromMinutes(RetryDelayMinutes), stoppingToken);
        }
    }

    private async Task RefreshPullRequestsAsync(ICacheService cacheService, Domain.Models.GitHubConfig config, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Refreshing pull requests for {Organization}", config.Organization);
        await cacheService.RefreshPullRequestsAsync(config);
        await cacheService.UpdateLastRefreshAsync();
        _logger.LogInformation("Pull requests refreshed successfully");

        await Task.Delay(TimeSpan.FromMinutes(config.RefreshIntervalMinutes), stoppingToken);
    }
}