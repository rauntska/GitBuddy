using Microsoft.AspNetCore.Mvc;
using GitBuddy.Api.Services;
using GitBuddy.Api.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace GitBuddy.Api.Controllers;

[ApiController]
[Route("api/users/me/settings")]
[Authorize]
public class UserSettingsController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserSettingsDto>> GetSettings()
    {
        var userIdClaim = User.FindFirst("UserId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized();
        }

        var settings = await userService.GetUserSettingsAsync(userId);
        return Ok(settings);
    }

    [HttpPut]
    public async Task<ActionResult<UserSettingsDto>> UpdateSettings([FromBody] UpdateUserSettingsRequest request)
    {
        var userIdClaim = User.FindFirst("UserId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized();
        }

        var settings = await userService.UpdateUserSettingsAsync(userId, request);
        return Ok(settings);
    }
}
