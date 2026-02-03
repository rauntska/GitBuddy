using Graphite.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Graphite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ICacheService _cacheService;

    public SettingsController(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var config = await _cacheService.GetConfigAsync();

        if (config == null)
        {
            return Ok(new
            {
                organization = string.Empty,
                personalAccessToken = string.Empty,
                refreshIntervalMinutes = 5,
                lastRefresh = (DateTime?)null,
                appId = string.Empty,
                privateKey = string.Empty,
                installationId = string.Empty,
                useGitHubApp = false,
                deleteOldPRs = false
            });
        }

            return Ok(new
            {
                organization = config.Organization,
                personalAccessToken = config.PersonalAccessToken,
                refreshIntervalMinutes = config.RefreshIntervalMinutes,
                lastRefresh = config.LastRefresh,
                appId = config.AppId,
                privateKey = config.PrivateKey,
                installationId = config.InstallationId,
                useGitHubApp = config.UseGitHubApp,
                deleteOldPRs = config.DeleteOldPRs
            });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Organization))
        {
            return BadRequest("Organization is required");
        }

        if (request.UseGitHubApp)
        {
            if (string.IsNullOrWhiteSpace(request.AppId) || string.IsNullOrWhiteSpace(request.PrivateKey) || string.IsNullOrWhiteSpace(request.InstallationId))
            {
                return BadRequest("App ID, Private Key, and Installation ID are required when using GitHub App");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.PersonalAccessToken))
            {
                return BadRequest("Personal Access Token is required when not using GitHub App");
            }
        }

        await _cacheService.SaveConfigAsync(
            request.Organization,
            request.PersonalAccessToken,
            request.RefreshIntervalMinutes,
            request.AppId,
            request.PrivateKey,
            request.InstallationId,
            request.UseGitHubApp,
            request.DeleteOldPRs
        );

        return Ok(new { message = "Settings saved successfully" });
    }
}

public record SaveSettingsRequest(
    string Organization,
    string PersonalAccessToken,
    int RefreshIntervalMinutes,
    string AppId,
    string PrivateKey,
    string InstallationId,
    bool UseGitHubApp,
    bool DeleteOldPRs
);