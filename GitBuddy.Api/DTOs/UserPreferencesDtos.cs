namespace GitBuddy.Api.DTOs;

public record UserPreferencesDto(
    string DiffViewMode,
    string MarkdownDiffMode,
    int FileTreeWidth,
    int CommentsPanelWidth,
    bool FileTreeVisible,
    string ListViewMode,
    bool ShowColumnHeaders,
    bool ShowContext,
    bool PrioritySort,
    string? PinnedPrIds,
    string? DashboardGroupOrder,
    string? HiddenDashboardGroups,
    string? NotificationPreferences
);

public record UpdatePreferencesRequest(
    string? DiffViewMode,
    string? MarkdownDiffMode,
    int? FileTreeWidth,
    int? CommentsPanelWidth,
    bool? FileTreeVisible,
    string? ListViewMode,
    bool? ShowColumnHeaders,
    bool? ShowContext,
    bool? PrioritySort,
    string? PinnedPrIds,
    string? DashboardGroupOrder,
    string? HiddenDashboardGroups,
    string? NotificationPreferences
);
