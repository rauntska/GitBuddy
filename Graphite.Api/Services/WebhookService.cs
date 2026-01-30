using Graphite.Domain.Data;
using Graphite.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Models;

namespace Graphite.Api.Services;

public class WebhookService(
    AppDbContext context,
    IGitHubService gitHubService,
    ICacheService cacheService,
    ILogger<WebhookService> logger) : IWebhookService
{
    public async Task HandlePullRequestEventAsync(PullRequestEvent pullRequestEvent)
    {
        try
        {
            logger.LogInformation("Processing pull request event: {Action}", pullRequestEvent.Action);

            var action = pullRequestEvent.Action?.ToLower();
            var prData = pullRequestEvent.PullRequest;
            var repo = pullRequestEvent.Repository;

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
                        await CreateNewPRAsync(pullRequestEvent, config);
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing pull request event");
            throw;
        }
    }

    private async Task CreateNewPRAsync(PullRequestEvent pullRequestEvent, GitHubConfig config)
    {
        var prData = pullRequestEvent.PullRequest;
        var repo = pullRequestEvent.Repository;
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
            CreatedAt = prData.CreatedAt.DateTime,
            UpdatedAt = prData.UpdatedAt.DateTime,
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

    private async Task UpdatePRAsync(PullRequest existingPR, Octokit.Webhooks.Models.PullRequestEvent.PullRequest prData, bool reopened)
    {
        existingPR.Title = prData.Title;
        existingPR.Author = prData.User.Login;
        existingPR.AuthorAvatar = prData.User.AvatarUrl;
        existingPR.Draft = prData.Draft;
        existingPR.Additions = prData.Additions;
        existingPR.Deletions = prData.Deletions;
        existingPR.ChangedFiles = prData.ChangedFiles;
        existingPR.UpdatedAt = prData.UpdatedAt.DateTime;
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

    public async Task HandlePushEventAsync(PushEvent pushEvent)
    {
        try
        {
            var repo = pushEvent.Repository;
            var branch = pushEvent.Ref.Replace("refs/heads/", "");

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
                    await TriggerBackgroundRefreshAsync(config,null,null);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing push event");
            throw;
        }
    }

    public async Task HandleCommentEventAsync(IssueCommentEvent issueCommentEvent)
    {
        try
        {
            var action = issueCommentEvent.Action?.ToLower();
            var comment = issueCommentEvent.Comment;
            var repo = issueCommentEvent.Repository;
            var issue = issueCommentEvent.Issue;

            if (issue.PullRequest == null)
            {
                logger.LogInformation("Comment is not on a pull request");
                return;
            }

            logger.LogInformation("Comment webhook: {Action} - {Repository}#{PRNumber} - Comment #{CommentId}",
                action, repo.FullName, issue.Number, comment.Id);

            var existingPR = await context.PullRequests
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.GitHubId == issue.Number);

            if (existingPR == null)
            {
                logger.LogWarning("PR {Number} not found in database", issue.Number);
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
                await TriggerBackgroundRefreshAsync(config, repo.Name, issue.Number);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing comment event");
            throw;
        }
    }

    private async Task CreateCommentAsync(PullRequest pr, IssueComment commentData)
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
            Path = null,
            Line = null,
            IsOutdated = false,
            CreatedAt = commentData.CreatedAt.DateTime,
            UpdatedAt = commentData.UpdatedAt.DateTime
        };

        context.Comments.Add(comment);
        await context.SaveChangesAsync();
        logger.LogInformation("Created comment #{CommentId}", commentData.Id);
    }

    private async Task UpdateCommentAsync(PullRequest pr, IssueComment commentData)
    {
        var existingComment = pr.Comments.FirstOrDefault(c => c.GitHubId == commentData.Id);
        if (existingComment == null)
        {
            logger.LogWarning("Comment #{CommentId} not found for update", commentData.Id);
            return;
        }

        existingComment.Body = commentData.Body;
        existingComment.UpdatedAt = commentData.UpdatedAt.DateTime;

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

    public async Task HandleReviewCommentEventAsync(PullRequestReviewCommentEvent pullRequestReviewCommentEvent)
    {
        try
        {
            var action = pullRequestReviewCommentEvent.Action?.ToLower();
            var comment = pullRequestReviewCommentEvent.Comment;
            var repo = pullRequestReviewCommentEvent.Repository;
            var pr = pullRequestReviewCommentEvent.PullRequest;

            logger.LogInformation("Review comment webhook: {Action} - {Repository}#{PRNumber} - Comment #{CommentId}",
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
                    await CreateReviewCommentAsync(existingPR, comment);
                    break;
                case "edited":
                    await UpdateReviewCommentAsync(existingPR, comment);
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing review comment event");
            throw;
        }
    }

    private async Task CreateReviewCommentAsync(PullRequest pr, Octokit.Webhooks.Models.PullRequestReviewComment commentData)
    {
        var existingComment = pr.Comments.FirstOrDefault(c => c.GitHubId == commentData.Id);
        if (existingComment != null)
        {
            logger.LogInformation("Review comment #{CommentId} already exists", commentData.Id);
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
            IsOutdated =  false,
            CreatedAt = commentData.CreatedAt.DateTime,
            UpdatedAt = commentData.UpdatedAt.DateTime
        };

        context.Comments.Add(comment);
        await context.SaveChangesAsync();
        logger.LogInformation("Created review comment #{CommentId}", commentData.Id);
    }

    private async Task UpdateReviewCommentAsync(PullRequest pr, Octokit.Webhooks.Models.PullRequestReviewComment commentData)
    {
        var existingComment = pr.Comments.FirstOrDefault(c => c.GitHubId == commentData.Id);
        if (existingComment == null)
        {
            logger.LogWarning("Review comment #{CommentId} not found for update", commentData.Id);
            return;
        }

        existingComment.Body = commentData.Body;
        existingComment.UpdatedAt = commentData.UpdatedAt.DateTime;

        await context.SaveChangesAsync();
        logger.LogInformation("Updated review comment #{CommentId}", commentData.Id);
    }

    public async Task HandleReviewThreadEventAsync(PullRequestReviewThreadEvent pullRequestReviewThreadEvent)
    {
        try
        {
            var action = pullRequestReviewThreadEvent.Action?.ToLower();
            var thread = pullRequestReviewThreadEvent.Review;
            var repo = pullRequestReviewThreadEvent.Repository;
            var pr = pullRequestReviewThreadEvent.PullRequest;

            logger.LogInformation("Review thread webhook: {Action} - {Repository}#{PRNumber} - Thread #{ThreadId}",
                action, repo.FullName, pr.Number, thread.Id);

            var existingPR = await context.PullRequests
                .Include(p => p.ReviewThreads)
                .FirstOrDefaultAsync(p => p.GitHubId == pr.Id);

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing review thread event");
            throw;
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

    private Task TriggerBackgroundRefreshAsync(GitHubConfig config, string? repository, long? prNumber)
    {
        return Task.CompletedTask;
        _ = Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("Triggering background refresh for repository: {Repository}, PR: {PRNumber}", repository, prNumber);
                await cacheService.RefreshPullRequestsAsync(config);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in background refresh");
            }
        });
        return Task.CompletedTask;
    }
}
