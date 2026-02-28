namespace Graphite.Domain.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserRole Role { get; set; } = UserRole.Developer;

    public string? Provider { get; set; }
    public string? ProviderUserId { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? AccessToken { get; set; }
    public string? PersonalAccessToken { get; set; }

    public int? InvitationId { get; set; }
    public Invitation? Invitation { get; set; }

    public UserPreferences? Preferences { get; set; }
}
