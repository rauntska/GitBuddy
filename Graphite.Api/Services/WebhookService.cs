using Graphite.Domain.Data;
using Graphite.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Models;

namespace Graphite.Api.Services;

public class WebhookService : IWebhookService
{
    private readonly AppDbContext _context;
    private readonly IGitHubService _gitHubService;
    private readonly ICacheService _cacheService;
    private readonly IPullRequestStatusService _statusService;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        AppDbContext context,
        IGitHubService gitHubService,
        ICacheService cacheService,
        IPullRequestStatusService statusService,
        ILogger<WebhookService> logger)
    {
        _context = context;
        _gitHubService = gitHubService;
        _cacheService = cacheService;
        _statusService = statusService;
        _logger = logger;
    }
    public async Task HandlePullRequestEventAsync(PullRequestEvent pullRequestEvent)
    {
        try
        {
            _logger.LogInformation("Processing pull request event: {Action}", pullRequestEvent.Action);

            var action = pullRequestEvent.Action?.ToLower();
            var prData = pullRequestEvent.PullRequest;
            var repo = pullRequestEvent.Repository;

            _logger.LogInformation("PR webhook: {Action} - {Repository}#{Number}", action, repo.FullName, prData.Number);

            var config = await _context.GitHubConfigs.FirstOrDefaultAsync();
            if (config == null)
            {
                _logger.LogWarning("No GitHub configuration found");
                return;
            }

            var existingPR = await _context.PullRequests
                .Include(pr => pr.Reviews)
                .Include(pr => pr.ReviewThreads)
                .Include(pr => pr.Comments)
                .FirstOrDefaultAsync(pr => pr.GitHubId == prData.Id);

            await ProcessPullRequestActionAsync(action, existingPR, pullRequestEvent, config, repo, prData);
            await TriggerBackgroundRefreshAsync(config, repo.Name, prData.Number);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pull request event");
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

            case "synchronized":
            case "edited":
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

                    if (config.DeleteOldPRs)
                    {
                        _context.PullRequests.Remove(existingPR);
                        _logger.LogInformation("Deleted closed PR {Repository}#{Number}", repo.FullName, prData.Number);
                    }
                    else
                    {
                        existingPR.IsMerged = wasMerged;
                        existingPR.MergedAt = mergedAt;
                        existingPR.Status = wasMerged ? "Merged" : "Closed";
                        _logger.LogInformation("Marked PR {Repository}#{Number} as {Status}",
                            repo.FullName, prData.Number, wasMerged ? "merged" : "closed");
                    }

                    await _context.SaveChangesAsync();
                }
                break;

            default:
                _logger.LogInformation("Unhandled PR action: {Action}", action);
                break;
        }
    }

    private async Task CreateNewPrAsync(PullRequestEvent pullRequestEvent, GitHubConfig config)
    {
        var prData = pullRequestEvent.PullRequest;
        var repo = pullRequestEvent.Repository;
        var organization = repo.FullName.Split('/')[0];

        var reviews = await _gitHubService.GetReviewsAsync(organization, repo.Name, prData.Number, config);
        var reviewThreads = await _gitHubService.GetReviewThreadsAsync(organization, repo.Name, prData.Number, config);
        var comments = await _gitHubService.GetCommentsAsync(organization, repo.Name, prData.Number, config);

        var status = _statusService.DeterminePrStatus(prData.Draft, reviews);

        var pullRequest = CreatePullRequestEntity(prData, repo, status);
        _context.PullRequests.Add(pullRequest);
        await _context.SaveChangesAsync();

        AddReviewsToContext(pullRequest.Id, reviews);
        AddReviewThreadsToContext(pullRequest.Id, reviewThreads);
        AddCommentsToContext(pullRequest, comments);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Created new PR {Repository}#{Number}", repo.FullName, prData.Number);
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
            MergeableState = null,
            IsMerged = false,
            MergedAt = null
        };
    }

    private void AddReviewsToContext(int pullRequestId, List<GitHubReviewData> reviews)
    {
        foreach (var review in reviews)
        {
            _context.Reviews.Add(new Review
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
            _context.ReviewThreads.Add(new ReviewThread
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
            _context.Comments.Add(new Comment
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

    private async Task UpdatePrAsync(PullRequest existingPr, Octokit.Webhooks.Models.PullRequestEvent.PullRequest prData, bool reopened)
    {
        UpdatePullRequestFields(existingPr, prData, reopened);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated PR #{GitHubId}", existingPr.GitHubId);
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

            _logger.LogInformation("Push event: {Repository} - Branch: {Branch}", repo.FullName, branch);

            var affectedPRs = await _context.PullRequests
                .Where(pr => pr.SourceBranch == branch || pr.TargetBranch == branch)
                .ToListAsync();

            if (!affectedPRs.Any())
                return;

            UpdateAffectedPullRequests(affectedPRs, branch);

            await _context.SaveChangesAsync();

            var config = await _context.GitHubConfigs.FirstOrDefaultAsync();
            if (config != null)
            {
                await TriggerBackgroundRefreshAsync(config, null, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing push event");
            throw;
        }
    }

    private void UpdateAffectedPullRequests(List<PullRequest> affectedPRs, string branch)
    {
        foreach (var pr in affectedPRs)
        {
            pr.UpdatedAt = DateTime.UtcNow;
            pr.LastSyncedAt = DateTime.UtcNow;
            _logger.LogInformation("Updated PR #{GitHubId} due to push to branch {Branch}", pr.GitHubId, branch);
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
                _logger.LogInformation("Comment is not on a pull request");
                return;
            }

            _logger.LogInformation("Comment webhook: {Action} - {Repository}#{PRNumber} - Comment #{CommentId}",
                action, repo.FullName, issue.Number, comment.Id);

            var existingPR = await _context.PullRequests
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.GitHubId == issue.Number);

            if (existingPR == null)
            {
                _logger.LogWarning("PR {Number} not found in database", issue.Number);
                return;
            }

            await ProcessCommentActionAsync(action, existingPR, comment);

            var config = await _context.GitHubConfigs.FirstOrDefaultAsync();
            if (config != null)
            {
                await TriggerBackgroundRefreshAsync(config, repo.Name, issue.Number);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing comment event");
            throw;
        }
    }

    private async Task ProcessCommentActionAsync(string? action, PullRequest existingPR, IssueComment comment)
    {
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
    }

    private async Task CreateCommentAsync(PullRequest pr, IssueComment commentData)
    {
        if (pr.Comments.Any(c => c.GitHubId == commentData.Id))
        {
            _logger.LogInformation("Comment #{CommentId} already exists", commentData.Id);
            return;
        }

        var comment = BuildCommentEntity(pr.Id, commentData);
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created comment #{CommentId}", commentData.Id);
    }

    private async Task UpdateCommentAsync(PullRequest pr, IssueComment commentData)
    {
        var existingComment = pr.Comments.FirstOrDefault(c => c.GitHubId == commentData.Id);
        if (existingComment == null)
        {
            _logger.LogWarning("Comment #{CommentId} not found for update", commentData.Id);
            return;
        }

        existingComment.Body = commentData.Body;
        existingComment.UpdatedAt = commentData.UpdatedAt.DateTime;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated comment #{CommentId}", commentData.Id);
    }

    private async Task DeleteCommentAsync(PullRequest pr, long commentId)
    {
        var existingComment = pr.Comments.FirstOrDefault(c => c.GitHubId == commentId);
        if (existingComment == null)
        {
            _logger.LogWarning("Comment #{CommentId} not found for deletion", commentId);
            return;
        }

        _context.Comments.Remove(existingComment);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted comment #{CommentId}", commentId);
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

            _logger.LogInformation("Review comment webhook: {Action} - {Repository}#{PRNumber} - Comment #{CommentId}",
                action, repo.FullName, pr.Number, comment.Id);

            var existingPR = await _context.PullRequests
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.GitHubId == pr.Number);

            if (existingPR == null)
            {
                _logger.LogWarning("PR {Number} not found in database", pr.Number);
                return;
            }

            await ProcessReviewCommentActionAsync(action, existingPR, comment);

            var config = await _context.GitHubConfigs.FirstOrDefaultAsync();
            if (config != null)
            {
                await TriggerBackgroundRefreshAsync(config, repo.Name, pr.Number);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing review comment event");
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
            _logger.LogInformation("Review comment #{CommentId} already exists", commentData.Id);
            return;
        }

        var comment = BuildReviewCommentEntity(pr.Id, commentData);
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created review comment #{CommentId}", commentData.Id);
    }

    private async Task UpdateReviewCommentAsync(PullRequest pr, Octokit.Webhooks.Models.PullRequestReviewComment commentData)
    {
        var existingComment = pr.Comments.FirstOrDefault(c => c.GitHubId == commentData.Id);
        if (existingComment == null)
        {
            _logger.LogWarning("Review comment #{CommentId} not found for update", commentData.Id);
            return;
        }

        existingComment.Body = commentData.Body;
        existingComment.UpdatedAt = commentData.UpdatedAt.DateTime;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated review comment #{CommentId}", commentData.Id);
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
            var thread = pullRequestReviewThreadEvent.Review;
            var repo = pullRequestReviewThreadEvent.Repository;
            var pr = pullRequestReviewThreadEvent.PullRequest;

            _logger.LogInformation("Review thread webhook: {Action} - {Repository}#{PRNumber} - Thread #{ThreadId}",
                action, repo.FullName, pr.Number, thread.Id);

            var existingPR = await _context.PullRequests
                .Include(p => p.ReviewThreads)
                .FirstOrDefaultAsync(p => p.GitHubId == pr.Id);

            if (existingPR == null)
            {
                _logger.LogWarning("PR {Number} not found in database", pr.Number);
                return;
            }

            var existingThread = existingPR.ReviewThreads.FirstOrDefault(rt => rt.GitHubId == thread.Id.ToString());
            await ProcessReviewThreadActionAsync(action, existingThread, thread.Id);

            var config = await _context.GitHubConfigs.FirstOrDefaultAsync();
            if (config != null)
            {
                await TriggerBackgroundRefreshAsync(config, repo.Name, pr.Number);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing review thread event");
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
                await _context.SaveChangesAsync();
                _logger.LogInformation("Resolved review thread #{ThreadId}", threadId);
                break;

            case "unresolved":
                existingThread.IsResolved = false;
                existingThread.State = "UNRESOLVED";
                existingThread.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Unresolved review thread #{ThreadId}", threadId);
                break;
        }
    }

    private Task TriggerBackgroundRefreshAsync(GitHubConfig config, string? repository, long? prNumber)
    {
        return Task.CompletedTask;
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Triggering background refresh for repository: {Repository}, PR: {PRNumber}", repository, prNumber);
                await _cacheService.RefreshPullRequestsAsync(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background refresh");
            }
        });
        return Task.CompletedTask;
    }
}
