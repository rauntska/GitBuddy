> **Status: Implemented** — see commit history for the shipped code.

# Quick Actions Context Menu

## Summary
A "..." menu on each PR row (plus right-click) exposing common actions without opening PR detail. Includes: Open PR Detail, Pin/Unpin, Copy PR Link, Copy Branch Name, View on GitHub.

## Key Decisions
- `ContextMenu.vue` is a generic reusable component (accepts `MenuItem[]`)
- PRRow emits `contextmenu` event, Dashboard renders the ContextMenu
- Pin action persists via `UserPreferences.PinnedPrIds`
- No new dependency — pure Vue + Teleport
