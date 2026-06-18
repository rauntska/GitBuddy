> **Status: Implemented** — see commit history for the shipped code.

# Dashboard Layout Customization

## Summary
Users can reorder PR groups via drag-and-drop, show/hide groups, and reset to defaults. Edit mode is toggled via a "Customize" button. Layout preferences persist via UserPreferences API.

## Key Decisions
- Native HTML5 Drag and Drop (no new dependency)
- Group order stored as `dashboardGroupOrder` (JSON string array) in UserPreferences
- Hidden groups stored as `hiddenDashboardGroups` (JSON string array)
- Hidden groups shown in edit mode with "Show" buttons
- Pinned PRs stored separately as `pinnedPrIds` (pinned via Quick Actions menu)
