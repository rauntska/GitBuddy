# Review Queue Sessions — Guided "Review Everything" Mode

## Problem

Every existing surface is a *browsing* surface: the Dashboard lists PRs, `PRDetail` shows one, and you navigate between them by going back to the list and clicking the next row. Nothing helps you actually **work through** a backlog:

- After reviewing PR #412 you land back on the Dashboard and re-scan `PRGroup` lists to find where you were. Multiply by 12 PRs on a Monday morning.
- There is no notion of "done for now" within a sitting — `PRRow` unread state clears on visit, but a PR you deliberately skipped looks identical to one you never opened.
- Submitting a review from `PRDetail` (`POST /api/pull-requests/{id}/pending-review/submit`) drops you on the same page you already finished with. The next action is always manual.
- Related-but-different ideas: `saved-filters` defines *which* PRs matter, `command-palette` jumps to *one* PR, `pr-snooze` defers a PR indefinitely. None of them drive a *sequence*.

The pending-review machinery (`PendingReview`, `PendingComment`, `CommentDraft`, `useDraftAutosave`) already makes multi-PR work resumable at the data layer. There is just no UI that treats a set of PRs as a worklist.

## Rough Approach

### Session Model

- A session is an ordered, ephemeral list of PR ids plus a cursor. Start it from any list context: a group header ("Review all 7"), a saved filter, or "PRs awaiting my review".
- Persist per user so a session survives refresh and machine switch — either a `ReviewSession` table (`UserId`, `PrIds` JSON, `CursorIndex`, `StartedAt`, `CompletedAt`) or, cheaper, a JSON blob on `UserPreferences` alongside `PinnedPrIds`. Only one active session per user.
- Ordering respects the existing `PriorityService` score by default; the picker can offer "smallest first" (`FileDiff` counts are already stored) for a quick-wins pass.

### Session UI

- A slim session bar pinned above `PRDetail`: `3 / 12 · Approved 2 · Skipped 1`, with **Previous / Skip / Next** and an **End session** exit.
- Submitting a review, approving, or skipping advances the cursor automatically and routes to the next PR.
- Keyboard-first: `j`/`k` or `n`/`p` to move, `s` to skip, `a` to approve, `Esc` to leave. This is the natural place to introduce app-wide key handling that `command-palette` would also consume.
- Skips are session-scoped, not persistent — a skipped PR returns to the dashboard normally. Users wanting a durable defer should reach for `pr-snooze`.

### Session Summary

- On completion, a small recap: PRs reviewed, approvals given, comments left, time elapsed. Feeds naturally into the shipped analytics surface as a "review throughput" input.
- PRs that changed underneath you mid-session (webhook `pull_request.synchronize`) get flagged in the bar rather than silently reordered.

## Open Questions

- **Stale membership** — if a PR in the session gets merged or closed by someone else mid-session, skip it silently or show a "gone" card?
- **Session vs. filter coupling** — should a session be a live query (re-evaluates as PRs change) or a frozen snapshot at start time? Frozen is simpler and probably more predictable.
- **Persisted or not** — is a DB table justified, or is `UserPreferences` JSON enough for v1? Sessions have no cross-user meaning, so probably the latter.
- **Interaction with `pr-snooze`** — should skipping three times in a row suggest snoozing?
- **Scope of keyboard handling** — introduce a shared keymap composable now (shared with `command-palette`), or keep bindings local to session mode until a second consumer exists?
- **Mobile** — does session mode make sense on a phone (see `mobile-review`), where it is arguably the *best* interaction model, or is it desktop-only for v1?
