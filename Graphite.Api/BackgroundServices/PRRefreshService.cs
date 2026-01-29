using Graphite.Api.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Graphite.Api.BackgroundServices;

public class PRRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PRRefreshService> _logger;

    public PRRefreshService(IServiceProvider serviceProvider, ILogger<PRRefreshService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PR Refresh Service is starting");

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
                var config = await cacheService.GetConfigAsync();

                if (config != null && !string.IsNullOrEmpty(config.Organization))
                {
                    bool isValid = false;
                    if (config.UseGitHubApp)
                    {
                        isValid = !string.IsNullOrEmpty(config.AppId) && !string.IsNullOrEmpty(config.PrivateKey) && !string.IsNullOrEmpty(config.InstallationId);
                    }
                    else
                    {
                        isValid = !string.IsNullOrEmpty(config.PersonalAccessToken);
                    }

                    if (isValid)
                    {
                        _logger.LogInformation("Refreshing pull requests for {Organization}", config.Organization);
                        await cacheService.RefreshPullRequestsAsync(config);
                        await cacheService.UpdateLastRefreshAsync();
                        _logger.LogInformation("Pull requests refreshed successfully");

                        await Task.Delay(TimeSpan.FromMinutes(config.RefreshIntervalMinutes), stoppingToken);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing pull requests");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("PR Refresh Service is stopping");
    }
}