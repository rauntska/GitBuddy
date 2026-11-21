namespace GitBuddy.Domain.Models;

public class AutomationRunLog
{
    public int Id { get; set; }
    public int AutomationRuleId { get; set; }
    public AutomationRule AutomationRule { get; set; } = null!;
    public int PullRequestId { get; set; }
    public DateTime TriggeredAt { get; set; }
    public bool ConditionsMatched { get; set; }
    public bool DryRun { get; set; }

    /// <summary>JSON array of human-readable action results.</summary>
    public string ActionsTaken { get; set; } = "[]";
    public string? Error { get; set; }

    /// <summary>Stable key (rule, PR, trigger, target) used to suppress duplicates and self-triggered loops.</summary>
    public string? IdempotencyKey { get; set; }
}
