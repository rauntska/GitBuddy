> **Status: Implemented** — see commit history for the shipped code.

# View Density Modes

## Summary
Three density levels for PR rows: Compact (single-line, minimal), Comfortable (current default), and Expanded (2-line with metadata bar). Controlled via a segmented toggle in the dashboard toolbar.

## Key Decisions
- Extended `listViewMode` from `"compact" | "normal"` to `"compact" | "comfortable" | "expanded"`
- No backend migration needed — column is already a string
- Expanded mode shows author avatar initials, title with PR number, and a metadata bar with additions/deletions, file count, threads, reviewer avatars, and time ago
