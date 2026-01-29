using Graphite.Domain.Data;
using Graphite.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Services;

public class CacheService : ICacheService
{
    private readonly AppDbContext _context;
    private readonly IGitHubService _gitHubService;
    private readonly ILogger<CacheService> _logger;

    public CacheService(AppDbContext context, IGitHubService gitHubService, ILogger<CacheService> logger)
    {
        _context = context;
        _gitHubService = gitHubService;
        _logger = logger;
    }

    public async Task RefreshPullRequestsAsync(GitHubConfig config)
    {
        var prDataList = await _gitHubService.GetOpenPullRequestsAsync(config.Organization, config);

        foreach (var prData in prDataList)
        {
            var existingPR = await _context.PullRequests
                .Include(pr => pr.Reviews)
                .Include(pr => pr.ReviewThreads)
                .Include(pr => pr.Comments)
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
                    MergeableState = prData.MergeableState
                };
                _context.PullRequests.Add(existingPR);
                await _context.SaveChangesAsync();
            }

            var existingReviewers = existingPR.Reviews.Select(r => r.Reviewer).ToHashSet();
            var incomingReviewers = (prData.Reviews ?? []).Select(r => r.Reviewer).ToHashSet();

            foreach (var reviewer in incomingReviewers.Except(existingReviewers))
            {
                var reviewData = prData.Reviews!.First(r => r.Reviewer == reviewer);
                _context.Reviews.Add(new Review
                {
                    PullRequestId = existingPR.Id,
                    Reviewer = reviewData.Reviewer,
                    ReviewerAvatar = reviewData.ReviewerAvatar,
                    State = reviewData.State,
                    SubmittedAt = reviewData.SubmittedAt
                });
            }

            foreach (var reviewer in existingReviewers.Except(incomingReviewers))
            {
                var review = existingPR.Reviews.First(r => r.Reviewer == reviewer);
                _context.Reviews.Remove(review);
            }

            // Fetch and sync individual comments
            try
            {
                var comments = await _gitHubService.GetCommentsAsync(config.Organization, prData.Repository, prData.Id, config);
                var existingCommentIds = existingPR.Comments.Select(c => c.GitHubId).ToHashSet();
                var reviewThreadMap = existingPR.ReviewThreads.ToDictionary(rt => rt.GitHubId, rt => rt.Id);

                foreach (var comment in comments)
                {
                    var reviewThread = existingPR.ReviewThreads.FirstOrDefault(rt => rt.GitHubId == comment.ReviewThreadId);
                    
                    if (!existingCommentIds.Contains(comment.GitHubId))
                    {
                        _context.Comments.Add(new Comment
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
                    _context.Comments.Remove(comment);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comments for PR {Organization}/{Repository}#{PullRequestNumber}", config.Organization, prData.Repository, prData.Id);
            }

            var existingThreadIds = existingPR.ReviewThreads.Select(rt => rt.GitHubId).ToHashSet();
            var incomingThreadIds = (prData.ReviewThreads ?? []).Select(rt => rt.GitHubId).ToHashSet();

            foreach (var thread in prData.ReviewThreads ?? [])
            {
                if (!existingThreadIds.Contains(thread.GitHubId))
                {
                    _context.ReviewThreads.Add(new ReviewThread
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
                _context.ReviewThreads.Remove(thread);
            }

            // Fetch and store file diffs
            try
            {
                var fileDiffs = await _gitHubService.GetFileDiffsAsync(config.Organization, prData.Repository, prData.Id, config);
                var existingFileDiffs = await _context.FileDiffs
                    .Where(f => f.PullRequestId == existingPR.Id)
                    .ToListAsync();

                // Remove old file diffs
                _context.FileDiffs.RemoveRange(existingFileDiffs);

                // Add new file diffs with language detection
                foreach (var fileDiff in fileDiffs)
                {
                    var language = DetectLanguage(fileDiff.Path);
                    _context.FileDiffs.Add(new FileDiff
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
                // Log error but continue with other PRs
                Console.WriteLine($"Error fetching file diffs for PR {prData.Id}: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync(); 
        await CleanupOldPRsAsync(prDataList);
    }

    private string DetectLanguage(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".cs" => "csharp",
            ".vue" => "vue",
            ".ts" => "typescript",
            ".js" => "javascript",
            ".tsx" => "typescript",
            ".jsx" => "javascript",
            ".css" => "css",
            ".scss" => "scss",
            ".sass" => "sass",
            ".less" => "less",
            ".html" => "markup",
            ".xml" => "markup",
            ".json" => "json",
            ".yml" => "yaml",
            ".yaml" => "yaml",
            ".md" => "markdown",
            ".sql" => "sql",
            ".py" => "python",
            ".rb" => "ruby",
            ".java" => "java",
            ".go" => "go",
            ".rs" => "rust",
            ".cpp" => "cpp",
            ".c" => "c",
            ".h" => "c",
            ".hpp" => "cpp",
            ".sh" => "bash",
            ".ps1" => "powershell",
            ".dockerfile" => "docker",
            _ => path.EndsWith("Dockerfile") ? "docker" : "text"
        };
    }

    private async Task CleanupOldPRsAsync(List<GitHubPRData> currentPRs)
    {
        var currentIds = currentPRs.Select(pr => pr.Id).ToList();
        var oldPRs = await _context.PullRequests
            .Where(pr => !currentIds.Contains(pr.GitHubId))
            .ToListAsync();

        _context.PullRequests.RemoveRange(oldPRs);
        await _context.SaveChangesAsync();
    }

    public async Task<Dictionary<string, List<PullRequest>>> GetCachedPullRequestsAsync()
    {
        var pullRequests = await _context.PullRequests
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
        var pullRequests = await _context.PullRequests
            .Include(pullRequest => pullRequest.ReviewThreads)
            .ToListAsync();

        var totalThreads = pullRequests.Sum(pr => pr.ReviewThreads.Count);
        var resolvedThreads = pullRequests.Sum(pr => pr.ReviewThreads.Count(rt => rt.IsResolved));
        var pendingThreads = pullRequests.Sum(pr => pr.ReviewThreads.Count(rt => !rt.IsResolved));

        return new PRStats(
            TotalOpen: pullRequests.Count,
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
        return await _context.GitHubConfigs.FirstOrDefaultAsync();
    }

    public async Task SaveConfigAsync(string organization, string token, int refreshIntervalMinutes, string appId = "", string privateKey = "", string installationId = "", bool useGitHubApp = false)
    {
        var config = await _context.GitHubConfigs.FirstOrDefaultAsync();

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
                UseGitHubApp = useGitHubApp
            };
            _context.GitHubConfigs.Add(config);
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
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateLastRefreshAsync()
    {
        var config = await _context.GitHubConfigs.FirstOrDefaultAsync();
        if (config != null)
        {
            config.LastRefresh = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}