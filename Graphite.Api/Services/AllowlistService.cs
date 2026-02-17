using Graphite.Domain.Data;
using Graphite.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Services;

public interface IAllowlistService
{
    Task<AllowedUser> AddToAllowlistAsync(string? email, string? gitHubUsername, UserRole assignedRole, int createdById);
    Task<IEnumerable<AllowedUser>> GetAllAsync();
    Task<bool> RemoveFromAllowlistAsync(int id);
    Task<AllowedUser?> FindMatchAsync(string email, string gitHubUsername);
}

public class AllowlistService : IAllowlistService
{
    private readonly AppDbContext _context;

    public AllowlistService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AllowedUser> AddToAllowlistAsync(string? email, string? gitHubUsername, UserRole assignedRole, int createdById)
    {
        var allowedUser = new AllowedUser
        {
            Email = email?.ToLowerInvariant(),
            GitHubUsername = gitHubUsername?.ToLowerInvariant(),
            AssignedRole = assignedRole,
            CreatedByUserId = createdById
        };

        _context.AllowedUsers.Add(allowedUser);
        await _context.SaveChangesAsync();
        return allowedUser;
    }

    public async Task<IEnumerable<AllowedUser>> GetAllAsync()
    {
        return await _context.AllowedUsers
            .Include(a => a.CreatedByUser)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> RemoveFromAllowlistAsync(int id)
    {
        var allowedUser = await _context.AllowedUsers.FindAsync(id);
        if (allowedUser == null)
            return false;

        _context.AllowedUsers.Remove(allowedUser);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AllowedUser?> FindMatchAsync(string email, string gitHubUsername)
    {
        var normalizedEmail = email?.ToLowerInvariant();
        var normalizedUsername = gitHubUsername?.ToLowerInvariant();

        return await _context.AllowedUsers
            .FirstOrDefaultAsync(a =>
                (a.Email != null && a.Email == normalizedEmail) ||
                (a.GitHubUsername != null && a.GitHubUsername == normalizedUsername));
    }
}
