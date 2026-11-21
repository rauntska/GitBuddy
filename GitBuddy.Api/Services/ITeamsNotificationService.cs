using GitBuddy.Domain.Models;

namespace GitBuddy.Api.Services;

public interface ITeamsNotificationService
{
    Task SendNudgeAsync(PullRequest pr, List<string> reviewers, string nudgedBy);

    /// <summary>Posts a general message to the configured Teams channel (used by automation rules). Never throws.</summary>
    Task<bool> SendMessageAsync(PullRequest pr, string title, string text);
}
