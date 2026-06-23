namespace GitBuddy.Domain.Models;

public class CommentReaction
{
    public int Id { get; set; }
    public int CommentId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Reaction { get; set; } = string.Empty; // thumbsup, thumbsdown, laugh, hooray, confused, heart, rocket, eyes
    public DateTime CreatedAt { get; set; }

    public Comment Comment { get; set; } = null!;
}
