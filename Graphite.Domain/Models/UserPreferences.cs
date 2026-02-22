namespace Graphite.Domain.Models;

public class UserPreferences
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    // Diff viewer preferences
    public string DiffViewMode { get; set; } = "unified"; // "split" or "unified"
    public int FileTreeWidth { get; set; } = 256; // in pixels
    public int CommentsPanelWidth { get; set; } = 320; // in pixels
    public bool FileTreeVisible { get; set; } = true;
    
    // List view preferences
    public string ListViewMode { get; set; } = "normal"; // "compact" or "normal"
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User User { get; set; } = null!;
}
