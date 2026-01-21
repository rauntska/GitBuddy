using Graphite.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Graphite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
                lastRefresh = (DateTime?)null
            });
        }

        return Ok(new
        {
            organization = config.Organization,
            personalAccessToken = config.PersonalAccessToken,
            refreshIntervalMinutes = config.RefreshIntervalMinutes,
            lastRefresh = config.LastRefresh
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Organization))
        {
            return BadRequest("Organization is required");
        }

        if (string.IsNullOrWhiteSpace(request.PersonalAccessToken))
        {
            return BadRequest("Personal Access Token is required");
        }

        await _cacheService.SaveConfigAsync(
            request.Organization,
            request.PersonalAccessToken,
            request.RefreshIntervalMinutes
        );

        return Ok(new { message = "Settings saved successfully" });
    }
}

public record SaveSettingsRequest(
    string Organization,
    string PersonalAccessToken,
    int RefreshIntervalMinutes
);