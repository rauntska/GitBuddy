namespace GitBuddy.Domain.Models;

public class ChangelogEntry
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public DateOnly PublishedOn { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
