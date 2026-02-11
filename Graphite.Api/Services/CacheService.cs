using Graphite.Domain.Data;
using Graphite.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Services;

public class CacheService(
    AppDbContext context,
    IGitHubService gitHubService,
    ILanguageDetectionService languageDetectionService,
    ILogger<CacheService> logger)
    : ICacheService
{
    public async Task RefreshPullRequestsAsync(GitHubConfig config)
    {
        var prDataList = await gitHubService.GetOpenPullRequestsAsync(config.Organization, config);

        foreach (var prData in prDataList)
        {
            var existingPR = await context.PullRequests
                .Include(pr => pr.Reviews)
                .Include(pr => pr.ReviewThreads)
                .Include(pr => pr.Comments)
                .Include(pr => pr.CheckRuns)
                .FirstOrDefaultAsync(pr => pr.GitHubId == prData.Id);

            if (existingPR != null)
            {
                existingPR.Title = prData.Title;
                existingPR.Repository = prData.Repository;
                existingPR.Author = prData.Author;
                existingPR.AuthorAvatar = prData.AuthorAvatar;
                existingPR.Status = prData.Status;
                existingPR.Draft = prData.Draft;
                existingPR.Additions = prData.Additions;
                existingPR.Deletions = prData.Deletions;
                existingPR.ChangedFiles = prData.ChangedFiles;
                existingPR.UpdatedAt = prData.UpdatedAt;
                existingPR.LastSyncedAt = DateTime.UtcNow;
                existingPR.Description = prData.Description;
                existingPR.SourceBranch = prData.SourceBranch;
                existingPR.TargetBranch = prData.TargetBranch;
                existingPR.MergeableState = prData.MergeableState;
                existingPR.ChecksStatus = prData.ChecksStatus;
            }
            else
            {
                existingPR = new PullRequest
                {
                    GitHubId = prData.Id,
                    Title = prData.Title,
                    Repository = prData.Repository,
                    Author = prData.Author,
                    AuthorAvatar = prData.AuthorAvatar,
                    Status = prData.Status,
                    Draft = prData.Draft,
                    Url = prData.Url,
                    Additions = prData.Additions,
                    Deletions = prData.Deletions,
                    ChangedFiles = prData.ChangedFiles,
                    CreatedAt = prData.CreatedAt,
                    UpdatedAt = prData.UpdatedAt,
                    LastSyncedAt = DateTime.UtcNow,
                    Description = prData.Description,
                    SourceBranch = prData.SourceBranch,
                    TargetBranch = prData.TargetBranch,
                    MergeableState = prData.MergeableState,
                    ChecksStatus = prData.ChecksStatus
                };
                context.PullRequests.Add(existingPR);
                await context.SaveChangesAsync();
            }

            var existingReviewIds = existingPR.Reviews.Select(r => r.GitHubId).ToHashSet();
            var incomingReviewIds = (prData.Reviews ?? []).Select(r => r.GitHubId).ToHashSet();

            foreach (var reviewId in incomingReviewIds.Except(existingReviewIds))
            {
                var reviewData = prData.Reviews!.First(r => r.GitHubId == reviewId);
                context.Reviews.Add(new Review
                {
                    PullRequestId = existingPR.Id,
                    GitHubId = reviewData.GitHubId,
                    Reviewer = reviewData.Reviewer,
                    ReviewerAvatar = reviewData.ReviewerAvatar,
                    State = reviewData.State,
                    SubmittedAt = reviewData.SubmittedAt
                });
            }

            foreach (var reviewId in existingReviewIds.Except(incomingReviewIds))
            {
                var review = existingPR.Reviews.First(r => r.GitHubId == reviewId);
                context.Reviews.Remove(review);
            }

            // Fetch and sync individual comments
            try
            {
                var comments = await gitHubService.GetCommentsAsync(config.Organization, prData.Repository, prData.Id, config);
                var existingCommentIds = existingPR.Comments.Select(c => c.GitHubId).ToHashSet();
                var reviewThreadMap = existingPR.ReviewThreads.ToDictionary(rt => rt.GitHubId, rt => rt.Id);

                foreach (var comment in comments)
                {
                    var reviewThread = existingPR.ReviewThreads.FirstOrDefault(rt => rt.GitHubId == comment.ReviewThreadId);
                    
                    if (!existingCommentIds.Contains(comment.GitHubId))
                    {
                        context.Comments.Add(new Comment
                        {
                            PullRequestId = existingPR.Id,
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
                        var existingComment = existingPR.Comments.First(c => c.GitHubId == comment.GitHubId);
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

                // Remove comments that are no longer in the response
                var incomingCommentIds = comments.Select(c => c.GitHubId).ToHashSet();
                foreach (var comment in existingPR.Comments.Where(c => !incomingCommentIds.Contains(c.GitHubId)))
                {
                    context.Comments.Remove(comment);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching comments for PR {Organization}/{Repository}#{PullRequestNumber}", config.Organization, prData.Repository, prData.Id);
            }

            var existingThreadIds = existingPR.ReviewThreads.Select(rt => rt.GitHubId).ToHashSet();
            var incomingThreadIds = (prData.ReviewThreads ?? []).Select(rt => rt.GitHubId).ToHashSet();

            foreach (var thread in prData.ReviewThreads ?? [])
            {
                if (!existingThreadIds.Contains(thread.GitHubId))
                {
                    context.ReviewThreads.Add(new ReviewThread
                    {
                        PullRequestId = existingPR.Id,
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
                    var existingThread = existingPR.ReviewThreads.First(rt => rt.GitHubId == thread.GitHubId);
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

            foreach (var threadId in existingThreadIds.Except(incomingThreadIds))
            {
                var thread = existingPR.ReviewThreads.First(rt => rt.GitHubId == threadId);
                context.ReviewThreads.Remove(thread);
            }

            // Sync check runs
            try
            {
                var existingCheckRunIds = existingPR.CheckRuns.Select(cr => cr.GitHubId).ToHashSet();
                var incomingCheckRunIds = (prData.CheckRuns ?? []).Select(cr => cr.Id).ToHashSet();

                foreach (var checkRunData in prData.CheckRuns ?? [])
                {
                    if (!existingCheckRunIds.Contains(checkRunData.Id))
                    {
                        context.CheckRuns.Add(new CheckRun
                        {
                            PullRequestId = existingPR.Id,
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
                        var existingCheckRun = existingPR.CheckRuns.First(cr => cr.GitHubId == checkRunData.Id);
                        existingCheckRun.Name = checkRunData.Name;
                        existingCheckRun.Status = checkRunData.Status;
                        existingCheckRun.Conclusion = checkRunData.Conclusion;
                        existingCheckRun.Url = checkRunData.Url;
                        existingCheckRun.StartedAt = checkRunData.StartedAt;
                        existingCheckRun.CompletedAt = checkRunData.CompletedAt;
                        existingCheckRun.LastSyncedAt = DateTime.UtcNow;
                    }
                }

                // Remove check runs that are no longer in response
                foreach (var checkRun in existingPR.CheckRuns.Where(cr => !incomingCheckRunIds.Contains(cr.GitHubId)))
                {
                    context.CheckRuns.Remove(checkRun);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error syncing check runs for PR {PrId}", prData.Id);
            }

            // Fetch and store file diffs
            try
            {
                var fileDiffs = await gitHubService.GetFileDiffsAsync(config.Organization, prData.Repository, prData.Id, config);
                var existingFileDiffs = await context.FileDiffs
                    .Where(f => f.PullRequestId == existingPR.Id)
                    .ToListAsync();

                // Remove old file diffs
                context.FileDiffs.RemoveRange(existingFileDiffs);

                // Add new file diffs with language detection
                foreach (var fileDiff in fileDiffs)
                {
                    var language = languageDetectionService.DetectLanguage(fileDiff.Path);
                    context.FileDiffs.Add(new FileDiff
                    {
                        PullRequestId = existingPR.Id,
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

        await context.SaveChangesAsync();
        await CleanupOldPRsAsync(prDataList);
    }

    private async Task CleanupOldPRsAsync(List<GitHubPRData> currentPRs)
    {
        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null) return;

        var currentIds = currentPRs.Select(pr => pr.Id).ToList();
        var oldPRs = await context.PullRequests
            .Where(pr => !currentIds.Contains(pr.GitHubId) && !pr.IsMerged && pr.Status != "Closed")
            .ToListAsync();

        if (!oldPRs.Any()) return;

        const int batchSize = 15;
        for (int i = 0; i < oldPRs.Count; i += batchSize)
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

    public async Task SaveConfigAsync(string organization, string token, int refreshIntervalMinutes, string appId = "", string privateKey = "", string installationId = "", bool useGitHubApp = false, bool deleteOldPRs = false)
    {
        var config = await context.GitHubConfigs.FirstOrDefaultAsync();

        if (config == null)
        {
            config = new GitHubConfig
            {
                Organization = organization,
                PersonalAccessToken = token,
                RefreshIntervalMinutes = refreshIntervalMinutes,
                AppId = appId,
                PrivateKey = privateKey,
                InstallationId = installationId,
                UseGitHubApp = useGitHubApp,
                DeleteOldPRs = deleteOldPRs
            };
            context.GitHubConfigs.Add(config);
        }
        else
        {
            config.Organization = organization;
            config.PersonalAccessToken = token;
            config.RefreshIntervalMinutes = refreshIntervalMinutes;
            config.AppId = appId;
            config.PrivateKey = privateKey;
            config.InstallationId = installationId;
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