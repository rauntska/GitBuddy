using Graphite.Api.DTOs;
using Graphite.Api.Extensions;
using Graphite.Api.Features.PullRequests.AddComment;
using Graphite.Api.Features.PullRequests.AddCommentReply;
using Graphite.Api.Features.PullRequests.AddPendingReviewComment;
using Graphite.Api.Features.PullRequests.DeletePendingReview;
using Graphite.Api.Features.PullRequests.DeletePendingReviewComment;
using Graphite.Api.Features.PullRequests.GetById;
using Graphite.Api.Features.PullRequests.GetFileContent;
using Graphite.Api.Features.PullRequests.GetMentionableUsers;
using Graphite.Api.Features.PullRequests.GetMergeOptions;
using Graphite.Api.Features.PullRequests.GetPendingReview;
using Graphite.Api.Features.PullRequests.GetUnreadCount;
using Graphite.Api.Features.PullRequests.Merge;
using Graphite.Api.Features.PullRequests.PublishDraftPR;
using Graphite.Api.Features.PullRequests.RefreshFileViewedStates;
using Graphite.Api.Features.PullRequests.ResolveReviewThread;
using Graphite.Api.Features.PullRequests.SubmitPendingReview;
using Graphite.Api.Features.PullRequests.SubmitReview;
using Graphite.Api.Features.PullRequests.UnresolveReviewThread;
using Graphite.Api.Features.PullRequests.UpdateFileViewedState;
using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PullRequestsController(
    ISender mediator,
    ICacheService cacheService, 
    AppDbContext context) 
    : BaseController(context)
{
    private readonly AppDbContext _context = context;

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
        var result = await mediator.Send(new GetPullRequestByIdQuery(id, User));
        if (result == null)
            return NotFound(new { message = "Pull request not found" });
        return Ok(result);
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
        var user = await CurrentUser();
        if (user == null)
            return Unauthorized(new { message = "User not found" });

        var count = await mediator.Send(new GetUnreadCountQuery(user.Username));
        return Ok(new { count });
    }

    [HttpGet("merged")]
    public async Task<IActionResult> GetMergedPRs([FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        var mergedPRs = await _context.PullRequests
            .Include(pr => pr.Reviews)
            .Include(pr => pr.ReviewThreads)
            .Where(pr => pr.IsMerged)
            .OrderByDescending(pr => pr.MergedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var totalCount = await _context.PullRequests.CountAsync(pr => pr.IsMerged);

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
            return BadRequest("GitHub configuration not found. Please configure settings first.");

        if (!config.UseGitHubApp && string.IsNullOrEmpty(config.PersonalAccessToken))
            return BadRequest("GitHub configuration not found. Please configure settings first.");

        if (config.UseGitHubApp && (string.IsNullOrEmpty(config.AppId) || string.IsNullOrEmpty(config.PrivateKey) || string.IsNullOrEmpty(config.InstallationId)))
            return BadRequest("GitHub App configuration is incomplete. Please configure App ID, Private Key, and Installation ID.");

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
        var fileDiff = await _context.FileDiffs
            .FirstOrDefaultAsync(f => f.PullRequestId == id && f.Path == filePath);

        if (fileDiff == null)
            return NotFound(new { message = "File not found" });

        return Ok(fileDiff.ToDto());
    }

    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetComments(int id)
    {
        var comments = await _context.Comments
            .Where(c => c.PullRequestId == id)
            .ToListAsync();

        return Ok(comments.Select(c => c.ToDto()));
    }

    [HttpPost("{id}/comments")]
    [Authorize]
    public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentRequest request)
    {
        try
        {
            var result = await mediator.Send(new AddCommentCommand(id, request.Body, request.Path, request.Line, User));
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
        try
        {
            var result = await mediator.Send(new AddCommentReplyCommand(id, request.ReviewThreadId, request.Body, User));
            
            if (result.IsPending && result.PendingReply != null)
            {
                return Ok(new
                {
                    isPending = true,
                    pendingReviewId = result.PendingReply.PendingReviewId,
                    commentNodeId = result.PendingReply.CommentNodeId,
                    reviewThreadId = result.PendingReply.ReviewThreadId,
                    author = result.PendingReply.Author,
                    authorAvatar = result.PendingReply.AuthorAvatar,
                    body = result.PendingReply.Body,
                    createdAt = result.PendingReply.CreatedAt,
                    updatedAt = result.PendingReply.UpdatedAt
                });
            }

            return Ok(result.Comment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
        var result = await mediator.Send(new ResolveReviewThreadCommand(id, threadId, request.Resolved, User));
        
        if (!result.Success)
        {
            if (result.Error != null)
                return StatusCode(500, new { message = result.Message, error = result.Error });
            return NotFound(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpPost("{id}/threads/{threadId}/unresolve")]
    [Authorize]
    public async Task<IActionResult> UnresolveReviewThread(int id, string threadId, [FromBody] UnresolveThreadRequest request)
    {
        var result = await mediator.Send(new UnresolveReviewThreadCommand(id, threadId, User));
        
        if (!result.Success)
        {
            if (result.Error != null)
                return StatusCode(500, new { message = result.Message, error = result.Error });
            return NotFound(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpPost("{id}/review")]
    [Authorize]
    public async Task<IActionResult> SubmitReview(int id, [FromBody] SubmitReviewRequest request)
    {
        var result = await mediator.Send(new SubmitReviewCommand(id, request.State, request.Body, User));
        
        if (!result.Success)
        {
            if (result.Error != null)
                return StatusCode(500, new { message = result.Message, error = result.Error });
            return NotFound(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpPost("{id}/merge")]
    [Authorize]
    public async Task<IActionResult> MergePR(int id, [FromBody] MergePRRequest? request = null)
    {
        var result = await mediator.Send(new MergePRCommand(id, request?.MergeMethod, request?.CommitTitle, request?.CommitMessage, User));
        
        if (!result.Success)
        {
            if (result.StatusCode == 404)
                return NotFound(new { message = result.Message });
            if (result.StatusCode.HasValue)
                return StatusCode(result.StatusCode.Value, new { message = result.Message, error = result.Error });
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message, isMerged = result.IsMerged, mergedAt = result.MergedAt });
    }

    [HttpGet("{id}/merge-options")]
    [Authorize]
    public async Task<IActionResult> GetMergeOptions(int id)
    {
        try
        {
            var result = await mediator.Send(new GetMergeOptionsQuery(id, User));
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
        var result = await mediator.Send(new PublishDraftPRCommand(id, User));
        
        if (!result.Success)
        {
            if (result.Error != null)
                return StatusCode(500, new { message = result.Message, error = result.Error });
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message, draft = result.Draft, status = result.Status });
    }

    [HttpPost("{id}/files/viewed")]
    [Authorize]
    public async Task<IActionResult> UpdateFileViewedState(int id, [FromBody] UpdateViewedStateRequest request)
    {
        try
        {
            await mediator.Send(new UpdateFileViewedStateCommand(id, request.Path, request.Viewed, User));
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to update viewed state on GitHub", error = ex.Message });
        }
    }

    [HttpGet("{id}/mentionable-users")]
    public async Task<IActionResult> GetMentionableUsers(int id)
    {
        try
        {
            var result = await mediator.Send(new GetMentionableUsersQuery(id));
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/file-diffs/refresh-viewed-states")]
    [Authorize]
    public async Task<IActionResult> RefreshFileViewedStates(int id)
    {
        try
        {
            var result = await mediator.Send(new RefreshFileViewedStatesCommand(id, User));
            return Ok(result.Files);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
        try
        {
            var result = await mediator.Send(new GetFileContentQuery(id, path, oldStartLine, oldEndLine, newStartLine, newEndLine, User));
            return Ok(new
            {
                oldLines = result.OldLines.Select(l => new { lineNumber = l.LineNumber, content = l.Content }),
                newLines = result.NewLines.Select(l => new { lineNumber = l.LineNumber, content = l.Content })
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to fetch file content", error = ex.Message });
        }
    }

    [HttpGet("{id}/pending-review")]
    [Authorize]
    public async Task<IActionResult> GetPendingReview(int id)
    {
        try
        {
            var result = await mediator.Send(new GetPendingReviewQuery(id, User));
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
        try
        {
            var result = await mediator.Send(new AddPendingReviewCommentCommand(id, request.Body, request.Path, request.Line, User));
            return Ok(new
            {
                commentId = result.CommentId,
                reviewId = result.ReviewId,
                path = result.Path,
                line = result.Line,
                body = result.Body,
                author = result.Author,
                authorAvatar = result.AuthorAvatar,
                createdAt = result.CreatedAt
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
        try
        {
            var result = await mediator.Send(new DeletePendingReviewCommentCommand(id, commentId, User));
            
            if (!result.Success)
                return BadRequest(new { message = result.Message });
            
            return Ok(new { message = result.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
        var result = await mediator.Send(new SubmitPendingReviewCommand(id, request.State, request.Body, User));
        
        if (!result.Success)
        {
            if (result.Error != null)
                return StatusCode(500, new { message = result.Message, error = result.Error });
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpDelete("{id}/pending-review")]
    [Authorize]
    public async Task<IActionResult> DeletePendingReview(int id)
    {
        var result = await mediator.Send(new DeletePendingReviewCommand(id, User));
        
        if (!result.Success)
        {
            if (result.Error != null)
                return StatusCode(500, new { message = result.Message, error = result.Error });
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }
}
