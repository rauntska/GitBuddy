using GitBuddy.Domain.Data;
using GitBuddy.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Services;

public interface IAllowlistService
{
    Task<AllowedUser> AddToAllowlistAsync(string? email, string? gitHubUsername, UserRole assignedRole, int createdById);
    Task<IEnumerable<AllowedUser>> GetAllAsync();
    Task<bool> RemoveFromAllowlistAsync(int id);
    Task<AllowedUser?> FindMatchAsync(string email, string gitHubUsername);
}

public class AllowlistService(AppDbContext context) : IAllowlistService
{
    public async Task<AllowedUser> AddToAllowlistAsync(string? email, string? gitHubUsername, UserRole assignedRole, int createdById)
    {
        var allowedUser = new AllowedUser
        {
            Email = email?.ToLowerInvariant(),
            GitHubUsername = gitHubUsername?.ToLowerInvariant(),
            AssignedRole = assignedRole,
            CreatedByUserId = createdById
        };

        context.AllowedUsers.Add(allowedUser);
        await context.SaveChangesAsync();
        return allowedUser;
    }

    public async Task<IEnumerable<AllowedUser>> GetAllAsync()
    {
        return await context.AllowedUsers
            .Include(a => a.CreatedByUser)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> RemoveFromAllowlistAsync(int id)
    {
        var allowedUser = await context.AllowedUsers.FindAsync(id);
        if (allowedUser == null)
            return false;

        context.AllowedUsers.Remove(allowedUser);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<AllowedUser?> FindMatchAsync(string email, string gitHubUsername)
    {
        var normalizedEmail = email?.ToLowerInvariant();
        var normalizedUsername = gitHubUsername?.ToLowerInvariant();

        return await context.AllowedUsers
            .FirstOrDefaultAsync(a =>
                (a.Email != null && a.Email == normalizedEmail) ||
                (a.GitHubUsername != null && a.GitHubUsername == normalizedUsername));
    }
}
