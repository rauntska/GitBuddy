# Multi-Line Diff Comments & Line Selection

## Problem

Diff comments in GitBuddy anchor to exactly one line. `diff-line-row.vue` renders a hover `+` button per side that emits `addComment(lineNumber, side)` — a single number, no range. GitHub has supported click-and-drag range selection on line numbers for years, and reviewers expect it.

The single-line limit distorts how review comments get written:

- **Comments about a block get pinned to an arbitrary line.** "This whole loop should be extracted" lands on the `for` line, and the reader has to infer the extent. On a 30-line function it is genuinely ambiguous which code is being discussed.
- **Reviewers compensate with prose.** "Lines 40–68 below" — coordinates typed by hand, which go stale the moment the author pushes.
- **`code-suggestions` needs ranges to work at all.** A suggestion block replacing five lines with two is a range operation; GitHub's API takes `start_line`/`line`. Single-line anchoring blocks the most useful half of that idea.
- **No line addressing at all.** There is no way to select lines, copy a permalink to them, or highlight them for someone else — `FileDiffViewer` can `scrollIntoView` a target but nothing produces such a target from the UI.

The backend is not obviously the blocker: `Comment` already carries a path and side (`DiffSide`), and GitHub's review-comment API accepts ranges natively.

## Rough Approach

### Selection Interaction

- Click a line number to select it; shift-click or drag to extend. Selected rows get a tinted background and the gutter shows the range extent.
- The hover `+` button becomes "comment on selection" when a range is active, and stays single-line otherwise — the current behaviour is the zero-selection default, so nothing regresses.
- Selection is per-file and clears on `Esc`, which fits the shortcut scoping in `keyboard-shortcuts` (`c` to comment on the current selection).
- Ranges must stay within one file and one side. Cross-side ranges have no meaning in the GitHub model, so the interaction should refuse them rather than fail on submit.

### Data & API

- Extend `Comment` with `StartLine` (nullable) alongside the existing line anchor; null means single-line and every existing comment stays valid without backfill.
- Pass `start_line`/`start_side` through the comment-creation path, including the pending-review flow (`POST /{id}/pending-review/comments`), so batched review comments support ranges too.
- Rendering: highlight the full range in the diff when a threaded comment is anchored to one, and show `40–68` rather than `40` in `thread-header.vue`.

### Line Permalinks

- Once a range is addressable, a selection can produce a link (`/pr/:id/files?path=…&L40-L68`) that scrolls and highlights on load — reusing the deep-link work in `pr-detail-navigation`.
- "Copy link to lines" belongs in the existing `ContextMenu` pattern.

## Open Questions

- **Outdated ranges.** A single-line anchor going stale is already handled as outdated; a range can be *partially* invalidated by a push. Mark the whole thread outdated, or attempt to re-anchor?
- **Unified vs. split view.** Selection in unified mode is one column; split mode has two independent sides. Does drag selection behave identically in both, and what happens when a drag crosses the gutter?
- **Text selection conflict.** Dragging over line numbers must not fight native text selection of the code itself. Restricting drag to the line-number gutter is the usual answer — is that discoverable enough?
- **Range size cap.** Should selecting 800 lines be allowed? GitHub caps ranges; a limit with a clear message beats an API rejection after the comment is written.
- **Sequencing with `code-suggestions`** — is this a prerequisite that ships first, or should both be designed as one piece of work since the suggestion UI needs the range UI anyway?
