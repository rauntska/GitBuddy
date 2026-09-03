# Merge Digest & Release Notes

## Problem

GitBuddy's attention stops at the merge button. Everything downstream of it is unserved:

- `GET /api/pull-requests/merged` exists and the data is rich (title, body, author, labels, files, review history), but the only consumer is a list view. Nobody can answer "what shipped in `api-service` last week?" without reading rows.
- Writing release notes is a manual archaeology exercise across GitHub's compare view and a pile of PR titles — done by hand, every release, in every team.
- The shipped analytics feature measures the *process* (latency, throughput, reviewer load). Nothing describes the *product*: what actually changed, for whom.
- Stakeholders who do not review code (support, PM, QA) have no reason to open GitBuddy at all today. A readable "what shipped" digest is the one artifact they would actually want.

## Rough Approach

### Range Selection

- Pick a repo plus a range: two dates, two tags, or "since the last generated digest". Tags require a GitHub tags/releases call — new territory for `GitHubService`, but a small one.
- Resolve to the set of PRs merged into the target branch in that window. `PullRequest` already carries merge state and target branch, so date-range digests need no new GitHub calls at all; tag ranges need one compare.

### Grouping and Titles

- Group by label first (`feature`, `fix`, `chore`), falling back to conventional-commit prefixes parsed from the PR title, then "Other". Both signals are already in the database.
- Each entry: PR title, number, author, link. Bot PRs (see `bot-pr-automation`) collapse into a single "Dependency updates (14)" line rather than 14 entries.
- Optional per-PR "release note" override — a line the author writes for humans, stored on `PullRequest` and editable from `PRDetail`. Titles written for reviewers rarely read well for users.

### Output

- A digest view in the app with copy-as-markdown, plus a plain-text form for pasting into Teams (the existing `TeamsNotificationService` webhook is a natural delivery channel).
- Optionally publish as a GitHub Release body — a write action, so opt-in and confirmed, never automatic.
- Scheduled variant: a weekly org-wide "what shipped" digest, reusing whatever scheduling exists for `PRRefreshService`-style background work.

### Contributor Credit

- Since reviews are tracked, digests can credit reviewers as well as authors. Small touch, disproportionate goodwill.

## Open Questions

- **Squash vs. merge commits** — is "PRs merged into branch X in window" reliable enough, or does it need a real commit-range walk to catch direct pushes and cherry-picks?
- **Tags and releases** — is tag-range support worth the new GitHub surface for v1, or are date ranges sufficient?
- **AI summarisation** — the `ai-review-assistant` idea introduces a model dependency; rewriting PR titles into user-facing notes is the same dependency for a different purpose. Should the two share one provider abstraction?
- **Multi-repo digests** — a feature usually spans repos. Group by repo, or interleave? Ties into `linked-issues`, which would let entries group by work item instead of by PR.
- **Who can publish** — anyone, or admins only, given publishing writes to GitHub?
- **Storage** — persist generated digests (so "since last digest" works and history is browsable), or regenerate on demand every time?
