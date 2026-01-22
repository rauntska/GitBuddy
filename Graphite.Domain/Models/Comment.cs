namespace Graphite.Domain.Models;

public class Comment
{
    public int Id { get; set; }
    public int PullRequestId { get; set; }
    public int Count { get; set; }
    public int ResolvedCount { get; set; }
    public int PendingCount { get; set; }
    public DateTime? LastUpdated { get; set; }

    public PullRequest PullRequest { get; set; } = null!;
}