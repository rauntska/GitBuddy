using GitBuddy.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GitBuddy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController(ICacheService cacheService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Get()
    {
        var config = await cacheService.GetConfigAsync();

        if (config == null)
        {
            return Ok(new
            {
                organization = string.Empty,
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save([FromBody] SaveSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Organization))
        {
            return BadRequest(new { message = "Organization is required" });
        }

        if (request.UseGitHubApp)
        {
            if (string.IsNullOrWhiteSpace(request.AppId) || string.IsNullOrWhiteSpace(request.PrivateKey) || string.IsNullOrWhiteSpace(request.InstallationId))
            {
                return BadRequest(new { message = "App ID, Private Key, and Installation ID are required when using GitHub App" });
            }
        }

        await cacheService.SaveConfigAsync(
            request.Organization,
            null,
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
    int RefreshIntervalMinutes,
    string? AppId,
    string? PrivateKey,
    string? InstallationId,
    bool UseGitHubApp,
    bool DeleteOldPRs
);