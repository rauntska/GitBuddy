namespace Graphite.Domain.Models;

public class PullRequest
{
    public int Id { get; set; }
    public long GitHubId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool Draft { get; set; }
    public string Url { get; set; } = string.Empty;
    public long Additions { get; set; }
    public long Deletions { get; set; }
    public long ChangedFiles { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    
    // Additional fields for PR details
    public string Description { get; set; } = string.Empty;
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public string? MergeableState { get; set; }
    public string? ChecksStatus { get; set; }
    public bool IsMerged { get; set; }
    public DateTime? MergedAt { get; set; }

    // Merge readiness fields
    public int? RequiredApprovingReviews { get; set; }
    public int CurrentApprovingReviews { get; set; }
    public bool HasUnresolvedThreads { get; set; }
    public bool IsMergeReady { get; set; }
    public string? MergeBlockReason { get; set; }

    public List<Review> Reviews { get; set; } = new();
    public List<ReviewThread> ReviewThreads { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public List<CheckRun> CheckRuns { get; set; } = new();
}