namespace Graphite.Api.DTOs;

public record UserPreferencesDto(
    string DiffViewMode,
    int FileTreeWidth,
    int CommentsPanelWidth,
    bool FileTreeVisible,
    string ListViewMode,
    string? NotificationPreferences
);

public record UpdatePreferencesRequest(
    string? DiffViewMode,
    int? FileTreeWidth,
    int? CommentsPanelWidth,
    bool? FileTreeVisible,
    string? ListViewMode,
    string? NotificationPreferences
);
