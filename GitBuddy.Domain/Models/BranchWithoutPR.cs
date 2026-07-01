namespace GitBuddy.Domain.Models;

public class BranchWithoutPR
{
    public int Id { get; set; }
    public string Owner { get; set; } = string.Empty;
    public string Repo { get; set; } = string.Empty;
    public string RepoFullName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = string.Empty;
    public DateTime? LastActivityAt { get; set; }
    public DateTime LastRefreshedAt { get; set; }
}
