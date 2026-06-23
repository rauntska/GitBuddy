using GitBuddy.Api.DTOs;
using GitBuddy.Api.Services;
using GitBuddy.Domain.Data;
using GitBuddy.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Controllers;

[ApiController]
[Route("api/comment-templates")]
[Authorize]
public class CommentTemplatesController(AppDbContext context, IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CommentTemplateDto>>> GetTemplates([FromQuery] string? search, [FromQuery] string? tag)
    {
        var currentUser = await userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var query = context.CommentTemplates
            .Where(t => t.UserId == currentUser.Id || t.IsOrganizationTemplate);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t =>
                t.Title.ToLower().Contains(search.ToLower()) ||
                t.Body.ToLower().Contains(search.ToLower()));
        }

        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(t => t.Tags != null && t.Tags.ToLower().Contains(tag.ToLower()));
        }

        var templates = await query
            .OrderByDescending(t => t.UsageCount)
            .ThenByDescending(t => t.LastUsedAt)
            .ToListAsync();

        return Ok(templates.Select(MapToDto));
    }

    [HttpGet("tags")]
    public async Task<ActionResult<List<string>>> GetTags()
    {
        var currentUser = await userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var templates = await context.CommentTemplates
            .Where(t => t.UserId == currentUser.Id || t.IsOrganizationTemplate)
            .Where(t => t.Tags != null)
            .Select(t => t.Tags!)
            .ToListAsync();

        var tags = templates
            .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(t => t.Trim().ToLower())
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        return Ok(tags);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CommentTemplateDto>> GetTemplate(int id)
    {
        var currentUser = await userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var template = await context.CommentTemplates
            .FirstOrDefaultAsync(t => t.Id == id && (t.UserId == currentUser.Id || t.IsOrganizationTemplate));

        if (template == null)
            return NotFound();

        return Ok(MapToDto(template));
    }

    [HttpPost]
    public async Task<ActionResult<CommentTemplateDto>> CreateTemplate([FromBody] CreateCommentTemplateDto dto)
    {
        var currentUser = await userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var template = new CommentTemplate
        {
            UserId = currentUser.Id,
            Title = dto.Title,
            Body = dto.Body,
            Tags = dto.Tags,
            IsOrganizationTemplate = dto.IsOrganizationTemplate,
            CreatedAt = DateTime.UtcNow
        };

        context.CommentTemplates.Add(template);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, MapToDto(template));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CommentTemplateDto>> UpdateTemplate(int id, [FromBody] UpdateCommentTemplateDto dto)
    {
        var currentUser = await userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var template = await context.CommentTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUser.Id);

        if (template == null)
            return NotFound();

        // Only the creator can edit their templates
        if (template.UserId != currentUser.Id)
            return Forbid();

        template.Title = dto.Title;
        template.Body = dto.Body;
        template.Tags = dto.Tags;

        await context.SaveChangesAsync();

        return Ok(MapToDto(template));
    }

    [HttpPost("{id}/use")]
    public async Task<ActionResult<CommentTemplateDto>> RecordUsage(int id)
    {
        var currentUser = await userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var template = await context.CommentTemplates
            .FirstOrDefaultAsync(t => t.Id == id && (t.UserId == currentUser.Id || t.IsOrganizationTemplate));

        if (template == null)
            return NotFound();

        template.UsageCount++;
        template.LastUsedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(MapToDto(template));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var currentUser = await userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var template = await context.CommentTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUser.Id);

        if (template == null)
            return NotFound();

        context.CommentTemplates.Remove(template);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private CommentTemplateDto MapToDto(CommentTemplate template)
    {
        return new CommentTemplateDto
        {
            Id = template.Id,
            Title = template.Title,
            Body = template.Body,
            Tags = template.Tags,
            UsageCount = template.UsageCount,
            CreatedAt = template.CreatedAt,
            LastUsedAt = template.LastUsedAt,
            IsOrganizationTemplate = template.IsOrganizationTemplate
        };
    }
}
