namespace GitBuddy.Domain.Models;

public class Comment
{
    public int Id { get; set; }
    public int PullRequestId { get; set; }
    public int? ReviewThreadId { get; set; }
    public long GitHubId { get; set; }
    public string Author { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Path { get; set; }
    public int? Line { get; set; }
    public bool IsOutdated { get; set; }

    // Editing support
    public DateTime? EditedAt { get; set; }
    public int EditCount { get; set; }

    // Reply chain support
    public int? ReplyToCommentId { get; set; }
    public Comment? ReplyToComment { get; set; }

    public ReviewThread? ReviewThread { get; set; }
    public List<CommentReaction> Reactions { get; set; } = new();
    public List<Comment> Replies { get; set; } = new();
}