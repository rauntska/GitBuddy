using GitBuddy.Domain.Data;
using GitBuddy.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Services;

public interface IInvitationService
{
    Task<Invitation> CreateInvitationAsync(string email, string? gitHubUsername, UserRole assignedRole, int createdById, DateTime? expiresAt = null);
    Task<IEnumerable<Invitation>> GetAllInvitationsAsync();
    Task<IEnumerable<Invitation>> GetPendingInvitationsAsync();
    Task<Invitation?> GetByTokenAsync(string token);
    Task<Invitation?> ValidateInvitationAsync(string token);
    Task AcceptInvitationAsync(int invitationId, int userId);
    Task<bool> RevokeInvitationAsync(int id);
    Task<bool> DeleteInvitationAsync(int id);
}

public class InvitationService : IInvitationService
{
    private readonly AppDbContext _context;

    public InvitationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Invitation> CreateInvitationAsync(string email, string? gitHubUsername, UserRole assignedRole, int createdById, DateTime? expiresAt = null)
    {
        var invitation = new Invitation
        {
            Email = email.ToLowerInvariant(),
            GitHubUsername = gitHubUsername?.ToLowerInvariant(),
            Token = Guid.NewGuid().ToString("N"),
            AssignedRole = assignedRole,
            CreatedByUserId = createdById,
            ExpiresAt = expiresAt
        };

        _context.Invitations.Add(invitation);
        await _context.SaveChangesAsync();
        return invitation;
    }

    public async Task<IEnumerable<Invitation>> GetAllInvitationsAsync()
    {
        return await _context.Invitations
            .Include(i => i.CreatedByUser)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invitation>> GetPendingInvitationsAsync()
    {
        return await _context.Invitations
            .Include(i => i.CreatedByUser)
            .Where(i => i.AcceptedAt == null && (i.ExpiresAt == null || i.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<Invitation?> GetByTokenAsync(string token)
    {
        return await _context.Invitations
            .Include(i => i.CreatedByUser)
            .FirstOrDefaultAsync(i => i.Token == token);
    }

    public async Task<Invitation?> ValidateInvitationAsync(string token)
    {
        var invitation = await _context.Invitations
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invitation == null)
            return null;

        if (invitation.AcceptedAt != null)
            return null;

        if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value < DateTime.UtcNow)
            return null;

        return invitation;
    }

    public async Task AcceptInvitationAsync(int invitationId, int userId)
    {
        var invitation = await _context.Invitations.FindAsync(invitationId);
        if (invitation != null)
        {
            invitation.AcceptedAt = DateTime.UtcNow;
            invitation.AcceptedByUserId = userId;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> RevokeInvitationAsync(int id)
    {
        var invitation = await _context.Invitations.FindAsync(id);
        if (invitation == null || invitation.AcceptedAt != null)
            return false;

        _context.Invitations.Remove(invitation);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteInvitationAsync(int id)
    {
        var invitation = await _context.Invitations.FindAsync(id);
        if (invitation == null)
            return false;

        _context.Invitations.Remove(invitation);
        await _context.SaveChangesAsync();
        return true;
    }
}
