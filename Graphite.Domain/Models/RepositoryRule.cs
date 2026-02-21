namespace Graphite.Domain.Models;

public class RepositoryRule
{
    public int Id { get; set; }
    public string Repository { get; set; } = string.Empty;
    public string? BranchPattern { get; set; }
    public string? RulesetName { get; set; }
    public bool RequiresApprovingReviews { get; set; }
    public int? RequiredApprovingReviewCount { get; set; }
    public bool RequiresStatusChecks { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
