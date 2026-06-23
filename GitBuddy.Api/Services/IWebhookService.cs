using Octokit.Webhooks.Events;

namespace GitBuddy.Api.Services;

public interface IWebhookService
{
    Task HandlePullRequestEventAsync(PullRequestEvent pullRequestEvent);
    Task HandlePushEventAsync(PushEvent pushEvent);
    Task HandleCommentEventAsync(IssueCommentEvent issueCommentEvent);
    Task HandleReviewCommentEventAsync(PullRequestReviewCommentEvent pullRequestReviewCommentEvent);
    Task HandleReviewThreadEventAsync(PullRequestReviewThreadEvent pullRequestReviewThreadEvent);
    Task HandlePullRequestReviewEventAsync(PullRequestReviewEvent pullRequestReviewEvent);
}