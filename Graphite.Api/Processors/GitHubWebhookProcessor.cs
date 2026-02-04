using Graphite.Api.Services;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.PullRequest;
using Octokit.Webhooks.Events.IssueComment;
using Octokit.Webhooks.Events.PullRequestReview;
using Octokit.Webhooks.Events.PullRequestReviewComment;
using Octokit.Webhooks.Events.PullRequestReviewThread;

namespace Graphite.Api.Processors;

public class GitHubWebhookProcessor(IWebhookService webhookService, ILogger<GitHubWebhookProcessor> logger) 
    : WebhookEventProcessor
{
    protected override async ValueTask ProcessPullRequestWebhookAsync(
        WebhookHeaders headers,
        PullRequestEvent pullRequestEvent,
        PullRequestAction action,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing pull request webhook: {Action}", action);
        await webhookService.HandlePullRequestEventAsync(pullRequestEvent);
    }

    protected override async ValueTask ProcessPushWebhookAsync(
        WebhookHeaders headers,
        PushEvent pushEvent,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing push webhook");
        await webhookService.HandlePushEventAsync(pushEvent);
    }

    protected override async ValueTask ProcessIssueCommentWebhookAsync(
        WebhookHeaders headers,
        IssueCommentEvent issueCommentEvent,
        IssueCommentAction action,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing issue comment webhook: {Action}", action);
        await webhookService.HandleCommentEventAsync(issueCommentEvent);
    }

    protected override async ValueTask ProcessPullRequestReviewCommentWebhookAsync(
        WebhookHeaders headers,
        PullRequestReviewCommentEvent pullRequestReviewCommentEvent,
        PullRequestReviewCommentAction action,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing pull request review comment webhook: {Action}", action);
        await webhookService.HandleReviewCommentEventAsync(pullRequestReviewCommentEvent);
    }

    protected override async ValueTask ProcessPullRequestReviewThreadWebhookAsync(
        WebhookHeaders headers,
        PullRequestReviewThreadEvent pullRequestReviewThreadEvent,
        PullRequestReviewThreadAction action,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing pull request review thread webhook: {Action}", action);
        await webhookService.HandleReviewThreadEventAsync(pullRequestReviewThreadEvent);
    }

    protected override async ValueTask ProcessPullRequestReviewWebhookAsync(WebhookHeaders headers, PullRequestReviewEvent pullRequestReviewEvent,
        PullRequestReviewAction action, CancellationToken cancellationToken = new CancellationToken())
    {
        logger.LogInformation("Processing pull request review webhook: {Action}", action);
        await webhookService.HandlePullRequestReviewEventAsync(pullRequestReviewEvent);
    }
}
