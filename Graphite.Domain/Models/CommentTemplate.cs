namespace Graphite.Domain.Models;

public class CommentTemplate
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Tags { get; set; } // Comma-separated: "security,performance,nitpick"
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsOrganizationTemplate { get; set; } // If true, available to all users

    public User User { get; set; } = null!;
}
