namespace Graphite.Domain.Models;

public class PullRequest
{
    public int Id { get; set; }
    public int GitHubId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool Draft { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public int ChangedFiles { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    
    // Additional fields for PR details
    public string Description { get; set; } = string.Empty;
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public string? MergeableState { get; set; }
    public string? ChecksStatus { get; set; }

    public List<Review> Reviews { get; set; } = new();
    public List<ReviewThread> ReviewThreads { get; set; } = new();
}