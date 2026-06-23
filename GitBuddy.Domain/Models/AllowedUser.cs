namespace GitBuddy.Domain.Models;

public class AllowedUser
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? GitHubUsername { get; set; }
    public UserRole AssignedRole { get; set; } = UserRole.Developer;
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
