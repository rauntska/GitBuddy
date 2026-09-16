namespace GitBuddy.Api.DTOs;

public record ChangelogEntryDto(
    string Slug,
    DateOnly PublishedOn,
    string Category,
    string Title,
    string Body,
    DateTime CreatedAt
);

public record ChangelogResponseDto(
    IReadOnlyList<ChangelogEntryDto> Entries,
    DateTime LastSeenChangelogAt
);

public record MarkSeenResponseDto(DateTime LastSeenChangelogAt);
