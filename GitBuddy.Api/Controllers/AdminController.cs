using GitBuddy.Api.DTOs.Analytics;
using GitBuddy.Api.Services;
using GitBuddy.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GitBuddy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(
    IUserService userService,
    IInvitationService invitationService,
    IAllowlistService allowlistService,
    IAnalyticsService analyticsService,
    IConfiguration configuration)
    : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await userService.GetAllUsersAsync();
        return Ok(users.Select(u => new
        {
            u.Id,
            u.Username,
            u.Email,
            u.Name,
            u.AvatarUrl,
            u.Role,
            u.CreatedAt,
            u.LastLoginAt,
            u.Provider
        }));
    }

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleRequest request)
    {
        var success = await userService.UpdateUserRoleAsync(id, request.Role);
        if (!success)
            return NotFound(new { message = "User not found" });

        return Ok(new { message = "Role updated successfully" });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserId = User.FindFirst("UserId")?.Value;
        if (currentUserId == id.ToString())
        {
            return BadRequest(new { message = "Cannot delete your own account" });
        }

        var success = await userService.DeleteUserAsync(id);
        if (!success)
            return NotFound(new { message = "User not found" });

        return Ok(new { message = "User deleted successfully" });
    }

    [HttpGet("invitations")]
    public async Task<IActionResult> GetInvitations()
    {
        var invitations = await invitationService.GetAllInvitationsAsync();
        var frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:5173";
        
        return Ok(invitations.Select(i => new
        {
            i.Id,
            i.Email,
            i.GitHubUsername,
            i.Token,
            i.AssignedRole,
            i.CreatedAt,
            i.ExpiresAt,
            i.AcceptedAt,
            i.AcceptedByUserId,
            InviteUrl = $"{frontendUrl}/invite/{i.Token}",
            CreatedBy = i.CreatedByUser != null ? new { i.CreatedByUser.Id, i.CreatedByUser.Username } : null,
            Status = i.AcceptedAt.HasValue ? "Accepted" : 
                     (i.ExpiresAt.HasValue && i.ExpiresAt < DateTime.UtcNow ? "Expired" : "Pending")
        }));
    }

    [HttpPost("invitations")]
    public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationRequest request)
    {
        var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        
        DateTime? expiresAt = null;
        if (request.ExpiresInDays.HasValue && request.ExpiresInDays > 0)
        {
            expiresAt = DateTime.UtcNow.AddDays(request.ExpiresInDays.Value);
        }

        var invitation = await invitationService.CreateInvitationAsync(
            request.Email,
            request.GitHubUsername,
            request.AssignedRole,
            currentUserId,
            expiresAt
        );

        var frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:5173";

        return Ok(new
        {
            invitation.Id,
            invitation.Email,
            invitation.GitHubUsername,
            invitation.Token,
            invitation.AssignedRole,
            invitation.CreatedAt,
            invitation.ExpiresAt,
            InviteUrl = $"{frontendUrl}/invite/{invitation.Token}"
        });
    }

    [HttpDelete("invitations/{id}")]
    public async Task<IActionResult> RevokeInvitation(int id)
    {
        var success = await invitationService.RevokeInvitationAsync(id);
        if (!success)
            return NotFound(new { message = "Invitation not found or already accepted" });

        return Ok(new { message = "Invitation revoked successfully" });
    }

    [HttpGet("allowlist")]
    public async Task<IActionResult> GetAllowlist()
    {
        var allowlist = await allowlistService.GetAllAsync();
        return Ok(allowlist.Select(a => new
        {
            a.Id,
            a.Email,
            a.GitHubUsername,
            a.AssignedRole,
            a.CreatedAt,
            CreatedBy = a.CreatedByUser != null ? new { a.CreatedByUser.Id, a.CreatedByUser.Username } : null
        }));
    }

    [HttpPost("allowlist")]
    public async Task<IActionResult> AddToAllowlist([FromBody] AddToAllowlistRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.GitHubUsername))
        {
            return BadRequest(new { message = "Either email or GitHub username is required" });
        }

        var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

        var allowedUser = await allowlistService.AddToAllowlistAsync(
            request.Email,
            request.GitHubUsername,
            request.AssignedRole,
            currentUserId
        );

        return Ok(new
        {
            allowedUser.Id,
            allowedUser.Email,
            allowedUser.GitHubUsername,
            allowedUser.AssignedRole,
            allowedUser.CreatedAt
        });
    }

    [HttpDelete("allowlist/{id}")]
    public async Task<IActionResult> RemoveFromAllowlist(int id)
    {
        var success = await allowlistService.RemoveFromAllowlistAsync(id);
        if (!success)
            return NotFound(new { message = "Allowlist entry not found" });

        return Ok(new { message = "Removed from allowlist successfully" });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var users = await userService.GetAllUsersAsync();
        var invitations = await invitationService.GetAllInvitationsAsync();
        var allowlist = await allowlistService.GetAllAsync();

        return Ok(new
        {
            TotalUsers = users.Count(),
            AdminCount = users.Count(u => u.Role == UserRole.Admin),
            DeveloperCount = users.Count(u => u.Role == UserRole.Developer),
            PendingInvitations = invitations.Count(i => i.AcceptedAt == null && (i.ExpiresAt == null || i.ExpiresAt > DateTime.UtcNow)),
            AllowlistEntries = allowlist.Count()
        });
    }

    [HttpGet("analytics/throughput")]
    public async Task<IActionResult> GetAnalyticsThroughput([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string[]? authors)
    {
        var (f, t) = NormalizeWindow(from, to);
        var result = await analyticsService.GetThroughputAsync(f, t, authors);
        return Ok(result);
    }

    [HttpGet("analytics/reviewers")]
    public async Task<IActionResult> GetAnalyticsReviewers([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string[]? authors)
    {
        var (f, t) = NormalizeWindow(from, to);
        var result = await analyticsService.GetReviewerStatsAsync(f, t, authors);
        return Ok(result);
    }

    [HttpGet("analytics/health")]
    public async Task<IActionResult> GetAnalyticsHealth([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string[]? authors)
    {
        var (f, t) = NormalizeWindow(from, to);
        var result = await analyticsService.GetHealthAsync(f, t, authors);
        return Ok(result);
    }

    private static (DateTime from, DateTime to) NormalizeWindow(DateTime? from, DateTime? to)
    {
        var end = to ?? DateTime.UtcNow;
        var start = from ?? end.AddDays(-30);
        if (start.Kind == DateTimeKind.Unspecified) start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        if (end.Kind == DateTimeKind.Unspecified) end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
        return (start.ToUniversalTime(), end.ToUniversalTime());
    }
}

public record UpdateRoleRequest
{
    public UserRole Role { get; init; }
}
public record CreateInvitationRequest(string Email, string? GitHubUsername, UserRole AssignedRole = UserRole.Developer, int? ExpiresInDays = null);
public record AddToAllowlistRequest(string? Email, string? GitHubUsername, UserRole AssignedRole = UserRole.Developer);
