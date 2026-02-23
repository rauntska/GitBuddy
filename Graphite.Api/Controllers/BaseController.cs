using Graphite.Domain.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Controllers;

public class BaseController(AppDbContext context) : ControllerBase
{
    protected async Task<Domain.Models.User?> CurrentUser()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        return await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }
}