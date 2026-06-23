namespace GitBuddy.Domain.Models;

public class PendingReview
{
    public int Id { get; set; }
    public int PullRequestId { get; set; }
    public int UserId { get; set; }
    public string GitHubReviewId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public PullRequest PullRequest { get; set; } = null!;
    public User User { get; set; } = null!;
    public List<PendingComment> PendingComments { get; set; } = new();
}
