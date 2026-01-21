using Graphite.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Graphite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PullRequestsController : ControllerBase
{
    private readonly ICacheService _cacheService;

    public PullRequestsController(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var groupedPRs = await _cacheService.GetCachedPullRequestsAsync();
        return Ok(groupedPRs);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _cacheService.GetPullRequestStatsAsync();
        return Ok(stats);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var config = await _cacheService.GetConfigAsync();

        if (config == null || string.IsNullOrEmpty(config.Organization) || string.IsNullOrEmpty(config.PersonalAccessToken))
        {
            return BadRequest("GitHub configuration not found. Please configure settings first.");
        }

        try
        {
            await _cacheService.RefreshPullRequestsAsync(config.Organization, config.PersonalAccessToken);
            await _cacheService.UpdateLastRefreshAsync();
            return Ok(new { message = "Pull requests refreshed successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}