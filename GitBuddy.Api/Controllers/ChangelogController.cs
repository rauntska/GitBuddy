using Microsoft.AspNetCore.Mvc;
using GitBuddy.Api.Services;
using GitBuddy.Api.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace GitBuddy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChangelogController(IChangelogService changelogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ChangelogResponseDto>> GetChangelog()
    {
        var userIdClaim = User.FindFirst("UserId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized();
        }

        var response = await changelogService.GetChangelogAsync(userId);
        return Ok(response);
    }

    [HttpPost("mark-seen")]
    public async Task<ActionResult<MarkSeenResponseDto>> MarkSeen()
    {
        var userIdClaim = User.FindFirst("UserId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized();
        }

        var lastSeenChangelogAt = await changelogService.MarkSeenAsync(userId);
        return Ok(new MarkSeenResponseDto(lastSeenChangelogAt));
    }
}
