namespace Graphite.Domain.Models;

public class Invitation
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? GitHubUsername { get; set; }
    public string Token { get; set; } = Guid.NewGuid().ToString();
    public UserRole AssignedRole { get; set; } = UserRole.Developer;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public int? AcceptedByUserId { get; set; }
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
