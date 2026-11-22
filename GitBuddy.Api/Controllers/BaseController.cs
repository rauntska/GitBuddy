using GitBuddy.Api.Services;
using GitBuddy.Domain.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Controllers;

public class BaseController(AppDbContext context) : ControllerBase
{
    protected async Task<Domain.Models.User?> CurrentUser()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        return await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    /// <summary>Lower-cased bare names of the repositories the current user has hidden from the dashboard.</summary>
    protected async Task<string[]> GetHiddenRepositoriesAsync()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return [];

        var json = await context.UserPreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.HiddenRepositories)
            .FirstOrDefaultAsync();

        return HiddenRepositoryFilter.Parse(json);
    }
}