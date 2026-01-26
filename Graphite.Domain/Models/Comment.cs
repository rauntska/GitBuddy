namespace Graphite.Domain.Models;

public class Comment
{
    public int Id { get; set; }
    public int PullRequestId { get; set; }
    public long GitHubId { get; set; }
    public string Author { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsResolved { get; set; }
    public string? Path { get; set; }
    public int? Line { get; set; }
    public bool IsOutdated { get; set; }

    public PullRequest PullRequest { get; set; } = null!;
}