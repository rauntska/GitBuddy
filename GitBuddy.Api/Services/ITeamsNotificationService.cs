using GitBuddy.Domain.Models;

namespace GitBuddy.Api.Services;

public interface ITeamsNotificationService
{
    Task SendNudgeAsync(PullRequest pr, List<string> reviewers, string nudgedBy);
}
