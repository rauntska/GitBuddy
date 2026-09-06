# CI Failure Triage — Inline Logs & Annotations

## Problem

`CheckRun` rows carry only `Name`, `Url`, `StartedAt`, `CompletedAt` — a name and a link. When CI fails:

- The reviewer/author must leave GitBuddy, open GitHub, find the failing job, and dig through log noise — for every PR, every failure.
- `PRDetail`'s checks panel and the `CIBadge` say *that* CI failed, never *why*. The most common triage answers (lint error, flaky test, compile error on line X) are one API call away and not surfaced.
- Failed CI just sits there — there's no re-run from GitBuddy, even though re-running a flaky job is the single most common CI action.

The `check_run` webhook event is already processed (`GitHubWebhookProcessor`), so the data trigger already exists.

## Rough Approach

### Enrich CheckRun data

- Add `Status`/`Conclusion` (success, failure, cancelled, skipped…) and `DetailsUrl` to `CheckRun`, synced alongside the existing fields in `CacheService`.

### Annotations first (cheap, structured, precise)

- GitHub's REST `GET /repos/{owner}/{repo}/check-runs/{id}/annotations` returns structured problems: message, severity, **file path + line range**.
- Surface in two places:
  - **Checks panel** — failing check expands to its annotation list (error message, file, line; click → jumps into the diff viewer at that file/line).
  - **Inline in `FileDiffViewer`** — annotations anchored to their path/line render as a red gutter marker on the exact diff line, GitHub-review style. This turns "CI failed" into "this line broke it".

### Raw logs second (the messy fallback)

- For failures without annotations (e.g. test runners), fetch job logs via the Actions jobs API on demand — never eagerly; logs are big.
- Render in a collapsible monospace pane under the failing check, with auto-scroll to first `error`/`failed` match and basic level-based coloring. Client-side fetch through the existing auth'd proxy pattern.

### Re-run actions

- Per-check "Re-run failed jobs" / "Re-run all" buttons on the checks panel (REST re-run endpoints, acting-user token), with the action racing against the next `check_run` webhook → `CheckRunsUpdated` SignalR broadcast, so the UI updates live.

## Open Questions

- **Annotations API limits** — max 50 per check run via REST; is per-annotation pagination needed in practice?
- **Log retention/auth** — Actions log download is a redirect to a signed blob; proxy it server-side (like `ImagesController.proxy`) or short-TTL link?
- **Non-Actions checks** (third-party CI like CircleCI via check runs) — annotations still work when providers emit them; logs won't. Graceful "open in provider" fallback.
- **Flaky detection** — with `Conclusion` history, could GitBuddy recognize "this job failed then passed on re-run with no new commit" and tag it as flaky? Nice-to-have.
- **Overlap boundary** — `pr-quality-gates` concerns *which* checks are required for merge; this idea is about *diagnosing* failing checks. Keep them separate.
