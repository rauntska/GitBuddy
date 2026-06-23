namespace GitBuddy.Domain.Models;

public class PendingComment
{
    public int Id { get; set; }
    public int PendingReviewId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Line { get; set; }
    public DateTime CreatedAt { get; set; }

    public PendingReview PendingReview { get; set; } = null!;
}
