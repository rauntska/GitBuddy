namespace Graphite.Domain.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // OAuth fields
    public string? Provider { get; set; }
    public string? ProviderUserId { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? AccessToken { get; set; }
    
    // Navigation property
    public UserPreferences? Preferences { get; set; }
}
