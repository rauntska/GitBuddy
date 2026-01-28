using Microsoft.AspNetCore.Mvc;
using Graphite.Api.Services;
using Graphite.Api.DTOs;

namespace Graphite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserPreferencesController : ControllerBase
{
    private readonly IUserService _userService;

    public UserPreferencesController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<UserPreferencesDto>> GetPreferences()
    {
        // For now, use default user (userId = 1)
        // In a real app, get userId from auth context
        var user = await _userService.GetOrCreateDefaultUserAsync();
        var preferences = await _userService.GetPreferencesAsync(user.Id);
        return Ok(preferences);
    }

    [HttpPatch]
    public async Task<ActionResult<UserPreferencesDto>> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        // For now, use default user (userId = 1)
        // In a real app, get userId from auth context
        var user = await _userService.GetOrCreateDefaultUserAsync();
        var preferences = await _userService.UpdatePreferencesAsync(user.Id, request);
        return Ok(preferences);
    }
}
