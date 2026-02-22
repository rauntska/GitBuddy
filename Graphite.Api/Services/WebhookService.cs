using Graphite.Api.DTOs;
using Graphite.Api.Extensions;
using Graphite.Domain.Data;
using Graphite.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Models;

namespace Graphite.Api.Services;

public class WebhookService(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestStatusService statusService,
    ILanguageDetectionService languageDetectionService,
    INotificationService notificationService,
    ILogger<WebhookService> logger,
    IServiceScopeFactory serviceScopeFactory)
    : IWebhookService
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

            var existingPr = await context.PullRequests
                .Include(pr => pr.Reviews)
                .Include(pr => pr.ReviewThreads)
                .Include(pr => pr.Comments)
                .FirstOrDefaultAsync(pr => pr.GitHubId == prData.Number);

            await ProcessPullRequestActionAsync(action, existingPr, pullRequestEvent, config, repo, prData);
            await TriggerBackgroundRefreshAsync(config, repo.Name, prData.Number);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing pull request event");
            throw;
        }
    }

    private async Task ProcessPullRequestActionAsync(
        string? action,
        PullRequest? existingPR,
        PullRequestEvent pullRequestEvent,
        GitHubConfig config,
        Repository repo,
        Octokit.Webhooks.Models.PullRequestEvent.PullRequest prData)
    {
        switch (action)
        {
            case "opened":
            case "reopened":
                if (existingPR == null)
                {
                    await CreateNewPrAsync(pullRequestEvent, config);
                }
                else
                {
                    await UpdatePrAsync(existingPR, prData, action == "reopened");
                }
                break;

            case "synchronize":
            case "synchronized":
            case "edited":
            case "ready_for_review":
            case "converted_to_draft":
                if (existingPR != null)
                {
                    await UpdatePrAsync(existingPR, prData, false);
                }
                break;

            case "closed":
                if (existingPR != null)
                {
                    bool wasMerged = prData.Merged.HasValue && prData.Merged.Value;
                    DateTime? mergedAt = prData.MergedAt?.UtcDateTime;
                    var prId = existingPR.Id;
                    var gitHubId = existingPR.GitHubId;
                    var repository = repo.Name;

                    if (config.DeleteOldPRs)
                    {
                        context.PullRequests.Remove(existingPR);
                        logger.LogInformation("Deleted closed PR {Repository}#{Number}", repo.FullName, prData.Number);
                    }
                    else
                    {
                        existingPR.IsMerged = wasMerged;
                        existingPR.MergedAt = mergedAt;
                        existingPR.Status = wasMerged ? "Merged" : "Closed";
                        logger.LogInformation("Marked PR {Repository}#{Number} as {Status}",
                            repo.FullName, prData.Number, wasMerged ? "merged" : "closed");
                    }

                    await context.SaveChangesAsync();
                    await notificationService.BroadcastPRClosedAsync(prId, gitHubId, repository, wasMerged);
                }
                break;

            default:
                logger.LogInformation("Unhandled PR action: {Action}", action);
                break;
        }
    }

    private async Task CreateNewPrAsync(PullRequestEvent pullRequestEvent, GitHubConfig config)
    {
        var prData = pullRequestEvent.PullRequest;
        var repo = pullRequestEvent.Repository;
        var organization = repo.FullName.Split('/')[0];

        var reviews = await gitHubService.GetReviewsAsync(organization, repo.Name, prData.Number, config);
        var reviewThreads = await gitHubService.GetReviewThreadsAsync(organization, repo.Name, prData.Number, config);
        var comments = await gitHubService.GetCommentsAsync(organization, repo.Name, prData.Number, config);

        var status = statusService.DeterminePrStatus(prData.Draft, reviews);

        var pullRequest = CreatePullRequestEntity(prData, repo, status);
        context.PullRequests.Add(pullRequest);
        await context.SaveChangesAsync();

        AddReviewsToContext(pullRequest.Id, reviews);
        AddReviewThreadsToContext(pullRequest.Id, reviewThreads);
        AddCommentsToContext(pullRequest, comments);

        await context.SaveChangesAsync();
        
        try
        {
            var fileDiffs = await gitHubService.GetFileDiffsAsync(organization, repo.Name, prData.Number, config);
            AddFileDiffsToContext(pullRequest.Id, fileDiffs);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching file diffs for PR {Repository}#{Number}", repo.FullName, prData.Number);
        }
        
        logger.LogInformation("Created new PR {Repository}#{Number}", repo.FullName, prData.Number);
        
        var fullPR = await context.PullRequests
            .Include(p => p.Reviews)
            .Include(p => p.ReviewThreads)
            .FirstOrDefaultAsync(p => p.Id == pullRequest.Id);
        
        if (fullPR != null)
        {
            await notificationService.BroadcastPRCreatedAsync(fullPR.ToPRListUpdateDto());
        }
    }

    private static PullRequest CreatePullRequestEntity(
        Octokit.Webhooks.Models.PullRequestEvent.PullRequest prData,
        Repository repo,
        string status)
    {
        return new PullRequest
        {
            GitHubId = prData.Number,
            Title = prData.Title,
            Repository = repo.Name,
            Author = prData.User.Login,
            AuthorAvatar = prData.User.AvatarUrl,
            Status = status,
            Draft = prData.Draft,
            Url = prData.HtmlUrl,
            Additions = prData.Additions,
            Deletions = prData.Deletions,
            ChangedFiles = prData.ChangedFiles,
            CreatedAt = prData.CreatedAt.DateTime,
            UpdatedAt = prData.UpdatedAt.DateTime,
            LastSyncedAt = DateTime.UtcNow,
            Description = prData.Body ?? string.Empty,
            SourceBranch = prData.Head.Ref,
            TargetBranch = prData.Base.Ref,
            MergeableState = null,
            IsMerged = false,
            MergedAt = null
        };
    }

    private void AddReviewsToContext(int pullRequestId, List<GitHubReviewData> reviews)
    {
        foreach (var review in reviews)
        {
            context.Reviews.Add(new Review
            {
                PullRequestId = pullRequestId,
                GitHubId = review.GitHubId,
                Reviewer = review.Reviewer,
                ReviewerAvatar = review.ReviewerAvatar,
                State = review.State,
                SubmittedAt = review.SubmittedAt
            });
        }
    }

    private void AddReviewThreadsToContext(int pullRequestId, List<GitHubReviewThreadData> reviewThreads)
    {
        foreach (var thread in reviewThreads)
        {
            context.ReviewThreads.Add(new ReviewThread
            {
                PullRequestId = pullRequestId,
                GitHubId = thread.GitHubId,
                Path = thread.Path,
                Line = thread.Line,
                DiffSide = MapDiffSide(thread.diffSide),
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
    }

    private static Domain.Models.DiffSide MapDiffSide(Octokit.GraphQL.Model.DiffSide gitHubDiffSide)
    {
        return gitHubDiffSide switch
        {
            Octokit.GraphQL.Model.DiffSide.Left => Domain.Models.DiffSide.Left,
            Octokit.GraphQL.Model.DiffSide.Right => Domain.Models.DiffSide.Right,
            _ => Domain.Models.DiffSide.Right
        };
    }

    private void AddCommentsToContext(PullRequest pullRequest, List<GitHubCommentData> comments)
    {
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
    }

    private void AddFileDiffsToContext(int pullRequestId, List<GitHubFileDiffData> fileDiffs)
    {
        foreach (var fileDiff in fileDiffs)
        {
            var language = languageDetectionService.DetectLanguage(fileDiff.Path);
            context.FileDiffs.Add(new FileDiff
            {
                PullRequestId = pullRequestId,
                Path = fileDiff.Path,
                OldPath = fileDiff.OldPath,
                Status = fileDiff.Status,
                Additions = fileDiff.Additions,
                Deletions = fileDiff.Deletions,
                Changes = fileDiff.Changes,
                Patch = fileDiff.Patch,
                Language = language
            });
        }
    }

    private async Task UpdatePrAsync(PullRequest existingPr, Octokit.Webhooks.Models.PullRequestEvent.PullRequest prData, bool reopened)
    {
        UpdatePullRequestFields(existingPr, prData, reopened);

        var allReviews = await context.Reviews
            .Where(r => r.PullRequestId == existingPr.Id)
            .ToListAsync();

        var reviewData = allReviews.Select(r => new GitHubReviewData(
            r.GitHubId,
            r.Reviewer,
            r.ReviewerAvatar,
            r.State,
            r.SubmittedAt
        )).ToList();

        existingPr.Status = statusService.DeterminePrStatus(prData.Draft, reviewData);

        await context.SaveChangesAsync();
        logger.LogInformation("Updated PR #{GitHubId}", existingPr.GitHubId);
        
        var fullPR = await context.PullRequests
            .Include(p => p.Reviews)
            .Include(p => p.ReviewThreads)
            .FirstOrDefaultAsync(p => p.Id == existingPr.Id);
        
        if (fullPR != null)
        {
            await notificationService.BroadcastPRUpdatedAsync(fullPR.ToPRListUpdateDto());
        }
    }

    private static void UpdatePullRequestFields(PullRequest existingPr, Octokit.Webhooks.Models.PullRequestEvent.PullRequest prData, bool reopened)
    {
        existingPr.Title = prData.Title;
        existingPr.Author = prData.User.Login;
        existingPr.AuthorAvatar = prData.User.AvatarUrl;
        existingPr.Draft = prData.Draft;
        existingPr.Additions = prData.Additions;
        existingPr.Deletions = prData.Deletions;
        existingPr.ChangedFiles = prData.ChangedFiles;
        existingPr.UpdatedAt = prData.UpdatedAt.DateTime;
        existingPr.LastSyncedAt = DateTime.UtcNow;
        existingPr.Description = prData.Body ?? string.Empty;
        existingPr.SourceBranch = prData.Head.Ref;
        existingPr.TargetBranch = prData.Base.Ref;

        if (reopened)
        {
            existingPr.Status = "AwaitingReview";
            existingPr.IsMerged = false;
            existingPr.MergedAt = null;
        }
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

            if (affectedPRs.Count == 0)
                return;

            UpdateAffectedPullRequests(affectedPRs, branch);

            await context.SaveChangesAsync();

            var config = await context.GitHubConfigs.FirstOrDefaultAsync();
            if (config != null)
            {
                await TriggerBackgroundRefreshAsync(config, null, null);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing push event");
            throw;
        }
    }

    private void UpdateAffectedPullRequests(List<PullRequest> affectedPRs, string branch)
    {
        foreach (var pr in affectedPRs)
        {
            pr.UpdatedAt = DateTime.UtcNow;
            pr.LastSyncedAt = DateTime.UtcNow;
            logger.LogInformation("Updated PR #{GitHubId} due to push to branch {Branch}", pr.GitHubId, branch);
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

            var existingPr = await context.PullRequests
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.GitHubId == issue.Number);

            if (existingPr == null)
            {
                logger.LogWarning("PR {Number} not found in database", issue.Number);
                return;
            }

            await ProcessCommentActionAsync(action, existingPr, comment);

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

    private async Task ProcessCommentActionAsync(string? action, PullRequest existingPr, IssueComment comment)
    {
        switch (action)
        {
            case "created":
                await CreateCommentAsync(existingPr, comment);
                break;
            case "edited":
                await UpdateCommentAsync(existingPr, comment);
                break;
            case "deleted":
                await DeleteCommentAsync(existingPr, comment.Id);
                break;
        }
    }

    private async Task CreateCommentAsync(PullRequest pr, IssueComment commentData)
    {
        if (pr.Comments.Any(c => c.GitHubId == commentData.Id))
        {
            logger.LogInformation("Comment #{CommentId} already exists", commentData.Id);
            return;
        }

        var comment = BuildCommentEntity(pr.Id, commentData);
        context.Comments.Add(comment);
        await context.SaveChangesAsync();
        logger.LogInformation("Created comment #{CommentId}", commentData.Id);
        
        var commentDto = new CommentDto(
            comment.Id,
            commentData.Id,
            null,
            commentData.User.Login,
            commentData.User.AvatarUrl,
            commentData.Body,
            commentData.CreatedAt.DateTime,
            commentData.UpdatedAt.DateTime,
            null,
            null,
            false
        );
        await notificationService.BroadcastCommentChangedAsync(pr.Id, "added", commentDto);
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

    private static Comment BuildCommentEntity(int pullRequestId, IssueComment commentData)
    {
        return new Comment
        {
            PullRequestId = pullRequestId,
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

            var existingPr = await context.PullRequests
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.GitHubId == pr.Number);

            if (existingPr == null)
            {
                logger.LogWarning("PR {Number} not found in database", pr.Number);
                return;
            }

            await ProcessReviewCommentActionAsync(action, existingPr, comment);

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

    private async Task ProcessReviewCommentActionAsync(string? action, PullRequest existingPR, Octokit.Webhooks.Models.PullRequestReviewComment comment)
    {
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
    }

    private async Task CreateReviewCommentAsync(PullRequest pr, Octokit.Webhooks.Models.PullRequestReviewComment commentData)
    {
        if (pr.Comments.Any(c => c.GitHubId == commentData.Id))
        {
            logger.LogInformation("Review comment #{CommentId} already exists", commentData.Id);
            return;
        }

        var comment = BuildReviewCommentEntity(pr.Id, commentData);
        context.Comments.Add(comment);
        await context.SaveChangesAsync();
        logger.LogInformation("Created review comment #{CommentId}", commentData.Id);
        
        var commentDto = new CommentDto(
            comment.Id,
            commentData.Id,
            null,
            commentData.User.Login,
            commentData.User.AvatarUrl,
            commentData.Body,
            commentData.CreatedAt.DateTime,
            commentData.UpdatedAt.DateTime,
            commentData.Path,
            commentData.Line,
            false
        );
        await notificationService.BroadcastCommentChangedAsync(pr.Id, "added", commentDto);
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

    private static Comment BuildReviewCommentEntity(int pullRequestId, Octokit.Webhooks.Models.PullRequestReviewComment commentData)
    {
        return new Comment
        {
            PullRequestId = pullRequestId,
            GitHubId = commentData.Id,
            Author = commentData.User.Login,
            AuthorAvatar = commentData.User.AvatarUrl,
            Body = commentData.Body,
            Path = commentData.Path,
            Line = commentData.Line,
            IsOutdated = false,
            CreatedAt = commentData.CreatedAt.DateTime,
            UpdatedAt = commentData.UpdatedAt.DateTime
        };
    }

    public async Task HandleReviewThreadEventAsync(PullRequestReviewThreadEvent pullRequestReviewThreadEvent)
    {
        try
        {
            var action = pullRequestReviewThreadEvent.Action?.ToLower();
            //the actual thread is under json "thread" not "review"
            var thread = pullRequestReviewThreadEvent.Review;
            var repo = pullRequestReviewThreadEvent.Repository;
            var pr = pullRequestReviewThreadEvent.PullRequest;

            logger.LogInformation("Review thread webhook: {Action} - {Repository}#{PRNumber} - Thread #{ThreadId}",
                action, repo?.FullName, pr.Number, thread?.Id);

            var existingPr = await context.PullRequests
                .Include(p => p.ReviewThreads)
                .FirstOrDefaultAsync(p => p.GitHubId == pr.Number);

            if (existingPr == null)
            {
                logger.LogWarning("PR {Number} not found in database", pr.Number);
                return;
            }

            var existingThread = existingPr.ReviewThreads.FirstOrDefault(rt => rt.GitHubId == thread.Id.ToString());
            await ProcessReviewThreadActionAsync(action, existingThread, thread.Id);

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

    private async Task ProcessReviewThreadActionAsync(string? action, ReviewThread? existingThread, long threadId)
    {
        if (existingThread == null)
            return;

        switch (action)
        {
            case "resolved":
                existingThread.IsResolved = true;
                existingThread.State = "RESOLVED";
                existingThread.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                logger.LogInformation("Resolved review thread #{ThreadId}", threadId);
                await notificationService.BroadcastThreadChangedAsync(existingThread.PullRequestId, existingThread.Id, true);
                break;

            case "unresolved":
                existingThread.IsResolved = false;
                existingThread.State = "UNRESOLVED";
                existingThread.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                logger.LogInformation("Unresolved review thread #{ThreadId}", threadId);
                await notificationService.BroadcastThreadChangedAsync(existingThread.PullRequestId, existingThread.Id, false);
                break;
        }
    }

    public async Task HandlePullRequestReviewEventAsync(PullRequestReviewEvent pullRequestReviewEvent)
    {
        try
        {
            var action = pullRequestReviewEvent.Action?.ToLower();
            var review = pullRequestReviewEvent.Review;
            var repo = pullRequestReviewEvent.Repository;
            var pr = pullRequestReviewEvent.PullRequest;

            logger.LogInformation("Pull request review webhook: {Action} - {Repository}#{PRNumber} - Review #{ReviewId} - State: {State}",
                action, repo.FullName, pr.Number, review.Id, review.State);

            var existingPr = await context.PullRequests
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.GitHubId == pr.Number);

            if (existingPr == null)
            {
                logger.LogWarning("PR {Number} not found in database", pr.Number);
                return;
            }

            await ProcessPullRequestReviewActionAsync(action, existingPr, review);

            // Update PR status based on reviews
            var allReviews = await context.Reviews
                .Where(r => r.PullRequestId == existingPr.Id)
                .ToListAsync();

            var reviewData = allReviews.Select(r => new GitHubReviewData(
                r.GitHubId,
                r.Reviewer,
                r.ReviewerAvatar,
                r.State,
                r.SubmittedAt
            )).ToList();

            existingPr.Status = statusService.DeterminePrStatus(existingPr.Draft, reviewData);
            existingPr.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            var config = await context.GitHubConfigs.FirstOrDefaultAsync();
            if (config != null)
            {
                await TriggerBackgroundRefreshAsync(config, repo.Name, pr.Number);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing pull request review event");
            throw;
        }
    }

    private async Task ProcessPullRequestReviewActionAsync(string? action, PullRequest existingPR, dynamic review)
    {
        var reviewStateString = "Commented";
        if (review.State != null)
        {
            reviewStateString = review.State.ToString();
        }

        var reviewState = reviewStateString.ToPascalCase();

        switch (action)
        {
            case "submitted":
                await CreateOrUpdateReviewAsync(existingPR, review, reviewState);
                break;
            case "edited":
                await UpdateExistingReviewAsync(existingPR, review, reviewState);
                break;
            case "dismissed":
                await DismissReviewAsync(existingPR, review.Id);
                break;
        }
    }

    private async Task CreateOrUpdateReviewAsync(PullRequest pr, dynamic reviewData, string reviewState)
    {
        var existingReview = pr.Reviews.FirstOrDefault(r => r.GitHubId == reviewData.Id.ToString());

        if (existingReview != null)
        {
            existingReview.State = reviewState;
            existingReview.SubmittedAt = reviewData.SubmittedAt?.UtcDateTime;
            long reviewId = reviewData.Id;
            logger.LogInformation("Updated review #{ReviewId} with state {State}", reviewId, reviewState);
        }
        else
        {
            var newReview = new Review
            {
                PullRequestId = pr.Id,
                GitHubId = reviewData.Id.ToString(),
                Reviewer = reviewData.User.Login,
                ReviewerAvatar = reviewData.User.AvatarUrl,
                State = reviewState,
                SubmittedAt = reviewData.SubmittedAt?.UtcDateTime
            };
            context.Reviews.Add(newReview);
            long reviewId = reviewData.Id;
            logger.LogInformation("Created review #{ReviewId} with state {State}", reviewId, reviewState);
        }

        await context.SaveChangesAsync();
        
        var reviewDto = new ReviewDto(
            existingReview?.Id ?? 0,
            reviewData.Id.ToString(),
            (string)reviewData.User.Login,
            (string?)reviewData.User.AvatarUrl,
            reviewState,
            (DateTime?)reviewData.SubmittedAt?.UtcDateTime
        );
        await notificationService.BroadcastReviewAddedAsync(pr.Id, pr.Status, reviewDto);
    }

    private async Task UpdateExistingReviewAsync(PullRequest pr, dynamic reviewData, string reviewState)
    {
        var existingReview = pr.Reviews.FirstOrDefault(r => r.GitHubId == reviewData.Id.ToString());
        if (existingReview == null)
        {
            long reviewId = reviewData.Id;
            logger.LogWarning("Review #{ReviewId} not found for update", reviewId);
            return;
        }

        existingReview.State = reviewState;
        existingReview.SubmittedAt = reviewData.SubmittedAt?.UtcDateTime;

        await context.SaveChangesAsync();
        long id = reviewData.Id;
        logger.LogInformation("Updated review #{ReviewId} with state {State}", id, reviewState);
    }

    private async Task DismissReviewAsync(PullRequest pr, long reviewId)
    {
        var existingReview = pr.Reviews.FirstOrDefault(r => r.GitHubId == reviewId.ToString());
        if (existingReview == null)
        {
            logger.LogWarning("Review #{ReviewId} not found for dismissal", reviewId);
            return;
        }

        existingReview.State = "DISMISSED";
        await context.SaveChangesAsync();
        logger.LogInformation("Dismissed review #{ReviewId}", reviewId);
    }

    private Task TriggerBackgroundRefreshAsync(GitHubConfig config, string? repository, long? prNumber)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("Triggering background refresh for repository: {Repository}, PR: {PRNumber}", repository, prNumber);
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
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
