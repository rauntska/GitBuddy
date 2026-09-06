# Bot PR Automation — Dependabot/Renovate Triage

## Problem

A large fraction of any org's open PRs are bot PRs: dependabot, renovate, codecov, translation bots. They pollute every part of the dashboard:

- They sit in "AwaitingReview" next to PRs needing human judgment, inflating the review queue and the unread count.
- They distort analytics (review latency, reviewer workload counts, PR size stats).
- `stale-pr-management` (rightly) wants to nudge *humans* — nudging `dependabot[bot]` is nonsense, so bot PRs need explicit exclusion there.
- Merging them is pure ceremony: CI green + approvals → merge. Reviewers burn clicks on dozens of identical rubber-stamp reviews per week.

## Rough Approach

### Bot Detection

- Author-based: known bot patterns (`dependabot[bot]`, `renovate[bot]`, `*-bot`, GitHub App authors) — detectable from the already-synced author login, plus the `author_association`/app hints available at sync time.
- Configurable list in settings: extra bot patterns per org, and an allowlist of "bots that actually need review".

### Dashboard Separation

- Bot PRs move to a dedicated **"Bot & dependency PRs"** collapsed section (reusing the group show/hide machinery from dashboard layout customization) instead of the status groups — out of the attention path, one click away.
- Excluded from stats (`StatsSummary`), unread counts, and stale detection by default; each exclusion individually toggleable.

### Dependency PR Grouping

- Cluster by dependency: all 14 PRs bumping `npgsql` across repos shown as one row ("npgsql 8.0.4 → 8.0.5, 14 repos") with expand-and-merge-all. Cross-repo batching is something neither dependabot nor GitHub's UI offers — same org-wide advantage as `merge-collision-detection`.

### Auto-merge Policy

- Per-repo (or org) policy: when a bot PR has green required checks and the required approvals are met, **merge it** via the existing merge endpoint, or simply **enable GitHub's native auto-merge** on it and let GitHub do the work (safer — no logic of ours can misfire).
- Mandatory guardrails: version-scope limits (semver-major excluded by default), lockfile-only vs. code changes distinction (dependabot commit messages carry this), daily merge cap, full audit trail of what was auto-merged and why (cross-reference `audit-log` if it lands).

## Open Questions

- **Auto-approve or not** — auto-merging still requires the approvals, so does the org want a designated approver bot account (new credential, new risk) or keep humans in the loop for that one click? Native auto-merge + one human approval may be the sweet spot.
- **Failure handling** — auto-merge candidates whose CI fails: group for human fix? Auto-close? Leave in the bot section with a "broken" filter?
- **Detection false positives** — humans with "bot" in their username are rare but real; the App-author signal is the reliable one, login patterns are the heuristic.
- **Analytics integration** — exclude bots from reviewer/health analytics entirely, or a toggle ("include automation")? Default exclude, almost certainly.
- **Rebase churn** — dependabot rebases itself on conflict; does the bot section need its own conflict indicator, or does `merge-collision-detection` cover it?
