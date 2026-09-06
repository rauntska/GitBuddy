# Performance at Scale — Large Diffs & Long Lists

## Problem

Both primary surfaces render everything they know about, all at once. That works for a demo org and degrades badly at real volume:

- **`GET /api/pull-requests` has no pagination.** It returns every open PR with its reviews, check runs, and reviewer sets. At a few hundred open PRs across an org this becomes a multi-megabyte payload on every dashboard load and every refresh.
- **No list virtualization.** `PRGroup` renders a `PRRow` per PR into the DOM. Several hundred rows, each with badges, avatars, and hover transitions, is a slow first paint and a janky scroll.
- **Diffs render fully.** `FileDiffViewer` walks every hunk of every file, and `diff-line-row` syntax-highlights via Prism per line. A 4,000-line generated file or a lockfile change locks the tab. There is a `diff-minimap`, which implies long diffs are expected.
- **`FileDiff` rows store full patches.** They are loaded with the PR detail regardless of whether the user opens that file.
- **Unbounded growth.** `DeleteOldPRs` is an opt-in flag on `GitHubConfig`; left off, `PullRequest`, `Comment`, `FileDiff`, and `CheckRun` grow without limit, and the queries above get slower with the org's entire history.

None of the existing 24 ideas touch this — several (`saved-filters`, `command-palette`, `pr-analytics`) add *more* load on top of it.

## Rough Approach

### API Shape

- Paginate and slim the list endpoint: a lightweight row DTO (what `PRRow` actually renders) with cursor pagination, and heavier detail fetched on demand. Groups can fetch independently since the dashboard already thinks in groups.
- Move `FileDiff` patch bodies out of the PR detail payload — send the file list with stats, fetch each patch when a file is opened. `GET /api/pull-requests/{id}/files/{*filePath}` already establishes the per-file pattern.
- Audit indexes on the hot paths: PR state + target repo, comments by PR, `FileDiff` by PR + path, `UserFileViewedState` by user + PR.

### Rendering

- Virtualize long lists (dashboard groups, file tree, diff lines). Prefer a small purpose-built windowing composable over a dependency — the row heights are known per density mode, which is exactly the easy case.
- Collapse-by-default heuristics for large or generated files (lockfiles, `*.min.*`, minified bundles, over N lines) with an explicit "load anyway".
- Highlight lazily: syntax-highlight only visible lines, and skip highlighting entirely above a size threshold. Prism work is currently unconditional.

### Retention

- Make retention explicit and safe: keep merged/closed PRs for a configurable window, prune `FileDiff` patches earlier than the PR rows themselves (they are the bulk of the bytes and the least re-read).

## Open Questions

- **Where does it actually hurt first?** This should start with measurement — payload sizes, query timings, render profiles on a seeded large org via `SampleDataSeeder` — not with optimisation. Which surface breaks first is a guess right now.
- **Pagination vs. groups** — the dashboard groups by status; does pagination happen per group, or globally with client-side grouping (which needs the whole set anyway)?
- **Real-time interaction** — SignalR pushes updates for PRs that may not be in the loaded page. Does an update for an off-page PR get dropped, or force a refetch?
- **Virtualization vs. `view-density-modes`** — variable row heights across density modes complicate windowing. Fixed height per mode, or measured?
- **Dependency policy** — the frontend has kept its dependency list tight. Is a virtualization library acceptable, or is hand-rolled preferred?
- **Retention defaults** — is deleting old PR data acceptable given `audit-log` and `pr-analytics` both want history? Analytics probably needs aggregates retained past raw rows.
