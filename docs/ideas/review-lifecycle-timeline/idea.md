> **Status: Implemented** — see commit history for the shipped code.

# Review Progress Timeline

## Summary
Enhanced ReviewTimeline with a lifecycle view showing the review progression as a color-coded vertical timeline. Toggle between Events (original) and Lifecycle views via a segmented control.

## Key Decisions
- Added `viewMode` state (`'events' | 'lifecycle'`) — local component state, not persisted
- Lifecycle mode derives steps from existing `ReviewEvent[]` data — no new API calls
- Color-coded nodes: green (approved), blue (changes requested), purple (reviewed), amber (comment thread)
- Gradient timeline line colored per step
- No new dependencies
