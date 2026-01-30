using Graphite.Domain.Data;
using Graphite.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Graphite.Api.DTOs;

namespace Graphite.Api.Services;

public class WebhookService(
    AppDbContext context,
    IGitHubService gitHubService,
    ICacheService cacheService,
    ILogger<WebhookService> logger) : IWebhookService
{
    public async Task ProcessWebhookAsync(string eventType, JsonElement payload)
    {
        try
        {
            logger.LogInformation("Processing webhook event: {EventType}", eventType);

            switch (eventType.ToLower())
            {
                case "pull_request":
                    await HandlePullRequestEventAsync(payload);
                    break;
                case "push":
                    await HandlePushEventAsync(payload);
                    break;
                case "issue_comment":
                    await HandleCommentEventAsync(payload);
                    break;
                case "pull_request_review_comment":
                    await HandleCommentEventAsync(payload);
                    break;
                case "pull_request_review_thread":
                    await HandleReviewThreadEventAsync(payload);
                    break;
                default:
                    logger.LogInformation("Unsupported webhook event type: {EventType}", eventType);
                    break;
            }

            logger.LogInformation("Successfully processed webhook event: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing webhook event: {EventType}", eventType);
            throw;
        }
    }

    public async Task HandlePullRequestEventAsync(JsonElement payload)
    {
        PullRequestWebhookPayload? prPayload;
        try
        {
            prPayload = payload.Deserialize<PullRequestWebhookPayload>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize pull request webhook payload");
            return;
        }

        if (prPayload?.PullRequest == null)
        {
            logger.LogWarning("Invalid pull request webhook payload");
            return;
        }

        var action = prPayload.Action.ToLower();
        var repo = prPayload.Repository;
        var prData = prPayload.PullRequest;

        logger.LogInformation("PR webhook: {Action} - {Repository}#{Number}", action, repo.FullName, prData.Number);

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            logger.LogWarning("No GitHub configuration found");
            return;
        }

        var existingPR = await context.PullRequests
            .Include(pr => pr.Reviews)
            .Include(pr => pr.ReviewThreads)
            .Include(pr => pr.Comments)
            .FirstOrDefaultAsync(pr => pr.GitHubId == prData.Id);

        switch (action)
        {
            case "opened":
            case "reopened":
                if (existingPR == null)
                {
                    await CreateNewPRAsync(prPayload, config);
                }
                else
                {
                    await UpdatePRAsync(existingPR, prData, action == "reopened");
                }
                break;

            case "synchronized":
                if (existingPR != null)
                {
                    await UpdatePRAsync(existingPR, prData, false);
                }
                break;

            case "closed":
                if (existingPR != null)
                {
                    context.PullRequests.Remove(existingPR);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Deleted closed PR {Repository}#{Number}", repo.FullName, prData.Number);
                }
                break;

            case "edited":
                if (existingPR != null)
                {
                    await UpdatePRAsync(existingPR, prData, false);
                }
                break;

            default:
                logger.LogInformation("Unhandled PR action: {Action}", action);
                break;
        }

        await TriggerBackgroundRefreshAsync(config, repo.Name, prData.Number);
    }

    private async Task CreateNewPRAsync(PullRequestWebhookPayload prPayload, GitHubConfig config)
    {
        var repo = prPayload.Repository;
        var prData = prPayload.PullRequest;
        var organization = repo.FullName.Split('/')[0];

        var reviews = await gitHubService.GetReviewsAsync(organization, repo.Name, prData.Number, config);
        var reviewThreads = await gitHubService.GetReviewThreadsAsync(organization, repo.Name, prData.Number, config);
        var comments = await gitHubService.GetCommentsAsync(organization, repo.Name, prData.Number, config);

        var status = DeterminePrStatus(prData.Draft, reviews);

        var pullRequest = new PullRequest
        {
            GitHubId = prData.Id,
            Title = prData.Title,
            Repository = repo.Name,
            Author = prData.User.Login,
            AuthorAvatar = prData.User.AvatarUrl,
            Status = status,
            Draft = prData.Draft,
            Url = prData.Url,
            Additions = prData.Additions,
            Deletions = prData.Deletions,
            ChangedFiles = prData.ChangedFiles,
            CreatedAt = prData.CreatedAt,
            UpdatedAt = prData.UpdatedAt,
            LastSyncedAt = DateTime.UtcNow,
            Description = prData.Body ?? string.Empty,
            SourceBranch = prData.Head.Ref,
            TargetBranch = prData.Base.Ref,
            MergeableState = null
        };

        context.PullRequests.Add(pullRequest);
        await context.SaveChangesAsync();

        foreach (var review in reviews)
        {
            context.Reviews.Add(new Review
            {
                PullRequestId = pullRequest.Id,
                Reviewer = review.Reviewer,
                ReviewerAvatar = review.ReviewerAvatar,
                State = review.State,
                SubmittedAt = review.SubmittedAt
            });
        }

        foreach (var thread in reviewThreads)
        {
            context.ReviewThreads.Add(new ReviewThread
            {
                PullRequestId = pullRequest.Id,
                GitHubId = thread.GitHubId,
                Path = thread.Path,
                Line = thread.Line,
                State = thread.State,
                IsResolved = thread.IsResolved,
                IsOutdated = thread.IsOutdated,
                CreatedAt = thread.CreatedAt,
                UpdatedAt = thread.UpdatedAt,
                FirstCommentAuthor = thread.FirstCommentAuthor,
                FirstCommentBody = thread.FirstCommentBody,
                CommentCount = thread.CommentCount
            });
        }

        foreach (var comment in comments)
        {
            var reviewThread = pullRequest.ReviewThreads.FirstOrDefault(rt => rt.GitHubId == comment.ReviewThreadId);
            context.Comments.Add(new Comment
            {
                PullRequestId = pullRequest.Id,
                ReviewThreadId = reviewThread?.Id,
                GitHubId = comment.GitHubId,
                Author = comment.Author,
                AuthorAvatar = comment.AuthorAvatar,
                Body = comment.Body,
                Path = comment.Path,
                Line = comment.Line,
                IsOutdated = comment.IsOutdated,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            });
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Created new PR {Repository}#{Number}", repo.FullName, prData.Number);
    }

    private async Task UpdatePRAsync(PullRequest existingPR, PullRequestData prData, bool reopened)
    {
        existingPR.Title = prData.Title;
        existingPR.Author = prData.User.Login;
        existingPR.AuthorAvatar = prData.User.AvatarUrl;
        existingPR.Draft = prData.Draft;
        existingPR.Additions = prData.Additions;
        existingPR.Deletions = prData.Deletions;
        existingPR.ChangedFiles = prData.ChangedFiles;
        existingPR.UpdatedAt = prData.UpdatedAt;
        existingPR.LastSyncedAt = DateTime.UtcNow;
        existingPR.Description = prData.Body ?? string.Empty;
        existingPR.SourceBranch = prData.Head.Ref;
        existingPR.TargetBranch = prData.Base.Ref;

        if (reopened)
        {
            existingPR.Status = "AwaitingReview";
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Updated PR #{GitHubId}", existingPR.GitHubId);
    }

    public async Task HandlePushEventAsync(JsonElement payload)
    {
        PushWebhookPayload? pushData;
        try
        {
            pushData = payload.Deserialize<PushWebhookPayload>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize push webhook payload");
            return;
        }

        if (pushData?.Repository == null)
        {
            logger.LogWarning("Invalid push webhook payload");
            return;
        }

        var repo = pushData.Repository;
        var branch = pushData.Ref.Replace("refs/heads/", "");

        logger.LogInformation("Push event: {Repository} - Branch: {Branch}", repo.FullName, branch);

        var affectedPRs = await context.PullRequests
            .Where(pr => pr.SourceBranch == branch || pr.TargetBranch == branch)
            .ToListAsync();

        foreach (var pr in affectedPRs)
        {
            pr.UpdatedAt = DateTime.UtcNow;
            pr.LastSyncedAt = DateTime.UtcNow;
            logger.LogInformation("Updated PR #{GitHubId} due to push to branch {Branch}", pr.GitHubId, branch);
        }

        if (affectedPRs.Any())
        {
            await context.SaveChangesAsync();
            var config = await context.GitHubConfigs.FirstOrDefaultAsync();
            if (config != null)
            {
                await TriggerBackgroundRefreshAsync(config);
            }
        }
    }

    public async Task HandleCommentEventAsync(JsonElement payload)
    {
        CommentWebhookPayload? commentData;
        try
        {
            commentData = payload.Deserialize<CommentWebhookPayload>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize comment webhook payload");
            return;
        }

        if (commentData == null || commentData.Comment == null || commentData.Repository == null)
        {
            logger.LogWarning("Invalid comment webhook payload - missing required fields");
            return;
        }

        var action = commentData.Action?.ToLower();
        if (string.IsNullOrEmpty(action))
        {
            logger.LogWarning("Invalid comment webhook payload - missing action");
            return;
        }

        var comment = commentData.Comment;
        var repo = commentData.Repository;
        var pr = commentData.PullRequest;

        if (pr == null)
        {
            logger.LogInformation("Comment is not on a pull request");
            return;
        }

        logger.LogInformation("Comment webhook: {Action} - {Repository}#{PRNumber} - Comment #{CommentId}",
            action, repo.FullName, pr.Number, comment.Id);

        var existingPR = await context.PullRequests
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.GitHubId == pr.Number);

        if (existingPR == null)
        {
            logger.LogWarning("PR {Number} not found in database", pr.Number);
            return;
        }

        switch (action)
        {
            case "created":
                await CreateCommentAsync(existingPR, comment);
                break;
            case "edited":
                await UpdateCommentAsync(existingPR, comment);
                break;
            case "deleted":
                await DeleteCommentAsync(existingPR, comment.Id);
                break;
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config != null)
        {
            await TriggerBackgroundRefreshAsync(config, repo.Name, pr.Number);
        }
    }

    private async Task CreateCommentAsync(PullRequest pr, CommentData commentData)
    {
        var existingComment = pr.Comments.FirstOrDefault(c => c.GitHubId == commentData.Id);
        if (existingComment != null)
        {
            logger.LogInformation("Comment #{CommentId} already exists", commentData.Id);
            return;
        }

        var comment = new Comment
        {
            PullRequestId = pr.Id,
            GitHubId = commentData.Id,
            Author = commentData.User.Login,
            AuthorAvatar = commentData.User.AvatarUrl,
            Body = commentData.Body,
            Path = commentData.Path,
            Line = commentData.Line,
            IsOutdated = commentData.IsOutdated ?? false,
            CreatedAt = commentData.CreatedAt,
            UpdatedAt = commentData.UpdatedAt
        };

        context.Comments.Add(comment);
        await context.SaveChangesAsync();
        logger.LogInformation("Created comment #{CommentId}", commentData.Id);
    }

    private async Task UpdateCommentAsync(PullRequest pr, CommentData commentData)
    {
        var existingComment = pr.Comments.FirstOrDefault(c => c.GitHubId == commentData.Id);
        if (existingComment == null)
        {
            logger.LogWarning("Comment #{CommentId} not found for update", commentData.Id);
            return;
        }

        existingComment.Body = commentData.Body;
        existingComment.UpdatedAt = commentData.UpdatedAt;

        await context.SaveChangesAsync();
        logger.LogInformation("Updated comment #{CommentId}", commentData.Id);
    }

    private async Task DeleteCommentAsync(PullRequest pr, long commentId)
    {
        var existingComment = pr.Comments.FirstOrDefault(c => c.GitHubId == commentId);
        if (existingComment == null)
        {
            logger.LogWarning("Comment #{CommentId} not found for deletion", commentId);
            return;
        }

        context.Comments.Remove(existingComment);
        await context.SaveChangesAsync();
        logger.LogInformation("Deleted comment #{CommentId}", commentId);
    }

    public async Task HandleReviewThreadEventAsync(JsonElement payload)
    {
        ReviewThreadWebhookPayload? threadData;
        try
        {
            threadData = payload.Deserialize<ReviewThreadWebhookPayload>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize review thread webhook payload");
            return;
        }

        if (threadData?.Thread == null)
        {
            logger.LogWarning("Invalid review thread webhook payload");
            return;
        }

        var action = threadData.Action.ToLower();
        var thread = threadData.Thread;
        var repo = threadData.Repository;
        var pr = threadData.PullRequest;

        if (pr == null)
        {
            logger.LogInformation("Review thread is not on a pull request");
            return;
        }

        logger.LogInformation("Review thread webhook: {Action} - {Repository}#{PRNumber} - Thread #{ThreadId}", 
            action, repo.FullName, pr.Number, thread.Id);

        var existingPR = await context.PullRequests
            .Include(p => p.ReviewThreads)
            .FirstOrDefaultAsync(p => p.GitHubId == pr.Number);

        if (existingPR == null)
        {
            logger.LogWarning("PR {Number} not found in database", pr.Number);
            return;
        }

        var existingThread = existingPR.ReviewThreads.FirstOrDefault(rt => rt.GitHubId == thread.Id.ToString());

        switch (action)
        {
            case "resolved":
                if (existingThread != null)
                {
                    existingThread.IsResolved = true;
                    existingThread.State = "RESOLVED";
                    existingThread.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    logger.LogInformation("Resolved review thread #{ThreadId}", thread.Id);
                }
                break;

            case "unresolved":
                if (existingThread != null)
                {
                    existingThread.IsResolved = false;
                    existingThread.State = "UNRESOLVED";
                    existingThread.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    logger.LogInformation("Unresolved review thread #{ThreadId}", thread.Id);
                }
                break;
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config != null)
        {
            await TriggerBackgroundRefreshAsync(config, repo.Name, pr.Number);
        }
    }

    private static string DeterminePrStatus(bool isDraft, List<GitHubReviewData> reviews)
    {
        if (isDraft) return "Draft";

        var hasApproved = reviews.Any(r => r.State == "Approved");
        var hasChangesRequested = reviews.Any(r => r.State == "ChangesRequested");
        var hasComments = reviews.Any(r => r.State == "Commented");

        if (hasApproved && !hasChangesRequested) return "Approved";
        if (hasChangesRequested) return "ChangesRequested";
        if (hasComments) return "Reviewed";
        return "AwaitingReview";
    }

    private async Task TriggerBackgroundRefreshAsync(GitHubConfig config, string? repository = null, int? prNumber = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("Triggering background refresh");
                if (repository != null && prNumber != null)
                {
                    var organization = await context.GitHubConfigs.Select(c => c.Organization).FirstOrDefaultAsync();
                    if (organization != null)
                    {
                        await cacheService.RefreshPullRequestsAsync(config);
                    }
                }
                else
                {
                    await cacheService.RefreshPullRequestsAsync(config);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in background refresh");
            }
        });
    }
}