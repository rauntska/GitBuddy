namespace Graphite.Domain.Models;

public class CommentDraft
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PullRequestId { get; set; }
    public int? ReviewThreadId { get; set; }
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
