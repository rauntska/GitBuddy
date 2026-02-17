using Graphite.Domain.Data;
using Graphite.Domain.Models;
using Graphite.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Services;

public interface IUserService
{
    Task<UserPreferencesDto> GetPreferencesAsync(int userId);
    Task<UserPreferencesDto> UpdatePreferencesAsync(int userId, UpdatePreferencesRequest request);
    Task<User> GetOrCreateDefaultUserAsync();
    Task<User> GetOrCreateGitHubUserAsync(GitHubUserDto githubUser, string accessToken);
    Task UpdateUserLastLoginAsync(int userId);
    Task<User?> GetCurrentUserAsync(System.Security.Claims.ClaimsPrincipal user);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<bool> UpdateUserRoleAsync(int userId, UserRole role);
    Task<bool> DeleteUserAsync(int userId);
    Task<bool> HasAnyUsersAsync();
    Task<int> GetUserCountAsync();
}

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserPreferencesDto> GetPreferencesAsync(int userId)
    {
        var preferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // Create default preferences
            preferences = new UserPreferences
            {
                UserId = userId,
                DiffViewMode = "unified",
                FileTreeWidth = 256,
                CommentsPanelWidth = 320,
                FileTreeVisible = true
            };
            _context.UserPreferences.Add(preferences);
            await _context.SaveChangesAsync();
        }

        return new UserPreferencesDto(
            preferences.DiffViewMode,
            preferences.FileTreeWidth,
            preferences.CommentsPanelWidth,
            preferences.FileTreeVisible
        );
    }

    public async Task<UserPreferencesDto> UpdatePreferencesAsync(int userId, UpdatePreferencesRequest request)
    {
        var preferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            preferences = new UserPreferences { UserId = userId };
            _context.UserPreferences.Add(preferences);
        }

        if (request.DiffViewMode != null)
            preferences.DiffViewMode = request.DiffViewMode;
        
        if (request.FileTreeWidth.HasValue)
            preferences.FileTreeWidth = request.FileTreeWidth.Value;
        
        if (request.CommentsPanelWidth.HasValue)
            preferences.CommentsPanelWidth = request.CommentsPanelWidth.Value;
        
        if (request.FileTreeVisible.HasValue)
            preferences.FileTreeVisible = request.FileTreeVisible.Value;

        preferences.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new UserPreferencesDto(
            preferences.DiffViewMode,
            preferences.FileTreeWidth,
            preferences.CommentsPanelWidth,
            preferences.FileTreeVisible
        );
    }

    public async Task<User> GetOrCreateDefaultUserAsync()
    {
        var user = await _context.Users
            .Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.Username == "default");

        if (user == null)
        {
            user = new User
            {
                Username = "default",
                Email = "default@localhost",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);

            var preferences = new UserPreferences
            {
                User = user,
                DiffViewMode = "unified",
                FileTreeWidth = 256,
                CommentsPanelWidth = 320,
                FileTreeVisible = true
            };
            _context.UserPreferences.Add(preferences);

            await _context.SaveChangesAsync();
        }

        return user;
    }

    public async Task<User> GetOrCreateGitHubUserAsync(GitHubUserDto githubUser, string accessToken)
    {
        var user = await _context.Users
            .Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.Provider == "GitHub" && u.ProviderUserId == githubUser.Id.ToString());

        if (user == null)
        {
            user = new User
            {
                Username = githubUser.Login,
                Email = githubUser.Email ?? $"{githubUser.Login}@github.local",
                Provider = "GitHub",
                ProviderUserId = githubUser.Id.ToString(),
                AvatarUrl = githubUser.AvatarUrl,
                AccessToken = accessToken,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            _context.Users.Add(user);

            var preferences = new UserPreferences
            {
                User = user,
                DiffViewMode = "unified",
                FileTreeWidth = 256,
                CommentsPanelWidth = 320,
                FileTreeVisible = true
            };
            _context.UserPreferences.Add(preferences);

            await _context.SaveChangesAsync();
        }
        else
        {
            user.Username = githubUser.Login;
            user.Email = githubUser.Email ?? $"{githubUser.Login}@github.local";
            user.AvatarUrl = githubUser.AvatarUrl;
            user.AccessToken = accessToken;
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return user;
    }

    public async Task UpdateUserLastLoginAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<User?> GetCurrentUserAsync(System.Security.Claims.ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return await _context.Users.FindAsync(userId);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .Include(u => u.Preferences)
            .OrderBy(u => u.Username)
            .ToListAsync();
    }

    public async Task<bool> UpdateUserRoleAsync(int userId, UserRole role)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.Role = role;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasAnyUsersAsync()
    {
        return await _context.Users.AnyAsync();
    }

    public async Task<int> GetUserCountAsync()
    {
        return await _context.Users.CountAsync();
    }
}
