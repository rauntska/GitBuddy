namespace Graphite.Domain.Models;

public class ReviewThread
{
    public int Id { get; set; }
    public int PullRequestId { get; set; }
    public string GitHubId { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int? Line { get; set; }
    public string State { get; set; } = string.Empty; // RESOLVED, UNRESOLVED
    public bool IsResolved { get; set; }
    public bool IsOutdated { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string FirstCommentAuthor { get; set; } = string.Empty;
    public string FirstCommentBody { get; set; } = string.Empty;
    public int CommentCount { get; set; }

    public PullRequest PullRequest { get; set; } = null!;
}
