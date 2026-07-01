using GitBuddy.Domain.Data;
using GitBuddy.Domain.Models;
using GitBuddy.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Services;

public interface IUserService
{
    Task<UserPreferencesDto> GetPreferencesAsync(int userId);
    Task<UserPreferencesDto> UpdatePreferencesAsync(int userId, UpdatePreferencesRequest request);
    Task<User> GetOrCreateDefaultUserAsync();
    Task<User> GetOrCreateGitHubUserAsync(GitHubUserDto githubUser, string accessToken, UserRole? role = null);
    Task UpdateUserLastLoginAsync(int userId);
    Task<User?> GetCurrentUserAsync(System.Security.Claims.ClaimsPrincipal user);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<bool> UpdateUserRoleAsync(int userId, UserRole role);
    Task<bool> DeleteUserAsync(int userId);
    Task<bool> HasAnyUsersAsync();
    Task<int> GetUserCountAsync();
    Task<UserSettingsDto> GetUserSettingsAsync(int userId);
    Task<UserSettingsDto> UpdateUserSettingsAsync(int userId, UpdateUserSettingsRequest request);
    Task<string?> GetUserPersonalAccessTokenAsync(int userId);
}

public class UserService(AppDbContext context) : IUserService
{
    public async Task<UserPreferencesDto> GetPreferencesAsync(int userId)
    {
        var preferences = await context.UserPreferences
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
                FileTreeVisible = true,
                ListViewMode = "comfortable",
                ShowColumnHeaders = true
            };
            context.UserPreferences.Add(preferences);
            await context.SaveChangesAsync();
        }

        return new UserPreferencesDto(
            preferences.DiffViewMode,
            preferences.FileTreeWidth,
            preferences.CommentsPanelWidth,
            preferences.FileTreeVisible,
            preferences.ListViewMode ?? "comfortable",
            preferences.ShowColumnHeaders,
            preferences.PinnedPrIds,
            preferences.DashboardGroupOrder,
            preferences.HiddenDashboardGroups,
            preferences.NotificationPreferences
        );
    }

    public async Task<UserPreferencesDto> UpdatePreferencesAsync(int userId, UpdatePreferencesRequest request)
    {
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            preferences = new UserPreferences { UserId = userId };
            context.UserPreferences.Add(preferences);
        }

        if (request.DiffViewMode != null)
            preferences.DiffViewMode = request.DiffViewMode;

        if (request.FileTreeWidth.HasValue)
            preferences.FileTreeWidth = request.FileTreeWidth.Value;

        if (request.CommentsPanelWidth.HasValue)
            preferences.CommentsPanelWidth = request.CommentsPanelWidth.Value;

        if (request.FileTreeVisible.HasValue)
            preferences.FileTreeVisible = request.FileTreeVisible.Value;

        if (request.ShowColumnHeaders.HasValue)
            preferences.ShowColumnHeaders = request.ShowColumnHeaders.Value;

        if (request.ListViewMode != null)
            preferences.ListViewMode = request.ListViewMode;

        if (request.PinnedPrIds != null)
            preferences.PinnedPrIds = request.PinnedPrIds;

        if (request.DashboardGroupOrder != null)
            preferences.DashboardGroupOrder = request.DashboardGroupOrder;

        if (request.HiddenDashboardGroups != null)
            preferences.HiddenDashboardGroups = request.HiddenDashboardGroups;

        if (request.NotificationPreferences != null)
            preferences.NotificationPreferences = request.NotificationPreferences;

        preferences.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return new UserPreferencesDto(
            preferences.DiffViewMode,
            preferences.FileTreeWidth,
            preferences.CommentsPanelWidth,
            preferences.FileTreeVisible,
            preferences.ListViewMode ?? "comfortable",
            preferences.ShowColumnHeaders,
            preferences.PinnedPrIds,
            preferences.DashboardGroupOrder,
            preferences.HiddenDashboardGroups,
            preferences.NotificationPreferences
        );
    }

    public async Task<User> GetOrCreateDefaultUserAsync()
    {
        var user = await context.Users
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
            context.Users.Add(user);

            var preferences = new UserPreferences
            {
                User = user,
                DiffViewMode = "unified",
                FileTreeWidth = 256,
                CommentsPanelWidth = 320,
                FileTreeVisible = true,
                ListViewMode = "normal"
            };
            context.UserPreferences.Add(preferences);

            await context.SaveChangesAsync();
        }

        return user;
    }

    public async Task<User> GetOrCreateGitHubUserAsync(GitHubUserDto githubUser, string accessToken, UserRole? role = null)
    {
        var user = await context.Users
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
                LastLoginAt = DateTime.UtcNow,
                Role = role ?? UserRole.Developer
            };
            context.Users.Add(user);

            var preferences = new UserPreferences
            {
                User = user,
                DiffViewMode = "unified",
                FileTreeWidth = 256,
                CommentsPanelWidth = 320,
                FileTreeVisible = true,
                ListViewMode = "normal"
            };
            context.UserPreferences.Add(preferences);

            await context.SaveChangesAsync();
        }
        else
        {
            user.Username = githubUser.Login;
            user.Email = githubUser.Email ?? $"{githubUser.Login}@github.local";
            user.AvatarUrl = githubUser.AvatarUrl;
            user.AccessToken = accessToken;
            user.LastLoginAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        return user;
    }

    public async Task UpdateUserLastLoginAsync(int userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<User?> GetCurrentUserAsync(System.Security.Claims.ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return await context.Users.FindAsync(userId);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await context.Users
            .Include(u => u.Preferences)
            .OrderBy(u => u.Username)
            .ToListAsync();
    }

    public async Task<bool> UpdateUserRoleAsync(int userId, UserRole role)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.Role = role;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            return false;

        context.Users.Remove(user);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasAnyUsersAsync()
    {
        return await context.Users.AnyAsync();
    }

    public async Task<int> GetUserCountAsync()
    {
        return await context.Users.CountAsync();
    }

    public async Task<UserSettingsDto> GetUserSettingsAsync(int userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        return new UserSettingsDto(
            PersonalAccessToken: null,
            HasPersonalAccessToken: !string.IsNullOrEmpty(user.PersonalAccessToken)
        );
    }

    public async Task<UserSettingsDto> UpdateUserSettingsAsync(int userId, UpdateUserSettingsRequest request)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        if (request.PersonalAccessToken != null)
        {
            user.PersonalAccessToken = string.IsNullOrWhiteSpace(request.PersonalAccessToken) 
                ? null 
                : request.PersonalAccessToken;
            await context.SaveChangesAsync();
        }

        return new UserSettingsDto(
            PersonalAccessToken: null,
            HasPersonalAccessToken: !string.IsNullOrEmpty(user.PersonalAccessToken)
        );
    }

    public async Task<string?> GetUserPersonalAccessTokenAsync(int userId)
    {
        var user = await context.Users.FindAsync(userId);
        return user?.PersonalAccessToken;
    }
}
