namespace Graphite.Api.DTOs;

// Extended comment DTO for individual comment operations
public class ExtendedCommentDto
{
    public int Id { get; set; }
    public int PullRequestId { get; set; }
    public int? ReviewThreadId { get; set; }
    public long GitHubId { get; set; }
    public string Author { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Path { get; set; }
    public int? Line { get; set; }
    public bool IsOutdated { get; set; }
    public DateTime? EditedAt { get; set; }
    public int EditCount { get; set; }
    public int? ReplyToCommentId { get; set; }
    public List<CommentReactionDto> Reactions { get; set; } = new();
}

public class UpdateCommentDto
{
    public string Body { get; set; } = string.Empty;
}

public class AddReactionDto
{
    public string Reaction { get; set; } = string.Empty;
}

public class CommentDraftDto
{
    public int Id { get; set; }
    public int PullRequestId { get; set; }
    public int? ReviewThreadId { get; set; }
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class SaveCommentDraftDto
{
    public int PullRequestId { get; set; }
    public int? ReviewThreadId { get; set; }
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }
    public string Body { get; set; } = string.Empty;
}

public class CommentTemplateDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsOrganizationTemplate { get; set; }
}

public class CreateCommentTemplateDto
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public bool IsOrganizationTemplate { get; set; }
}

public class UpdateCommentTemplateDto
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Tags { get; set; }
}

public class MentionableUserDto
{
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Name { get; set; }
}

public class ReactionsSummaryDto
{
    public List<ReactionGroupDto> Groups { get; set; } = new();
}

public class ReactionGroupDto
{
    public string Reaction { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<string> Usernames { get; set; } = new();
    public bool HasReacted { get; set; }
}
