using Graphite.Domain.Data;
using Graphite.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Services;

public class CacheService : ICacheService
{
    private readonly AppDbContext _context;
    private readonly IGitHubService _gitHubService;

    public CacheService(AppDbContext context, IGitHubService gitHubService)
    {
        _context = context;
        _gitHubService = gitHubService;
    }

    public async Task RefreshPullRequestsAsync(string organization, string token)
    {
        var prDataList = await _gitHubService.GetOpenPullRequestsAsync(organization, token);

        foreach (var prData in prDataList)
        {
            var existingPR = await _context.PullRequests
                .Include(pr => pr.Reviews)
                .Include(pr => pr.Comment)
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
                    LastSyncedAt = DateTime.UtcNow
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

            var commentData = prData.Comments;

            if (existingPR.Comment == null)
            {
                if (commentData != null)
                {
                    existingPR.Comment = new Comment
                    {
                        PullRequestId = existingPR.Id,
                        Count = commentData.Count,
                        ResolvedCount = commentData.ResolvedCount,
                        PendingCount = commentData.PendingCount,
                        LastUpdated = commentData.LastUpdated
                    };
                }
            }
            else
            {
                existingPR.Comment.Count = commentData?.Count ?? 0;
                existingPR.Comment.ResolvedCount = commentData?.ResolvedCount ?? 0;
                existingPR.Comment.PendingCount = commentData?.PendingCount ?? 0;
                existingPR.Comment.LastUpdated = commentData?.LastUpdated;
            }
        }

        await _context.SaveChangesAsync(); 
        await CleanupOldPRsAsync(prDataList);
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
            .Include(pr => pr.Comment)
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
        var pullRequests = await _context.PullRequests.Include(pr => pr.Comment).ToListAsync();

        var totalComments = pullRequests.Sum(pr => pr.Comment?.Count ?? 0);
        var resolvedComments = pullRequests.Sum(pr => pr.Comment?.ResolvedCount ?? 0);
        var pendingComments = pullRequests.Sum(pr => pr.Comment?.PendingCount ?? 0);

        return new PRStats(
            TotalOpen: pullRequests.Count,
            Draft: pullRequests.Count(pr => pr.Status == "Draft"),
            Approved: pullRequests.Count(pr => pr.Status == "Approved"),
            AwaitingReview: pullRequests.Count(pr => pr.Status == "AwaitingReview"),
            TotalComments: totalComments,
            ResolvedComments: resolvedComments,
            PendingComments: pendingComments
        );
    }

    public async Task<GitHubConfig?> GetConfigAsync()
    {
        return await _context.GitHubConfigs.FirstOrDefaultAsync();
    }

    public async Task SaveConfigAsync(string organization, string token, int refreshIntervalMinutes)
    {
        var config = await _context.GitHubConfigs.FirstOrDefaultAsync();

        if (config == null)
        {
            config = new GitHubConfig
            {
                Organization = organization,
                PersonalAccessToken = token,
                RefreshIntervalMinutes = refreshIntervalMinutes
            };
            _context.GitHubConfigs.Add(config);
        }
        else
        {
            config.Organization = organization;
            config.PersonalAccessToken = token;
            config.RefreshIntervalMinutes = refreshIntervalMinutes;
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