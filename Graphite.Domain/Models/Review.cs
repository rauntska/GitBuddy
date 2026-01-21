namespace Graphite.Domain.Models;

public class Review
{
    public int Id { get; set; }
    public int PullRequestId { get; set; }
    public string Reviewer { get; set; } = string.Empty;
    public string? ReviewerAvatar { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }

    public PullRequest PullRequest { get; set; } = null!;
}