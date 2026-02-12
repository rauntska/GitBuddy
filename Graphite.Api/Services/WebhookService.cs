using Graphite.Api.DTOs;
using Graphite.Api.Extensions;
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
    private readonly ILanguageDetectionService _languageDetectionService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        AppDbContext context,
        IGitHubService gitHubService,
        ICacheService cacheService,
        IPullRequestStatusService statusService,
        ILanguageDetectionService languageDetectionService,
        INotificationService notificationService,
        ILogger<WebhookService> logger)
    {
        _context = context;
        _gitHubService = gitHubService;
        _cacheService = cacheService;
        _statusService = statusService;
        _languageDetectionService = languageDetectionService;
        _notificationService = notificationService;
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
                .FirstOrDefaultAsync(pr => pr.GitHubId == prData.Number);

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
                    await _notificationService.BroadcastPRClosedAsync(prId, gitHubId, repository, wasMerged);
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
        
        try
        {
            var fileDiffs = await _gitHubService.GetFileDiffsAsync(organization, repo.Name, prData.Number, config);
            AddFileDiffsToContext(pullRequest.Id, fileDiffs);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching file diffs for PR {Repository}#{Number}", repo.FullName, prData.Number);
        }
        
        _logger.LogInformation("Created new PR {Repository}#{Number}", repo.FullName, prData.Number);
        
        var fullPR = await _context.PullRequests
            .Include(p => p.Reviews)
            .Include(p => p.ReviewThreads)
            .FirstOrDefaultAsync(p => p.Id == pullRequest.Id);
        
        if (fullPR != null)
        {
            await _notificationService.BroadcastPRCreatedAsync(fullPR.ToPRListUpdateDto());
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

    private void AddFileDiffsToContext(int pullRequestId, List<GitHubFileDiffData> fileDiffs)
    {
        foreach (var fileDiff in fileDiffs)
        {
            var language = _languageDetectionService.DetectLanguage(fileDiff.Path);
            _context.FileDiffs.Add(new FileDiff
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

        var allReviews = await _context.Reviews
            .Where(r => r.PullRequestId == existingPr.Id)
            .ToListAsync();

        var reviewData = allReviews.Select(r => new GitHubReviewData(
            r.GitHubId,
            r.Reviewer,
            r.ReviewerAvatar,
            r.State,
            r.SubmittedAt
        )).ToList();

        existingPr.Status = _statusService.DeterminePrStatus(prData.Draft, reviewData);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated PR #{GitHubId}", existingPr.GitHubId);
        
        var fullPR = await _context.PullRequests
            .Include(p => p.Reviews)
            .Include(p => p.ReviewThreads)
            .FirstOrDefaultAsync(p => p.Id == existingPr.Id);
        
        if (fullPR != null)
        {
            await _notificationService.BroadcastPRUpdatedAsync(fullPR.ToPRListUpdateDto());
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
        await _notificationService.BroadcastCommentChangedAsync(pr.Id, "added", commentDto);
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
        await _notificationService.BroadcastCommentChangedAsync(pr.Id, "added", commentDto);
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
            //the actual thread is under json "thread" not "review"
            var thread = pullRequestReviewThreadEvent.Review;
            var repo = pullRequestReviewThreadEvent.Repository;
            var pr = pullRequestReviewThreadEvent.PullRequest;

            _logger.LogInformation("Review thread webhook: {Action} - {Repository}#{PRNumber} - Thread #{ThreadId}",
                action, repo?.FullName, pr.Number, thread?.Id);

            var existingPR = await _context.PullRequests
                .Include(p => p.ReviewThreads)
                .FirstOrDefaultAsync(p => p.GitHubId == pr.Number);

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
                await _notificationService.BroadcastThreadChangedAsync(existingThread.PullRequestId, existingThread.Id, true);
                break;

            case "unresolved":
                existingThread.IsResolved = false;
                existingThread.State = "UNRESOLVED";
                existingThread.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Unresolved review thread #{ThreadId}", threadId);
                await _notificationService.BroadcastThreadChangedAsync(existingThread.PullRequestId, existingThread.Id, false);
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

            _logger.LogInformation("Pull request review webhook: {Action} - {Repository}#{PRNumber} - Review #{ReviewId} - State: {State}",
                action, repo.FullName, pr.Number, review.Id, review.State);

            var existingPR = await _context.PullRequests
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.GitHubId == pr.Number);

            if (existingPR == null)
            {
                _logger.LogWarning("PR {Number} not found in database", pr.Number);
                return;
            }

            await ProcessPullRequestReviewActionAsync(action, existingPR, review);

            // Update PR status based on reviews
            var allReviews = await _context.Reviews
                .Where(r => r.PullRequestId == existingPR.Id)
                .ToListAsync();

            var reviewData = allReviews.Select(r => new GitHubReviewData(
                r.GitHubId,
                r.Reviewer,
                r.ReviewerAvatar,
                r.State,
                r.SubmittedAt
            )).ToList();

            existingPR.Status = _statusService.DeterminePrStatus(existingPR.Draft, reviewData);
            existingPR.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var config = await _context.GitHubConfigs.FirstOrDefaultAsync();
            if (config != null)
            {
                await TriggerBackgroundRefreshAsync(config, repo.Name, pr.Number);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pull request review event");
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
            _logger.LogInformation("Updated review #{ReviewId} with state {State}", reviewId, reviewState);
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
            _context.Reviews.Add(newReview);
            long reviewId = reviewData.Id;
            _logger.LogInformation("Created review #{ReviewId} with state {State}", reviewId, reviewState);
        }

        await _context.SaveChangesAsync();
        
        var reviewDto = new ReviewDto(
            existingReview?.Id ?? 0,
            reviewData.Id.ToString(),
            (string)reviewData.User.Login,
            (string?)reviewData.User.AvatarUrl,
            reviewState,
            (DateTime?)reviewData.SubmittedAt?.UtcDateTime
        );
        await _notificationService.BroadcastReviewAddedAsync(pr.Id, pr.Status, reviewDto);
    }

    private async Task UpdateExistingReviewAsync(PullRequest pr, dynamic reviewData, string reviewState)
    {
        var existingReview = pr.Reviews.FirstOrDefault(r => r.GitHubId == reviewData.Id.ToString());
        if (existingReview == null)
        {
            long reviewId = reviewData.Id;
            _logger.LogWarning("Review #{ReviewId} not found for update", reviewId);
            return;
        }

        existingReview.State = reviewState;
        existingReview.SubmittedAt = reviewData.SubmittedAt?.UtcDateTime;

        await _context.SaveChangesAsync();
        long id = reviewData.Id;
        _logger.LogInformation("Updated review #{ReviewId} with state {State}", id, reviewState);
    }

    private async Task DismissReviewAsync(PullRequest pr, long reviewId)
    {
        var existingReview = pr.Reviews.FirstOrDefault(r => r.GitHubId == reviewId.ToString());
        if (existingReview == null)
        {
            _logger.LogWarning("Review #{ReviewId} not found for dismissal", reviewId);
            return;
        }

        existingReview.State = "DISMISSED";
        await _context.SaveChangesAsync();
        _logger.LogInformation("Dismissed review #{ReviewId}", reviewId);
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
