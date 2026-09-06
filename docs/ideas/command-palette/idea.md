# Command Palette & Global Search

## Problem

There is no search anywhere in GitBuddy. Every interaction is mouse-driven: scan `PRGroup` lists on the Dashboard, find the row, click it. Concretely:

- **No jump-to-PR** — "open PR 4171" requires visually scanning the whole dashboard or typing the GitHub URL by hand.
- **No content search** — can't find "that PR about the retry logic" by title/branch, and can't search comment bodies at all ("who mentioned `NpgsqlDataSource` last week?").
- **Actions are buried** — merging, pinning, copying links, creating PRs from branches all live behind clicks into PR detail or the `ContextMenu`.
- `saved-filters` solves *recurring* views; it doesn't solve ad-hoc "find this one thing right now".

## Rough Approach

### Command Palette (Cmd/Ctrl+K)

- Global overlay component (Teleport, like `ContextMenu`) opened with Cmd/Ctrl+K or the `/` key, available from Dashboard and PR detail.
- Fuzzy-matched results, arrow-key navigation, Enter to execute, Esc to close, recent items when the query is empty.

### Search Sources

- **Open + merged PRs** — match on title, PR number, author, repo, source branch. The dashboard already holds all open PRs client-side (`usePullRequests`), so an in-memory index covers open PRs with zero backend work.
- **History & comments (phase 2)** — server-side search endpoint. Postgres full-text search over `PullRequest` (title/description) and `Comment` (body) with a `SearchController` → MediatR query. Merged PRs beyond the client's page are also only reachable this way.

### Actions, Not Just Navigation

Reuse the `MenuItem[]` action model from `ContextMenu` so the palette executes, not only jumps:

- PR-scoped (when a PR result is highlighted): Open detail, Copy link, Pin/Unpin, Merge, Change priority.
- Global: Create PR from branch…, Refresh dashboard, Open settings, Toggle density.
- Prefix filtering for muscle memory: `>` for actions, `#` for PR numbers, `@` for people (PRs authored by / reviewing).

### Keyboard Shortcuts Layer

The palette is the entry point for a broader keyboard-first pass:

- `j`/`k` — move through PR rows on the dashboard, `o`/`Enter` — open selection
- `g d` — go to dashboard, `?` — shortcut cheat-sheet modal
- Shortcuts suppressed while a Tiptap editor or input has focus

## Open Questions

- **Client vs server index** — start client-side (open PRs only) and add the server endpoint when history/comment search proves necessary? Client-side first is nearly free.
- **Search scope default** — open PRs only, or include merged? Merged is where "where did we discuss X" questions usually point.
- **Shortcut discoverability** — `?` cheat sheet vs. footer hints vs. onboarding toast?
- **Conflict policy** — single-letter shortcuts must check focus state carefully; editors and the diff viewer capture keys too.
- **Palette library vs. hand-rolled** — hand-rolled matches the no-new-dependency pattern of `ContextMenu`; a library (e.g. vue-command-palette style) would save fuzz-scoring code.
