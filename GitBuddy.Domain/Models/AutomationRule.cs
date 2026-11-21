namespace GitBuddy.Domain.Models;

public enum AutomationTriggerType
{
    PROpened,
    PRUpdated,
    LabelAdded,
    LabelRemoved,
    ReviewSubmitted,
    CheckRunCompleted,
    ChecksStatusChanged,
    MergeableStateChanged
}

public class AutomationRule
{
    public int Id { get; set; }

    /// <summary>"owner/repo" or just "repo" matching PullRequest.Repository; null = org-wide.</summary>
    public string? RepositoryFullName { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool DryRun { get; set; } = true;
    public AutomationTriggerType TriggerType { get; set; }

    /// <summary>JSON array of { type, value } objects.</summary>
    public string Conditions { get; set; } = "[]";

    /// <summary>JSON array of { type, params } objects, executed in order.</summary>
    public string Actions { get; set; } = "[]";

    public int MinIntervalSeconds { get; set; }
    public int CreatedBy { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public int TriggerCount { get; set; }

    public List<AutomationRunLog> RunLogs { get; set; } = new();
}
