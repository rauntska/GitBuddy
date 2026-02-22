using Graphite.Api.DTOs;
using Graphite.Api.Extensions;
using Graphite.Api.Services;
using Graphite.Domain.Data;
using Graphite.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Graphite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PullRequestsController(
    ICacheService cacheService, 
    AppDbContext context, 
    IGitHubService gitHubService,
    IGitHubGraphQLService gitHubGraphQLService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var groupedPRs = await cacheService.GetCachedPullRequestsAsync();
        
        var groupedPRsDtos = new Dictionary<string, object>();
        foreach (var group in groupedPRs)
        {
            groupedPRsDtos[group.Key] = group.Value.ToDto();
        }
        
        return Ok(groupedPRsDtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var pr = await context.PullRequests
            .Include(p => p.Reviews)
            .Include(p => p.ReviewThreads)
            .Include(p => p.CheckRuns)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        var files = await context.FileDiffs
            .Where(f => f.PullRequestId == id)
            .ToListAsync();

        var comments = await context.Comments
            .Where(c => c.PullRequestId == id)
            .ToListAsync();

        int? userId = null;
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        PendingReviewDto? pendingReviewDto = null;
        User? user = null;
        GitHubConfig? config = null;
        List<UserFileViewedState>? viewedStates = null;
        
        if (userId.HasValue)
        {
            user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            viewedStates = await context.UserFileViewedStates
                .Where(uvs => uvs.UserId == userId.Value && files.Select(f => f.Id).Contains(uvs.FileDiffId))
                .ToListAsync();
                
            if (user != null && !string.IsNullOrEmpty(user.AccessToken))
            {
                config = await context.GitHubConfigs.FirstOrDefaultAsync();
                if (config != null)
                {
                    try
                    {
                        var pendingReview = await gitHubService.GetPendingReviewAsync(
                            config.Organization,
                            pr.Repository,
                            pr.GitHubId,
                            user.Username,
                            user.AccessToken
                        );

                        if (pendingReview != null)
                        {
                            pendingReviewDto = new PendingReviewDto(
                                pendingReview.GitHubId,
                                pendingReview.State,
                                pendingReview.Comments.Select(c => new PendingReviewCommentDto(
                                    c.GitHubId,
                                    c.Path ?? string.Empty,
                                    c.Line,
                                    c.Body,
                                    c.Author,
                                    c.AuthorAvatar,
                                    c.CreatedAt,
                                    c.UpdatedAt,
                                    c.ThreadId
                                )).ToList()
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching pending review: {ex.Message}");
                    }
                }
            }
        }

        return Ok(pr.ToDetailDto(files, comments, viewedStates, pendingReviewDto, pr.RequiredApprovingReviews, pr.CurrentApprovingReviews, pr.HasUnresolvedThreads, pr.IsMergeReady, pr.MergeBlockReason));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await cacheService.GetPullRequestStatsAsync();
        return Ok(stats);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return Unauthorized(new { message = "User not found" });
        }

        var username = user.Username;
        
        var allPRs = await context.PullRequests
            .Include(pr => pr.Reviews)
            .Where(pr => !pr.IsMerged && pr.Status != "Closed" && pr.Status != "Merged")
            .ToListAsync();

        var userReviewedPRIds = allPRs
            .Where(pr => pr.Reviews.Any(r => r.Reviewer == username && 
                (r.State == "APPROVED" || r.State == "CHANGES_REQUESTED" || r.State == "COMMENTED")))
            .Select(pr => pr.Id)
            .ToHashSet();

        int unreadCount = 0;

        foreach (var pr in allPRs)
        {
            var isAuthor = pr.Author == username;
            var hasReviewed = userReviewedPRIds.Contains(pr.Id);
            var isReviewer = pr.Reviews.Any(r => r.Reviewer == username);
            var needsMoreReviewers = pr.RequiredApprovingReviews.HasValue && 
                                      pr.CurrentApprovingReviews < pr.RequiredApprovingReviews.Value;

            if (isAuthor && pr.Status == "ChangesRequested")
            {
                unreadCount++;
            }
            else if (!isAuthor && !hasReviewed)
            {
                if (isReviewer || needsMoreReviewers)
                {
                    unreadCount++;
                }
            }
        }

        return Ok(new { count = unreadCount });
    }

    [HttpGet("merged")]
    public async Task<IActionResult> GetMergedPRs([FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        var mergedPRs = await context.PullRequests
            .Include(pr => pr.Reviews)
            .Include(pr => pr.ReviewThreads)
            .Where(pr => pr.IsMerged)
            .OrderByDescending(pr => pr.MergedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var totalCount = await context.PullRequests.CountAsync(pr => pr.IsMerged);

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Has-More", ((skip + take) < totalCount).ToString().ToLower());

        return Ok(mergedPRs.ToDto());
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> Refresh()
    {
        var config = await cacheService.GetConfigAsync();

        if (config == null || string.IsNullOrEmpty(config.Organization))
        {
            return BadRequest("GitHub configuration not found. Please configure settings first.");
        }

        if (!config.UseGitHubApp && string.IsNullOrEmpty(config.PersonalAccessToken))
        {
            return BadRequest("GitHub configuration not found. Please configure settings first.");
        }

        if (config.UseGitHubApp && (string.IsNullOrEmpty(config.AppId) || string.IsNullOrEmpty(config.PrivateKey) || string.IsNullOrEmpty(config.InstallationId)))
        {
            return BadRequest("GitHub App configuration is incomplete. Please configure App ID, Private Key, and Installation ID.");
        }

        try
        {
            await cacheService.RefreshPullRequestsAsync(config);
            await cacheService.UpdateLastRefreshAsync();
            return Ok(new { message = "Pull requests refreshed successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/files/{*filePath}")]
    public async Task<IActionResult> GetFileDiff(int id, string filePath)
    {
        var fileDiff = await context.FileDiffs
            .FirstOrDefaultAsync(f => f.PullRequestId == id && f.Path == filePath);

        if (fileDiff == null)
        {
            return NotFound(new { message = "File not found" });
        }

        return Ok(fileDiff.ToDto());
    }

    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetComments(int id)
    {
        var comments = await context.Comments
            .Where(c => c.PullRequestId == id)
            .ToListAsync();

        return Ok(comments.Select(c => c.ToDto()));
    }

    [HttpPost("{id}/comments")]
    [Authorize]
    public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        var pr = await context.PullRequests
            .Include(p => p.ReviewThreads)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        try
        {
            if (!string.IsNullOrEmpty(request.Path) && request.Line.HasValue)
            {
                var pendingComment = await gitHubService.AddPendingReviewCommentAsync(
                    config.Organization,
                    pr.Repository,
                    pr.GitHubId,
                    request.Body,
                    request.Path,
                    request.Line.Value,
                    config,
                    user.AccessToken
                );

                var pendingReview = await context.PendingReviews
                    .FirstOrDefaultAsync(pr => pr.PullRequestId == id && pr.UserId == userId);

                if (pendingReview == null && !string.IsNullOrEmpty(pendingComment.ReviewId))
                {
                    pendingReview = new PendingReview
                    {
                        PullRequestId = id,
                        UserId = userId,
                        GitHubReviewId = pendingComment.ReviewId,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.PendingReviews.Add(pendingReview);
                    await context.SaveChangesAsync();
                }

                if (pendingReview != null)
                {
                    var dbPendingComment = new PendingComment
                    {
                        PendingReviewId = pendingReview.Id,
                        Body = pendingComment.Body,
                        Path = pendingComment.Path ?? request.Path,
                        Line = pendingComment.Line ?? request.Line.Value,
                        CreatedAt = pendingComment.CreatedAt
                    };
                    context.PendingComments.Add(dbPendingComment);
                    await context.SaveChangesAsync();

                    return Ok(new
                    {
                        isPending = true,
                        id = dbPendingComment.Id,
                        gitHubId = pendingComment.GitHubId,
                        pendingReviewId = pendingComment.ReviewId,
                        reviewThreadId = pendingComment.ThreadId,
                        path = pendingComment.Path,
                        line = pendingComment.Line,
                        body = pendingComment.Body,
                        author = pendingComment.Author,
                        authorAvatar = pendingComment.AuthorAvatar,
                        createdAt = pendingComment.CreatedAt,
                        updatedAt = pendingComment.UpdatedAt
                    });
                }

                return Ok(new
                {
                    isPending = true,
                    id = 0,
                    gitHubId = pendingComment.GitHubId,
                    pendingReviewId = pendingComment.ReviewId,
                    reviewThreadId = pendingComment.ThreadId,
                    path = pendingComment.Path,
                    line = pendingComment.Line,
                    body = pendingComment.Body,
                    author = pendingComment.Author,
                    authorAvatar = pendingComment.AuthorAvatar,
                    createdAt = pendingComment.CreatedAt,
                    updatedAt = pendingComment.UpdatedAt
                });
            }

            var comment = await gitHubService.AddPullRequestCommentAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                request.Body,
                null,
                null,
                config,
                user.AccessToken
            );

            return Ok(new CommentDto(
                0,
                comment.GitHubId,
                null,
                comment.Author,
                comment.AuthorAvatar,
                comment.Body,
                comment.CreatedAt,
                comment.UpdatedAt,
                comment.Path,
                comment.Line,
                comment.IsOutdated,
                null,
                0,
                null,
                null
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to add comment", error = ex.Message });
        }
    }

    [HttpPost("{id}/comments/reply")]
    [Authorize]
    public async Task<IActionResult> AddCommentReply(int id, [FromBody] AddCommentReplyRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        var pr = await context.PullRequests
            .Include(p => p.ReviewThreads)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        try
        {
            var comment = await gitHubService.AddPullRequestReviewThreadReplyAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                request.ReviewThreadId,
                request.Body,
                config,
                user.AccessToken
            );

            // If the reply is part of a pending review, don't save to database
            // Instead return it with pending review info
            if (comment.IsPending)
            {
                return Ok(new
                {
                    isPending = true,
                    pendingReviewId = comment.PendingReviewId,
                    commentNodeId = comment.CommentNodeId,
                    reviewThreadId = request.ReviewThreadId,
                    author = comment.Author,
                    authorAvatar = comment.AuthorAvatar,
                    body = comment.Body,
                    createdAt = comment.CreatedAt,
                    updatedAt = comment.UpdatedAt
                });
            }

            var reviewThread = pr.ReviewThreads.FirstOrDefault(rt => rt.GitHubId == request.ReviewThreadId);
            if (reviewThread != null)
            {
                reviewThread.CommentCount++;
                reviewThread.UpdatedAt = comment.UpdatedAt;

                var dbComment = new Comment
                {
                    PullRequestId = id,
                    ReviewThreadId = reviewThread.Id,
                    GitHubId = comment.GitHubId,
                    Author = comment.Author,
                    AuthorAvatar = comment.AuthorAvatar,
                    Body = comment.Body,
                    Path = comment.Path,
                    Line = comment.Line,
                    IsOutdated = false,
                    CreatedAt = comment.CreatedAt,
                    UpdatedAt = comment.UpdatedAt
                };
                context.Comments.Add(dbComment);
                await context.SaveChangesAsync();

                return Ok(new CommentDto(
                    dbComment.Id,
                    dbComment.GitHubId,
                    dbComment.ReviewThreadId,
                    dbComment.Author,
                    dbComment.AuthorAvatar,
                    dbComment.Body,
                    dbComment.CreatedAt,
                    dbComment.UpdatedAt,
                    dbComment.Path,
                    dbComment.Line,
                    dbComment.IsOutdated,
                    dbComment.EditedAt,
                    dbComment.EditCount,
                    dbComment.ReplyToCommentId,
                    null
                ));
            }

            return Ok(new CommentDto(
                0,
                comment.GitHubId,
                null,
                comment.Author,
                comment.AuthorAvatar,
                comment.Body,
                comment.CreatedAt,
                comment.UpdatedAt,
                comment.Path,
                comment.Line,
                comment.IsOutdated,
                null,
                0,
                null,
                null
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to add reply", error = ex.Message });
        }
    }

    [HttpPost("{id}/threads/{threadId}/resolve")]
    [Authorize]
    public async Task<IActionResult> ResolveReviewThread(int id, string threadId, [FromBody] ResolveThreadRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        try
        {
            await gitHubService.ResolveReviewThreadAsync(
                config.Organization,
                pr.Repository,
                threadId,
                request.Resolved,
                config,
                user.AccessToken
            );

            return Ok(new { message = "Thread resolved successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to resolve thread", error = ex.Message });
        }
    }

    [HttpPost("{id}/threads/{threadId}/unresolve")]
    [Authorize]
    public async Task<IActionResult> UnresolveReviewThread(int id, string threadId, [FromBody] UnresolveThreadRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        try
        {
            await gitHubService.UnresolveReviewThreadAsync(
                config.Organization,
                pr.Repository,
                threadId,
                config,
                user.AccessToken
            );

            return Ok(new { message = "Thread unresolved successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to unresolve thread", error = ex.Message });
        }
    }

    [HttpPost("{id}/review")]
    [Authorize]
    public async Task<IActionResult> SubmitReview(int id, [FromBody] SubmitReviewRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        try
        {
            await gitHubService.SubmitPullRequestReviewAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                request.State,
                request.Body,
                config,
                user.AccessToken
            );

            return Ok(new { message = "Review submitted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to submit review to GitHub", error = ex.Message });
        }
    }

    [HttpPost("{id}/merge")]
    [Authorize]
    public async Task<IActionResult> MergePR(int id, [FromBody] MergePRRequest? request = null)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        if (pr.Draft)
        {
            return BadRequest(new { message = "Cannot merge a draft pull request. Please publish it first." });
        }

        if (pr.IsMerged)
        {
            return BadRequest(new { message = "Pull request has already been merged." });
        }

        if (pr.MergeableState == "CONFLICTING")
        {
            return BadRequest(new { message = "Pull request has merge conflicts that must be resolved before merging." });
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        var mergeMethod = request?.MergeMethod ?? "merge";

        try
        {
            await gitHubService.MergePullRequestAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                request?.CommitTitle,
                request?.CommitMessage,
                mergeMethod,
                user.AccessToken
            );

            pr.IsMerged = true;
            pr.MergedAt = DateTime.UtcNow;
            pr.Status = "Merged";
            await context.SaveChangesAsync();

            return Ok(new { message = "Pull request merged successfully", isMerged = true, mergedAt = pr.MergedAt });
        }
        catch (Octokit.ApiException ex)
        {
            var errorMessage = ex.StatusCode switch
            {
                System.Net.HttpStatusCode.Conflict => "Pull request has merge conflicts that must be resolved.",
                System.Net.HttpStatusCode.MethodNotAllowed => "Merge method not allowed. Check repository settings.",
                System.Net.HttpStatusCode.Forbidden => "You don't have permission to merge this pull request.",
                System.Net.HttpStatusCode.UnprocessableEntity => "Pull request cannot be merged. It may be closed or have failing checks.",
                _ => $"Failed to merge pull request: {ex.Message}"
            };
            return StatusCode((int)ex.StatusCode, new { message = errorMessage });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to merge pull request", error = ex.Message });
        }
    }

    [HttpGet("{id}/merge-options")]
    [Authorize]
    public async Task<IActionResult> GetMergeOptions(int id)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        try
        {
            var mergeOptions = await gitHubService.GetRepositoryMergeOptionsAsync(
                config.Organization,
                pr.Repository,
                user.AccessToken
            );

            return Ok(new
            {
                mergeCommitAllowed = mergeOptions.MergeCommitAllowed,
                squashMergeAllowed = mergeOptions.SquashMergeAllowed,
                rebaseMergeAllowed = mergeOptions.RebaseMergeAllowed,
                defaultMergeMethod = mergeOptions.DefaultMergeMethod,
                mergeableState = pr.MergeableState,
                isMerged = pr.IsMerged,
                isDraft = pr.Draft
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to get merge options", error = ex.Message });
        }
    }

    [HttpPost("{id}/publish")]
    [Authorize]
    public async Task<IActionResult> PublishDraftPR(int id)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        if (!pr.Draft)
        {
            return BadRequest(new { message = "Pull request is not a draft" });
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        try
        {
            await gitHubService.PublishDraftPullRequestAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                config,
                user.AccessToken
            );

            // Update local database
            pr.Draft = false;
            pr.Status = "AwaitingReview";
            await context.SaveChangesAsync();

            return Ok(new { message = "Draft PR published successfully", draft = false, status = "AwaitingReview" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to publish draft PR", error = ex.Message });
        }
    }

    [HttpPost("{id}/files/viewed")]
    [Authorize]
    public async Task<IActionResult> UpdateFileViewedState(int id, [FromBody] UpdateViewedStateRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        //get user accesTOken from db
        // Get user's token from database
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userIdFromClaim))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userIdFromClaim);
        
        
        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null) return NotFound();

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null) return BadRequest(new { message = "GitHub configuration not found" });

        var userAccessToken = user.AccessToken;

        try
        {
            if (request.Viewed)
            {
                await gitHubService.MarkFileAsViewedAsync(config.Organization, pr.Repository, (int)pr.GitHubId, request.Path, config, userAccessToken);
            }
            else
            {
                await gitHubService.UnmarkFileAsViewedAsync(config.Organization, pr.Repository, (int)pr.GitHubId, request.Path, config, userAccessToken);
            }

            // Also update local DB state
            var fileDiff = await context.FileDiffs.FirstOrDefaultAsync(f => f.PullRequestId == id && f.Path == request.Path);
            if (fileDiff != null)
            {
                // Get current user from JWT
                var userIdStr = User.FindFirst("UserId")?.Value;
                if (int.TryParse(userIdStr, out var userId))
                {
                    var viewedState = await context.UserFileViewedStates
                        .FirstOrDefaultAsync(vs => vs.FileDiffId == fileDiff.Id && vs.UserId == userId);

                    if (viewedState == null)
                    {
                        context.UserFileViewedStates.Add(new UserFileViewedState
                        {
                            FileDiffId = fileDiff.Id,
                            UserId = userId,
                            ViewedState = request.Viewed ? "VIEWED" : "UNVIEWED",
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        viewedState.ViewedState = request.Viewed ? "VIEWED" : "UNVIEWED";
                        viewedState.UpdatedAt = DateTime.UtcNow;
                    }

                    await context.SaveChangesAsync();
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to update viewed state on GitHub", error = ex.Message });
        }
    }

    [HttpGet("{id}/mentionable-users")]
    public async Task<IActionResult> GetMentionableUsers(int id)
    {
        var pr = await context.PullRequests
            .Include(p => p.Reviews)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        // Collect unique users from reviews, comments, and the PR author
        var users = new HashSet<string> { pr.Author };

        foreach (var review in pr.Reviews)
        {
            users.Add(review.Reviewer);
        }

        foreach (var comment in pr.Comments)
        {
            users.Add(comment.Author);
        }

        // Add reviewers from the PR if available
        if (!string.IsNullOrEmpty(pr.Reviews.FirstOrDefault()?.Reviewer))
        {
            foreach (var review in pr.Reviews)
            {
                users.Add(review.Reviewer);
            }
        }

        // Also query for user avatars
        var userAvatars = await context.Users
            .Where(u => users.Contains(u.Username))
            .Select(u => new { u.Username, u.AvatarUrl, u.Name })
            .ToDictionaryAsync(u => u.Username);

        var result = users.Select(username => new
        {
            Username = username,
            AvatarUrl = userAvatars.GetValueOrDefault(username)?.AvatarUrl,
            Name = userAvatars.GetValueOrDefault(username)?.Name
        }).ToList();

        return Ok(result);
    }

    [HttpPost("{id}/file-diffs/refresh-viewed-states")]
    [Authorize]
    public async Task<IActionResult> RefreshFileViewedStates(int id)
    {
        // Get current user from JWT
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        // Get PR details
        var pr = await context.PullRequests
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        // Get user's token from database
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        // Get config
        var config = await cacheService.GetConfigAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        try
        {
            // Fetch file diffs with user's viewed state from GitHub
            var fileDiffs = await gitHubService.GetFileDiffsAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                config,
                user.AccessToken // Use user's personal token
            );

            // Get existing file diffs from database
            var dbFileDiffs = await context.FileDiffs
                .Where(f => f.PullRequestId == id)
                .ToListAsync();

            // Update database with viewed states
            foreach (var fileDiff in fileDiffs)
            {
                var dbFileDiff = dbFileDiffs.FirstOrDefault(f => f.Path == fileDiff.Path);
                if (dbFileDiff != null)
                {
                    // Update or create viewed state
                    var existingState = await context.UserFileViewedStates
                        .FirstOrDefaultAsync(uvs => uvs.UserId == userId && uvs.FileDiffId == dbFileDiff.Id);

                    if (existingState != null)
                    {
                        existingState.ViewedState = fileDiff.ViewerViewedState;
                        existingState.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // Create new viewed state
                        context.UserFileViewedStates.Add(new UserFileViewedState
                        {
                            UserId = userId,
                            FileDiffId = dbFileDiff.Id,
                            ViewedState = fileDiff.ViewerViewedState,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await context.SaveChangesAsync();

            // Return updated file diffs with viewed state
            var viewedStates = await context.UserFileViewedStates
                .Where(uvs => uvs.UserId == userId && dbFileDiffs.Select(f => f.Id).Contains(uvs.FileDiffId))
                .ToListAsync();

            var result = dbFileDiffs.Select(file =>
            {
                var viewedState = viewedStates.FirstOrDefault(vs => vs.FileDiffId == file.Id);
                return file.ToDto(viewedState?.ViewedState, viewedState?.UpdatedAt);
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to fetch file viewed states", error = ex.Message });
        }
    }

    [HttpGet("{id}/files/content")]
    [Authorize]
    public async Task<IActionResult> GetFileContent(
        int id,
        [FromQuery] string path,
        [FromQuery] int? oldStartLine,
        [FromQuery] int? oldEndLine,
        [FromQuery] int? newStartLine,
        [FromQuery] int? newEndLine)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        try
        {
            var oldLines = new List<FileLineContent>();
            var newLines = new List<FileLineContent>();

            if (oldStartLine.HasValue && oldEndLine.HasValue && !string.IsNullOrEmpty(pr.TargetBranch))
            {
                var oldContent = await gitHubGraphQLService.GetFileContentAsync(
                    config.Organization,
                    pr.Repository,
                    pr.TargetBranch,
                    path,
                    user.AccessToken
                );

                if (oldContent != null)
                {
                    oldLines = ExtractLines(oldContent, oldStartLine.Value, oldEndLine.Value);
                }
            }

            if (newStartLine.HasValue && newEndLine.HasValue && !string.IsNullOrEmpty(pr.SourceBranch))
            {
                var newContent = await gitHubGraphQLService.GetFileContentAsync(
                    config.Organization,
                    pr.Repository,
                    pr.SourceBranch,
                    path,
                    user.AccessToken
                );

                if (newContent != null)
                {
                    newLines = ExtractLines(newContent, newStartLine.Value, newEndLine.Value);
                }
            }

            return Ok(new
            {
                oldLines = oldLines.Select(l => new { lineNumber = l.LineNumber, content = l.Content }),
                newLines = newLines.Select(l => new { lineNumber = l.LineNumber, content = l.Content })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to fetch file content", error = ex.Message });
        }
    }

    private static List<FileLineContent> ExtractLines(string content, int startLine, int endLine)
    {
        var lines = content.Split('\n');
        var result = new List<FileLineContent>();

        for (int i = startLine; i <= endLine && i <= lines.Length; i++)
        {
            var lineIndex = i - 1;
            if (lineIndex >= 0 && lineIndex < lines.Length)
            {
                result.Add(new FileLineContent(i, lines[lineIndex].TrimEnd('\r')));
            }
        }

        return result;
    }

    [HttpGet("{id}/pending-review")]
    [Authorize]
    public async Task<IActionResult> GetPendingReview(int id)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        try
        {
            var pendingReview = await gitHubService.GetPendingReviewAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                user.Username,
                user.AccessToken
            );

            if (pendingReview == null)
            {
                return Ok(new PendingReviewDto(string.Empty, "NONE", new List<PendingReviewCommentDto>()));
            }

            var dto = new PendingReviewDto(
                pendingReview.GitHubId,
                pendingReview.State,
                pendingReview.Comments.Select(c => new PendingReviewCommentDto(
                    c.GitHubId,
                    c.Path ?? string.Empty,
                    c.Line,
                    c.Body,
                    c.Author,
                    c.AuthorAvatar,
                    c.CreatedAt,
                    c.UpdatedAt
                )).ToList()
            );

            return Ok(dto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to get pending review", error = ex.Message });
        }
    }

    [HttpPost("{id}/pending-review/comments")]
    [Authorize]
    public async Task<IActionResult> AddPendingReviewComment(int id, [FromBody] CreatePendingReviewCommentRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        try
        {
            var comment = await gitHubService.AddPendingReviewCommentAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                request.Body,
                request.Path,
                request.Line,
                config,
                user.AccessToken
            );

            return Ok(new
            {
                commentId = comment.GitHubId,
                reviewId = comment.ReviewId,
                path = comment.Path,
                line = comment.Line,
                body = comment.Body,
                author = comment.Author,
                authorAvatar = comment.AuthorAvatar,
                createdAt = comment.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to add pending review comment", error = ex.Message });
        }
    }

    [HttpDelete("{id}/pending-review/comments/{commentId}")]
    [Authorize]
    public async Task<IActionResult> DeletePendingReviewComment(int id, string commentId)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        try
        {
            var success = await gitHubService.DeletePendingReviewCommentAsync(
                config.Organization,
                pr.Repository,
                commentId,
                user.AccessToken
            );

            if (success)
            {
                var pendingComment = await context.PendingComments
                    .FirstOrDefaultAsync(c => c.PendingReview.PullRequestId == id && c.PendingReview.UserId == userId);
                if (pendingComment != null)
                {
                    context.PendingComments.Remove(pendingComment);
                    await context.SaveChangesAsync();
                }
                return Ok(new { message = "Comment deleted successfully" });
            }

            return BadRequest(new { message = "Failed to delete comment" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to delete pending review comment", error = ex.Message });
        }
    }

    [HttpPost("{id}/pending-review/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitPendingReview(int id, [FromBody] SubmitPendingReviewRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        try
        {
            var pendingReview = await gitHubService.GetPendingReviewAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                user.Username,
                user.AccessToken
            );

            if (pendingReview == null)
            {
                return BadRequest(new { message = "No pending review found to submit" });
            }

            await gitHubService.SubmitPendingReviewAsync(
                config.Organization,
                pr.Repository,
                pendingReview.GitHubId,
                request.State,
                request.Body,
                user.AccessToken
            );

            return Ok(new { message = "Pending review submitted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to submit pending review", error = ex.Message });
        }
    }

    [HttpDelete("{id}/pending-review")]
    [Authorize]
    public async Task<IActionResult> DeletePendingReview(int id)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || string.IsNullOrEmpty(user.AccessToken))
        {
            return Unauthorized(new { message = "User token not available" });
        }

        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        var config = await context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return BadRequest(new { message = "GitHub configuration not found" });
        }

        try
        {
            var pendingReview = await gitHubService.GetPendingReviewAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                user.Username,
                user.AccessToken
            );

            if (pendingReview == null)
            {
                return BadRequest(new { message = "No pending review found to delete" });
            }

            await gitHubService.DeletePendingReviewAsync(
                config.Organization,
                pr.Repository,
                pendingReview.GitHubId,
                user.AccessToken
            );

            return Ok(new { message = "Pending review deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to delete pending review", error = ex.Message });
        }
    }
}