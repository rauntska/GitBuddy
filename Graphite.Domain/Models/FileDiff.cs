namespace Graphite.Domain.Models;

public class FileDiff
{
    public int Id { get; set; }
    public int PullRequestId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string? OldPath { get; set; }
    public string Status { get; set; } = string.Empty; // added, modified, deleted, renamed
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public int Changes { get; set; }
    public string? Patch { get; set; }
    public string? Language { get; set; }
    
    public PullRequest PullRequest { get; set; } = null!;
}
