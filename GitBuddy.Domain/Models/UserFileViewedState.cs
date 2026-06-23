namespace GitBuddy.Domain.Models;

public class UserFileViewedState
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int FileDiffId { get; set; }
    public string ViewedState { get; set; } = string.Empty; // VIEWED, UNVIEWED, DISMISSED
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public User User { get; set; } = null!;
    public FileDiff FileDiff { get; set; } = null!;
}
