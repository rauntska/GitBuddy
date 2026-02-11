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
public class PullRequestsController(ICacheService cacheService, AppDbContext context, IGitHubService gitHubService)
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

        // Get user ID if authenticated
        int? userId = null;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        // Fetch user's viewed states if authenticated
        List<UserFileViewedState>? viewedStates = null;
        if (userId.HasValue)
        {
            viewedStates = await context.UserFileViewedStates
                .Where(uvs => uvs.UserId == userId.Value && files.Select(f => f.Id).Contains(uvs.FileDiffId))
                .ToListAsync();
        }

        return Ok(pr.ToDetailDto(files, comments, viewedStates));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await cacheService.GetPullRequestStatsAsync();
        return Ok(stats);
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
        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        // In a real implementation, this would use GitHub API
        // For now, we'll just create a mock comment
        var comment = new Domain.Models.Comment
        {
            PullRequestId = id,
            GitHubId = DateTime.UtcNow.Ticks, // Mock GitHub ID
            Author = "Current User", // Would come from auth
            AuthorAvatar = null,
            Body = request.Body,
            CreatedAt = DateTime.UtcNow,
            Path = request.Path,
            Line = request.Line,
            IsOutdated = false
        };

        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        return Ok(comment.ToDto());
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
            var comment = await gitHubService.AddPullRequestReviewThreadReplyAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                request.ReviewThreadId,
                request.Body,
                config,
                user.AccessToken
            );

            return Ok(comment);
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
    public async Task<IActionResult> MergePR(int id)
    {
        var pr = await context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        // In a real implementation, this would use GitHub API
        // For now, we'll just return a success message
        return Ok(new { message = "Pull request merge initiated" });
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
}