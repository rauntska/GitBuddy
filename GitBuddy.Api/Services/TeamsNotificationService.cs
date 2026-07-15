using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GitBuddy.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace GitBuddy.Api.Services;

public class TeamsNotificationService(
    IHttpClientFactory httpClientFactory,
    ICacheService cacheService,
    IConfiguration configuration,
    ILogger<TeamsNotificationService> logger) : ITeamsNotificationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task SendNudgeAsync(PullRequest pr, List<string> reviewers, string nudgedBy)
    {
        if (reviewers is null || reviewers.Count == 0)
            return;

        var config = await cacheService.GetConfigAsync();

        if (config is null || !config.TeamsEnabled || string.IsNullOrWhiteSpace(config.TeamsWebhookUrl))
        {
            logger.LogDebug("Teams nudge skipped (disabled or no webhook configured) for PR #{PrId}", pr.Id);
            return;
        }

        var payload = BuildAdaptiveCardPayload(pr, reviewers, nudgedBy, GetGitBuddyPrUrl(pr));
        var json = JsonSerializer.Serialize(payload, JsonOptions);

        try
        {
            using var client = httpClientFactory.CreateClient();
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(config.TeamsWebhookUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                logger.LogWarning("Teams webhook returned {Status} for PR #{PrId}: {Body}", (int)response.StatusCode, pr.Id, body);
            }
            else
            {
                logger.LogInformation("Teams nudge posted for PR #{PrId} to reviewers {Reviewers}", pr.Id, string.Join(", ", reviewers));
            }
        }
        catch (Exception ex)
        {
            // Teams outage must never break a nudge flow.
            logger.LogError(ex, "Failed to post Teams nudge for PR #{PrId}", pr.Id);
        }
    }

    private string GetGitBuddyPrUrl(PullRequest pr)
    {
        var frontendUrl = configuration["Frontend:Url"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(frontendUrl))
            return string.Empty;

        return $"{frontendUrl}/pr/{pr.Id}";
    }

    private static Dictionary<string, object> BuildAdaptiveCardPayload(PullRequest pr, List<string> reviewers, string nudgedBy, string gitBuddyUrl)
    {
        var reviewerList = string.Join(", ", reviewers);
        var title = $"Review requested: {pr.Title}";
        var gitHubLink = $"GitHub: [{pr.Repository}#{pr.GitHubId}]({pr.Url})";
        var gitBuddyLink = !string.IsNullOrWhiteSpace(gitBuddyUrl)
            ? $"GitBuddy: [View in dashboard]({gitBuddyUrl})"
            : string.Empty;

        var body = new List<object>
        {
            new Dictionary<string, object>
            {
                ["type"] = "TextBlock",
                ["text"] = title,
                ["size"] = "Medium",
                ["weight"] = "Bolder",
                ["wrap"] = true
            },
            new Dictionary<string, object>
            {
                ["type"] = "TextBlock",
                ["text"] = gitHubLink,
                ["wrap"] = true,
                ["spacing"] = "Small"
            },
            new Dictionary<string, object>
            {
                ["type"] = "TextBlock",
                ["text"] = $"Reviewers: {reviewerList}",
                ["wrap"] = true,
                ["spacing"] = "Small"
            },
            new Dictionary<string, object>
            {
                ["type"] = "TextBlock",
                ["text"] = $"Nudged by {nudgedBy}",
                ["wrap"] = true,
                ["spacing"] = "Small",
                ["isSubtle"] = true
            }
        };

        if (!string.IsNullOrWhiteSpace(gitBuddyLink))
        {
            // Insert the GitBuddy link right after the GitHub link.
            body.Insert(1, new Dictionary<string, object>
            {
                ["type"] = "TextBlock",
                ["text"] = gitBuddyLink,
                ["wrap"] = true,
                ["spacing"] = "Small"
            });
        }

        var actions = new List<object>
        {
            new Dictionary<string, object>
            {
                ["type"] = "Action.OpenUrl",
                ["title"] = "Open on GitHub",
                ["url"] = pr.Url
            }
        };

        if (!string.IsNullOrWhiteSpace(gitBuddyUrl))
        {
            actions.Add(new Dictionary<string, object>
            {
                ["type"] = "Action.OpenUrl",
                ["title"] = "Open in GitBuddy",
                ["url"] = gitBuddyUrl
            });
        }

        return new Dictionary<string, object>
        {
            ["type"] = "message",
            ["attachments"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["contentType"] = "application/vnd.microsoft.card.adaptive",
                    ["contentUrl"] = string.Empty,
                    ["content"] = new Dictionary<string, object>
                    {
                        ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
                        ["type"] = "AdaptiveCard",
                        ["version"] = "1.4",
                        ["body"] = body,
                        ["actions"] = actions
                    }
                }
            }
        };
    }
}
