using GitBuddy.Api.DTOs;
using GitBuddy.Api.Services;
using GitBuddy.Domain.Data;
using GitBuddy.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Controllers;

[ApiController]
[Route("api/comments")]
[Authorize]
public class CommentsController(
    AppDbContext context,
    IUserService userService,
    IGitHubService gitHubService)
    : ControllerBase
{
    [HttpPut("{commentId}")]
    public async Task<ActionResult<ExtendedCommentDto>> UpdateComment(int commentId, [FromBody] UpdateCommentDto dto)
    {
        var currentUser = await userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var comment = await context.Comments
            .Include(c => c.Reactions)
            .FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment == null)
            return NotFound();

        // Only allow author to edit
        if (comment.Author != currentUser.Username)
            return Forbid();

        // Update on GitHub if we have the GitHub ID and user's access token
        if (comment.GitHubId > 0 && !string.IsNullOrEmpty(currentUser.AccessToken))
        {
            var pr = await context.PullRequests
                .FirstOrDefaultAsync(p => p.Id == comment.PullRequestId);
            
            if (pr != null)
            {
                var config = await context.GitHubConfigs.FirstOrDefaultAsync();
                if (config != null)
                {
                    try
                    {
                        await gitHubService.UpdateReviewCommentAsync(
                            config.Organization,
                            pr.Repository,
                            comment.GitHubId,
                            dto.Body,
                            currentUser.AccessToken
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to update comment on GitHub: {ex.Message}");
                    }
                }
            }
        }

        comment.Body = dto.Body;
        comment.EditedAt = DateTime.UtcNow;
        comment.EditCount++;
        comment.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(MapToExtendedDto(comment, currentUser.Username));
    }

    [HttpDelete("{commentId}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var currentUser = await userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var comment = await context.Comments.FindAsync(commentId);
        if (comment == null)
            return NotFound();

        if (comment.Author != currentUser.Username)
            return Forbid();

        // Delete from GitHub if we have the GitHub ID and user's access token
        if (comment.GitHubId > 0 && !string.IsNullOrEmpty(currentUser.AccessToken))
        {
            var pr = await context.PullRequests
                .FirstOrDefaultAsync(p => p.Id == comment.PullRequestId);
            
            if (pr != null)
            {
                var config = await context.GitHubConfigs.FirstOrDefaultAsync();
                if (config != null)
                {
                    try
                    {
                        await gitHubService.DeleteReviewCommentAsync(
                            config.Organization,
                            pr.Repository,
                            comment.GitHubId,
                            currentUser.AccessToken
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to delete comment from GitHub: {ex.Message}");
                    }
                }
            }
        }

        context.Comments.Remove(comment);
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{commentId}/reactions")]
    public async Task<ActionResult<CommentReactionDto>> AddReaction(int commentId, [FromBody] AddReactionDto dto)
    {
        var currentUser = await userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var validReactions = new[] { "thumbsup", "thumbsdown", "laugh", "hooray", "confused", "heart", "rocket", "eyes" };
        if (!validReactions.Contains(dto.Reaction.ToLower()))
            return BadRequest("Invalid reaction type");

        var comment = await context.Comments.FindAsync(commentId);
        if (comment == null)
            return NotFound();

        // Check if reaction already exists
        var existingReaction = await context.CommentReactions
            .FirstOrDefaultAsync(r => r.CommentId == commentId && r.Username == currentUser.Username && r.Reaction == dto.Reaction);

        if (existingReaction != null)
            return Ok(new CommentReactionDto(
                existingReaction.Id,
                existingReaction.Username,
                existingReaction.Reaction,
                existingReaction.CreatedAt
            ));

        var reaction = new CommentReaction
        {
            CommentId = commentId,
            Username = currentUser.Username,
            Reaction = dto.Reaction.ToLower(),
            CreatedAt = DateTime.UtcNow
        };

        context.CommentReactions.Add(reaction);
        await context.SaveChangesAsync();

        return Ok(new CommentReactionDto(
            reaction.Id,
            reaction.Username,
            reaction.Reaction,
            reaction.CreatedAt
        ));
    }

    [HttpDelete("{commentId}/reactions/{reaction}")]
    public async Task<IActionResult> RemoveReaction(int commentId, string reaction)
    {
        var currentUser = await userService.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var existingReaction = await context.CommentReactions
            .FirstOrDefaultAsync(r => r.CommentId == commentId && r.Username == currentUser.Username && r.Reaction == reaction);

        if (existingReaction == null)
            return NotFound();

        context.CommentReactions.Remove(existingReaction);
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{commentId}/reactions")]
    public async Task<ActionResult<ReactionsSummaryDto>> GetReactions(int commentId)
    {
        var currentUser = await userService.GetCurrentUserAsync(User);
        var currentUsername = currentUser?.Username ?? "";

        var reactions = await context.CommentReactions
            .Where(r => r.CommentId == commentId)
            .ToListAsync();

        var emojiMap = new Dictionary<string, string>
        {
            { "thumbsup", "👍" },
            { "thumbsdown", "👎" },
            { "laugh", "😄" },
            { "hooray", "🎉" },
            { "confused", "😕" },
            { "heart", "❤️" },
            { "rocket", "🚀" },
            { "eyes", "👀" }
        };

        var groups = reactions
            .GroupBy(r => r.Reaction)
            .Select(g => new ReactionGroupDto
            {
                Reaction = g.Key,
                Emoji = emojiMap.GetValueOrDefault(g.Key, "👍"),
                Count = g.Count(),
                Usernames = g.Select(r => r.Username).ToList(),
                HasReacted = g.Any(r => r.Username == currentUsername)
            })
            .OrderByDescending(g => g.Count)
            .ToList();

        return Ok(new ReactionsSummaryDto { Groups = groups });
    }

    private ExtendedCommentDto MapToExtendedDto(Comment comment, string currentUsername)
    {
        return new ExtendedCommentDto
        {
            Id = comment.Id,
            PullRequestId = comment.PullRequestId,
            ReviewThreadId = comment.ReviewThreadId,
            GitHubId = comment.GitHubId,
            Author = comment.Author,
            AuthorAvatar = comment.AuthorAvatar,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            Path = comment.Path,
            Line = comment.Line,
            IsOutdated = comment.IsOutdated,
            EditedAt = comment.EditedAt,
            EditCount = comment.EditCount,
            ReplyToCommentId = comment.ReplyToCommentId,
            Reactions = comment.Reactions?.Select(r => new CommentReactionDto(
                r.Id,
                r.Username,
                r.Reaction,
                r.CreatedAt
            )).ToList() ?? new()
        };
    }
}
