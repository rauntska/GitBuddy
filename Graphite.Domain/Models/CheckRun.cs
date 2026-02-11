namespace Graphite.Domain.Models;

public class CheckRun
{
    public int Id { get; set; }
    public string GitHubId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;  // queued, in_progress, completed
    public string? Conclusion { get; set; }  // success, failure, neutral, cancelled, timed_out, etc.
    public string? Url { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public int PullRequestId { get; set; }
    public PullRequest PullRequest { get; set; } = null!;
}