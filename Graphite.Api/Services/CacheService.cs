using Graphite.Domain.Data;
using Graphite.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Services;

public class CacheService(
    AppDbContext context,
    IGitHubService gitHubService,
    ILanguageDetectionService languageDetectionService,
    IRepositoryRuleService repositoryRuleService,
    IPullRequestStatusService statusService,
    ILogger<CacheService> logger)
    : ICacheService
{
    public async Task RefreshPullRequestsAsync(GitHubConfig config, long? prNumber = null, string? repository = null)
    {
        var prDataList = await FetchPullRequestsAsync(config, prNumber, repository);
        
        foreach (var prData in prDataList)
        {
            await SyncPullRequestAsync(config, prData);
        }

        await context.SaveChangesAsync();
        
        if (!prNumber.HasValue)
        {
            await CleanupOldPRsAsync(prDataList);
        }
    }

    private async Task<List<GitHubPRData>> FetchPullRequestsAsync(GitHubConfig config, long? prNumber, string? repository)
    {
        if (prNumber.HasValue && !string.IsNullOrEmpty(repository))
        {
            var prData = await gitHubService.GetPullRequestAsync(config.Organization, repository, prNumber.Value, config);
            if (prData == null)
            {
                logger.LogWarning("Could not fetch PR {Repository}#{PrNumber}", repository, prNumber);
                return [];
            }
            return [prData];
        }
        
        return await gitHubService.GetOpenPullRequestsAsync(config.Organization, config);
    }

    private async Task SyncPullRequestAsync(GitHubConfig config, GitHubPRData prData)
    {
        var existingPR = await context.PullRequests
            .Include(pr => pr.Reviews)
            .Include(pr => pr.ReviewThreads)
            .Include(pr => pr.Comments)
            .Include(pr => pr.CheckRuns)
            .FirstOrDefaultAsync(pr => pr.GitHubId == prData.Id && pr.Repository == prData.Repository);

        existingPR = await UpsertPullRequestAsync(existingPR, prData);
        
        SyncReviews(existingPR, prData);
        SyncReviewThreads(existingPR, prData);
        await context.SaveChangesAsync();
        await SyncCommentsAsync(existingPR, prData, config);
        SyncCheckRuns(existingPR, prData);
        await SyncFileDiffsAsync(existingPR, prData, config);
        await SyncMergeReadinessAsync(existingPR);
    }

    private async Task<PullRequest> UpsertPullRequestAsync(PullRequest? existingPR, GitHubPRData prData)
    {
        if (existingPR != null)
        {
            UpdatePullRequestFields(existingPR, prData);
            return existingPR;
        }

        var newPR = CreatePullRequest(prData);
        context.PullRequests.Add(newPR);
        await context.SaveChangesAsync();
        return newPR;
    }

    private static void UpdatePullRequestFields(PullRequest pr, GitHubPRData data)
    {
        pr.Title = data.Title;
        pr.Repository = data.Repository;
        pr.Author = data.Author;
        pr.AuthorAvatar = data.AuthorAvatar;
        pr.Status = data.Status;
        pr.Draft = data.Draft;
        pr.Additions = data.Additions;
        pr.Deletions = data.Deletions;
        pr.ChangedFiles = data.ChangedFiles;
        pr.UpdatedAt = data.UpdatedAt;
        pr.LastSyncedAt = DateTime.UtcNow;
        pr.Description = data.Description;
        pr.SourceBranch = data.SourceBranch;
        pr.TargetBranch = data.TargetBranch;
        pr.MergeableState = data.MergeableState;
        pr.ChecksStatus = data.ChecksStatus;
    }

    private static PullRequest CreatePullRequest(GitHubPRData data)
    {
        return new PullRequest
        {
            GitHubId = data.Id,
            Title = data.Title,
            Repository = data.Repository,
            Author = data.Author,
            AuthorAvatar = data.AuthorAvatar,
            Status = data.Status,
            Draft = data.Draft,
            Url = data.Url,
            Additions = data.Additions,
            Deletions = data.Deletions,
            ChangedFiles = data.ChangedFiles,
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt,
            LastSyncedAt = DateTime.UtcNow,
            Description = data.Description,
            SourceBranch = data.SourceBranch,
            TargetBranch = data.TargetBranch,
            MergeableState = data.MergeableState,
            ChecksStatus = data.ChecksStatus
        };
    }

    private void SyncReviews(PullRequest pr, GitHubPRData prData)
    {
        var existingIds = pr.Reviews.Select(r => r.GitHubId).ToHashSet();
        var incomingIds = (prData.Reviews ?? []).Select(r => r.GitHubId).ToHashSet();

        foreach (var reviewId in incomingIds.Except(existingIds))
        {
            var reviewData = prData.Reviews!.First(r => r.GitHubId == reviewId);
            context.Reviews.Add(new Review
            {
                PullRequestId = pr.Id,
                GitHubId = reviewData.GitHubId,
                Reviewer = reviewData.Reviewer,
                ReviewerAvatar = reviewData.ReviewerAvatar,
                State = reviewData.State,
                SubmittedAt = reviewData.SubmittedAt
            });
        }

        foreach (var reviewId in existingIds.Except(incomingIds))
        {
            var review = pr.Reviews.First(r => r.GitHubId == reviewId);
            context.Reviews.Remove(review);
        }
    }

    private void SyncReviewThreads(PullRequest pr, GitHubPRData prData)
    {
        var existingIds = pr.ReviewThreads.Select(rt => rt.GitHubId).ToHashSet();
        var incomingIds = (prData.ReviewThreads ?? []).Select(rt => rt.GitHubId).ToHashSet();

        foreach (var thread in prData.ReviewThreads ?? [])
        {
            if (!existingIds.Contains(thread.GitHubId))
            {
                context.ReviewThreads.Add(new ReviewThread
                {
                    PullRequestId = pr.Id,
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
            else
            {
                var existingThread = pr.ReviewThreads.First(rt => rt.GitHubId == thread.GitHubId);
                existingThread.Path = thread.Path;
                existingThread.Line = thread.Line;
                existingThread.State = thread.State;
                existingThread.IsResolved = thread.IsResolved;
                existingThread.IsOutdated = thread.IsOutdated;
                existingThread.UpdatedAt = thread.UpdatedAt;
                existingThread.FirstCommentAuthor = thread.FirstCommentAuthor;
                existingThread.FirstCommentBody = thread.FirstCommentBody;
                existingThread.CommentCount = thread.CommentCount;
            }
        }

        foreach (var threadId in existingIds.Except(incomingIds))
        {
            var thread = pr.ReviewThreads.First(rt => rt.GitHubId == threadId);
            context.ReviewThreads.Remove(thread);
        }
    }

    private async Task SyncCommentsAsync(PullRequest pr, GitHubPRData prData, GitHubConfig config)
    {
        try
        {
            var comments = await gitHubService.GetCommentsAsync(config.Organization, prData.Repository, prData.Id, config);
            var existingIds = pr.Comments.Select(c => c.GitHubId).ToHashSet();

            foreach (var comment in comments)
            {
                var reviewThread = pr.ReviewThreads.FirstOrDefault(rt => rt.GitHubId == comment.ReviewThreadId);
                
                if (!existingIds.Contains(comment.GitHubId))
                {
                    context.Comments.Add(new Comment
                    {
                        PullRequestId = pr.Id,
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
                else
                {
                    var existingComment = pr.Comments.First(c => c.GitHubId == comment.GitHubId);
                    existingComment.ReviewThreadId = reviewThread?.Id;
                    existingComment.Author = comment.Author;
                    existingComment.AuthorAvatar = comment.AuthorAvatar;
                    existingComment.Body = comment.Body;
                    existingComment.Path = comment.Path;
                    existingComment.Line = comment.Line;
                    existingComment.IsOutdated = comment.IsOutdated;
                    existingComment.UpdatedAt = comment.UpdatedAt;
                }
            }

            var incomingIds = comments.Select(c => c.GitHubId).ToHashSet();
            foreach (var comment in pr.Comments.Where(c => !incomingIds.Contains(c.GitHubId)))
            {
                context.Comments.Remove(comment);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching comments for PR {Organization}/{Repository}#{PullRequestNumber}", 
                config.Organization, prData.Repository, prData.Id);
        }
    }

    private void SyncCheckRuns(PullRequest pr, GitHubPRData prData)
    {
        try
        {
            var existingIds = pr.CheckRuns.Select(cr => cr.GitHubId).ToHashSet();
            var incomingIds = (prData.CheckRuns ?? []).Select(cr => cr.Id).ToHashSet();

            foreach (var checkRunData in prData.CheckRuns ?? [])
            {
                if (!existingIds.Contains(checkRunData.Id))
                {
                    context.CheckRuns.Add(new CheckRun
                    {
                        PullRequestId = pr.Id,
                        GitHubId = checkRunData.Id,
                        Name = checkRunData.Name,
                        Status = checkRunData.Status,
                        Conclusion = checkRunData.Conclusion,
                        Url = checkRunData.Url,
                        StartedAt = checkRunData.StartedAt,
                        CompletedAt = checkRunData.CompletedAt,
                        LastSyncedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    var existingCheckRun = pr.CheckRuns.First(cr => cr.GitHubId == checkRunData.Id);
                    existingCheckRun.Name = checkRunData.Name;
                    existingCheckRun.Status = checkRunData.Status;
                    existingCheckRun.Conclusion = checkRunData.Conclusion;
                    existingCheckRun.Url = checkRunData.Url;
                    existingCheckRun.StartedAt = checkRunData.StartedAt;
                    existingCheckRun.CompletedAt = checkRunData.CompletedAt;
                    existingCheckRun.LastSyncedAt = DateTime.UtcNow;
                }
            }

            foreach (var checkRun in pr.CheckRuns.Where(cr => !incomingIds.Contains(cr.GitHubId)))
            {
                context.CheckRuns.Remove(checkRun);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error syncing check runs for PR {PrId}", prData.Id);
        }
    }

    private async Task SyncFileDiffsAsync(PullRequest pr, GitHubPRData prData, GitHubConfig config)
    {
        try
        {
            var fileDiffs = await gitHubService.GetFileDiffsAsync(config.Organization, prData.Repository, prData.Id, config);
            var existingFileDiffs = await context.FileDiffs
                .Where(f => f.PullRequestId == pr.Id)
                .ToListAsync();

            context.FileDiffs.RemoveRange(existingFileDiffs);

            foreach (var fileDiff in fileDiffs)
            {
                var language = languageDetectionService.DetectLanguage(fileDiff.Path);
                context.FileDiffs.Add(new FileDiff
                {
                    PullRequestId = pr.Id,
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching file diffs for PR {PrId}", prData.Id);
        }
    }

    private async Task SyncMergeReadinessAsync(PullRequest pr)
    {
        try
        {
            await repositoryRuleService.CalculateMergeReadinessAsync(pr);
            
            if (pr.Status != "Merged" && pr.Status != "Closed")
            {
                var reviewData = pr.Reviews.Select(r => new GitHubReviewData(
                    r.GitHubId,
                    r.Reviewer,
                    r.ReviewerAvatar,
                    r.State,
                    r.SubmittedAt
                )).ToList();
                pr.Status = statusService.DeterminePrStatus(pr.Status, pr.IsMerged,pr.Draft, pr.IsMergeReady, reviewData);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating merge readiness for PR {PrId}", pr.GitHubId);
        }
    }

    private async Task CleanupOldPRsAsync(List<GitHubPRData> currentPRs)
    {
        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null) return;

        var currentIds = currentPRs.Select(pr => pr.Id).ToList();
        var oldPRs = await context.PullRequests
            .Where(pr => !currentIds.Contains(pr.GitHubId) && !pr.IsMerged && pr.Status != "Closed")
            .ToListAsync();

        if (oldPRs.Count == 0) return;

        const int batchSize = 15;
        for (var i = 0; i < oldPRs.Count; i += batchSize)
        {
            var batch = oldPRs.Skip(i).Take(batchSize).ToList();

            foreach (var pr in batch)
            {
                var statusData = await gitHubService.GetPullRequestStatusAsync(
                    config.Organization,
                    pr.Repository,
                    pr.GitHubId,
                    config
                );

                if (statusData == null)
                {
                    logger.LogWarning("Could not fetch status for PR {Repository}#{Number} - skipping", pr.Repository, pr.GitHubId);
                    continue;
                }

                if (config.DeleteOldPRs)
                {
                    context.PullRequests.Remove(pr);
                    logger.LogInformation("Deleted PR {Repository}#{Number}", pr.Repository, pr.GitHubId);
                }
                else
                {
                    if (statusData.IsMerged)
                    {
                        pr.IsMerged = true;
                        pr.Status = "Merged";
                        pr.MergedAt = statusData.MergedAt;
                        logger.LogInformation("Marked PR {Repository}#{Number} as merged at {MergedAt}",
                            pr.Repository, pr.GitHubId, statusData.MergedAt);
                    }
                    else if (statusData.IsClosed)
                    {
                        pr.IsMerged = false;
                        pr.MergedAt = null;
                        pr.Status = "Closed";
                        logger.LogInformation("Marked PR {Repository}#{Number} as closed (not merged)",
                            pr.Repository, pr.GitHubId);
                    }
                }
            }

            await context.SaveChangesAsync();

            if (i + batchSize < oldPRs.Count)
            {
                await Task.Delay(100);
            }
        }
    }

    public async Task<Dictionary<string, List<PullRequest>>> GetCachedPullRequestsAsync()
    {
        var pullRequests = await context.PullRequests
            .Include(pr => pr.Reviews)
            .Include(pr => pr.ReviewThreads)
            .OrderByDescending(pr => pr.UpdatedAt)
            .ToListAsync();

        return new Dictionary<string, List<PullRequest>>
        {
            ["ReadyToMerge"] = pullRequests.Where(pr => pr.Status == "ReadyToMerge").ToList(),
            ["AwaitingReview"] = pullRequests.Where(pr => pr.Status == "AwaitingReview").ToList(),
            ["Approved"] = pullRequests.Where(pr => pr.Status == "Approved").ToList(),
            ["Reviewed"] = pullRequests.Where(pr => pr.Status == "Reviewed").ToList(),
            ["ChangesRequested"] = pullRequests.Where(pr => pr.Status == "ChangesRequested").ToList(),
            ["Draft"] = pullRequests.Where(pr => pr.Status == "Draft").ToList()
        };
    }

    public async Task<PRStats> GetPullRequestStatsAsync()
    {
        var pullRequests = await context.PullRequests
            .Include(pullRequest => pullRequest.ReviewThreads)
            .ToListAsync();

        var totalThreads = pullRequests.Sum(pr => pr.ReviewThreads.Count);
        var resolvedThreads = pullRequests.Sum(pr => pr.ReviewThreads.Count(rt => rt.IsResolved));
        var pendingThreads = pullRequests.Sum(pr => pr.ReviewThreads.Count(rt => !rt.IsResolved));

        return new PRStats(
            TotalOpen: pullRequests.Count(pr=>pr.Status != "Closed" && pr.Status != "Merged"),
            Draft: pullRequests.Count(pr => pr.Status == "Draft"),
            Approved: pullRequests.Count(pr => pr.Status == "Approved"),
            ReadyToMerge: pullRequests.Count(pr => pr.Status == "ReadyToMerge"),
            AwaitingReview: pullRequests.Count(pr => pr.Status == "AwaitingReview"),
            TotalComments: totalThreads,
            ResolvedComments: resolvedThreads,
            PendingComments: pendingThreads
        );
    }

    public async Task<GitHubConfig?> GetConfigAsync()
    {
        return await context.GitHubConfigs.FirstOrDefaultAsync();
    }

    public async Task SaveConfigAsync(string organization, string? token, int refreshIntervalMinutes, string? appId = "", string? privateKey = "", string? installationId = "", bool useGitHubApp = false, bool deleteOldPRs = false)
    {
        var config = await context.GitHubConfigs.FirstOrDefaultAsync();

        if (config == null)
        {
            config = new GitHubConfig
            {
                Organization = organization,
                RefreshIntervalMinutes = refreshIntervalMinutes,
                AppId = appId ?? string.Empty,
                PrivateKey = privateKey ?? string.Empty,
                InstallationId = installationId ?? string.Empty,
                UseGitHubApp = useGitHubApp,
                DeleteOldPRs = deleteOldPRs
            };
            context.GitHubConfigs.Add(config);
        }
        else
        {
            config.Organization = organization;
            config.RefreshIntervalMinutes = refreshIntervalMinutes;
            config.AppId = appId ?? string.Empty;
            config.PrivateKey = privateKey ?? string.Empty;
            config.InstallationId = installationId ?? string.Empty;
            config.UseGitHubApp = useGitHubApp;
            config.DeleteOldPRs = deleteOldPRs;
        }

        await context.SaveChangesAsync();
    }

    public async Task UpdateLastRefreshAsync()
    {
        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config != null)
        {
            config.LastRefresh = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }
}
