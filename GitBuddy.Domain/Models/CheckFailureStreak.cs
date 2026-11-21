namespace GitBuddy.Domain.Models;

/// <summary>
/// Number of consecutive failed completions of one named check on a PR. Maintained from check_run webhooks
/// because CheckRun rows only reflect the latest commit and are replaced on refresh.
/// </summary>
public class CheckFailureStreak
{
    public int Id { get; set; }
    public int PullRequestId { get; set; }
    public string CheckName { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime UpdatedAt { get; set; }
}
