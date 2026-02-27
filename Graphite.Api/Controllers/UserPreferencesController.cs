using Microsoft.AspNetCore.Mvc;
using Graphite.Api.Services;
using Graphite.Api.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace Graphite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserPreferencesController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserPreferencesDto>> GetPreferences()
    {
        var userIdClaim = User.FindFirst("UserId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized();
        }

        var preferences = await userService.GetPreferencesAsync(userId);
        return Ok(preferences);
    }

    [HttpPatch]
    public async Task<ActionResult<UserPreferencesDto>> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        var userIdClaim = User.FindFirst("UserId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized();
        }

        var preferences = await userService.UpdatePreferencesAsync(userId, request);
        return Ok(preferences);
    }
}
