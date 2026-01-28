using Graphite.Api.DTOs;
using Graphite.Api.Extensions;
using Graphite.Api.Services;
using Graphite.Domain.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PullRequestsController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly AppDbContext _context;

    public PullRequestsController(ICacheService cacheService, AppDbContext context)
    {
        _cacheService = cacheService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var groupedPRs = await _cacheService.GetCachedPullRequestsAsync();
        
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
        var pr = await _context.PullRequests
            .Include(p => p.Reviews)
            .Include(p => p.ReviewThreads)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        var files = await _context.FileDiffs
            .Where(f => f.PullRequestId == id)
            .ToListAsync();

        var comments = await _context.Comments
            .Where(c => c.PullRequestId == id)
            .ToListAsync();

        return Ok(pr.ToDetailDto(files, comments));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _cacheService.GetPullRequestStatsAsync();
        return Ok(stats);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var config = await _cacheService.GetConfigAsync();

        if (config == null || string.IsNullOrEmpty(config.Organization) || string.IsNullOrEmpty(config.PersonalAccessToken))
        {
            return BadRequest("GitHub configuration not found. Please configure settings first.");
        }

        try
        {
            await _cacheService.RefreshPullRequestsAsync(config.Organization, config.PersonalAccessToken);
            await _cacheService.UpdateLastRefreshAsync();
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
        {
            return NotFound(new { message = "File not found" });
        }

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
    public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentRequest request)
    {
        var pr = await _context.PullRequests.FindAsync(id);
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

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return Ok(comment.ToDto());
    }

    [HttpPost("{id}/review")]
    public async Task<IActionResult> SubmitReview(int id, [FromBody] SubmitReviewRequest request)
    {
        var pr = await _context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        // In a real implementation, this would use GitHub API
        // For now, we'll just return a success message
        return Ok(new { message = $"Review submitted: {request.State}" });
    }

    [HttpPost("{id}/merge")]
    public async Task<IActionResult> MergePR(int id)
    {
        var pr = await _context.PullRequests.FindAsync(id);
        if (pr == null)
        {
            return NotFound(new { message = "Pull request not found" });
        }

        // In a real implementation, this would use GitHub API
        // For now, we'll just return a success message
        return Ok(new { message = "Pull request merge initiated" });
    }
}