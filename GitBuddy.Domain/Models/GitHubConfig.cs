namespace GitBuddy.Domain.Models;

public class GitHubConfig
{
    public int Id { get; set; }
    public string Organization { get; set; } = string.Empty;
    public string PersonalAccessToken { get; set; } = string.Empty;
    public DateTime? LastRefresh { get; set; }
    public int RefreshIntervalMinutes { get; set; } = 5;
    public int BranchRefreshIntervalMinutes { get; set; } = 30;
    public string AppId { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string InstallationId { get; set; } = string.Empty;
    public bool UseGitHubApp { get; set; } = false;
    public bool DeleteOldPRs { get; set; } = false;

    // Org-wide Microsoft Teams incoming-webhook (Workflow) URL for reviewer nudges.
    public string TeamsWebhookUrl { get; set; } = string.Empty;
    public bool TeamsEnabled { get; set; } = false;
}