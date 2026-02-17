using Graphite.Api.DTOs;
using Graphite.Api.Services;
using Graphite.Domain.Data;
using Graphite.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Controllers;

[ApiController]
[Route("api/comment-drafts")]
[Authorize]
public class CommentDraftsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IUserService _userService;

    public CommentDraftsController(AppDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    [HttpGet("prs/{pullRequestId}")]
    public async Task<ActionResult<List<CommentDraftDto>>> GetDraftsForPullRequest(int pullRequestId)
    {
        var currentUser = await _userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var drafts = await _context.CommentDrafts
            .Where(d => d.UserId == currentUser.Id && d.PullRequestId == pullRequestId)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync();

        return Ok(drafts.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CommentDraftDto>> GetDraft(int id)
    {
        var currentUser = await _userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var draft = await _context.CommentDrafts
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == currentUser.Id);

        if (draft == null)
            return NotFound();

        return Ok(MapToDto(draft));
    }

    [HttpPost]
    public async Task<ActionResult<CommentDraftDto>> SaveDraft([FromBody] SaveCommentDraftDto dto)
    {
        var currentUser = await _userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        // Check if draft already exists for this location
        var existingDraft = await _context.CommentDrafts
            .FirstOrDefaultAsync(d =>
                d.UserId == currentUser.Id &&
                d.PullRequestId == dto.PullRequestId &&
                d.ReviewThreadId == dto.ReviewThreadId &&
                d.FilePath == dto.FilePath &&
                d.LineNumber == dto.LineNumber);

        if (existingDraft != null)
        {
            existingDraft.Body = dto.Body;
            existingDraft.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(MapToDto(existingDraft));
        }

        var draft = new CommentDraft
        {
            UserId = currentUser.Id,
            PullRequestId = dto.PullRequestId,
            ReviewThreadId = dto.ReviewThreadId,
            FilePath = dto.FilePath,
            LineNumber = dto.LineNumber,
            Body = dto.Body,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CommentDrafts.Add(draft);
        await _context.SaveChangesAsync();

        return Ok(MapToDto(draft));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDraft(int id)
    {
        var currentUser = await _userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var draft = await _context.CommentDrafts
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == currentUser.Id);

        if (draft == null)
            return NotFound();

        _context.CommentDrafts.Remove(draft);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("prs/{pullRequestId}/clear")]
    public async Task<IActionResult> ClearDraftsForPullRequest(int pullRequestId, [FromQuery] int? reviewThreadId, [FromQuery] string? filePath, [FromQuery] int? lineNumber)
    {
        var currentUser = await _userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var query = _context.CommentDrafts
            .Where(d => d.UserId == currentUser.Id && d.PullRequestId == pullRequestId);

        if (reviewThreadId.HasValue)
            query = query.Where(d => d.ReviewThreadId == reviewThreadId);
        if (!string.IsNullOrEmpty(filePath))
            query = query.Where(d => d.FilePath == filePath);
        if (lineNumber.HasValue)
            query = query.Where(d => d.LineNumber == lineNumber);

        var drafts = await query.ToListAsync();
        _context.CommentDrafts.RemoveRange(drafts);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private CommentDraftDto MapToDto(CommentDraft draft)
    {
        return new CommentDraftDto
        {
            Id = draft.Id,
            PullRequestId = draft.PullRequestId,
            ReviewThreadId = draft.ReviewThreadId,
            FilePath = draft.FilePath,
            LineNumber = draft.LineNumber,
            Body = draft.Body,
            UpdatedAt = draft.UpdatedAt
        };
    }
}
