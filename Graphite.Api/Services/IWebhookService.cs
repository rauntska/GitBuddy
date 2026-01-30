using System.Text.Json;

namespace Graphite.Api.Services;

public interface IWebhookService
{
    Task ProcessWebhookAsync(string eventType, JsonElement payload);
    Task HandlePullRequestEventAsync(JsonElement payload);
    Task HandlePushEventAsync(JsonElement payload);
    Task HandleCommentEventAsync(JsonElement payload);
    Task HandleReviewThreadEventAsync(JsonElement payload);
}