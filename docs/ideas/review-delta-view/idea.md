# Review Delta View — "Changes Since My Last Review"

## Problem

Reviewing is iterative: the author pushes fixes, the reviewer returns, and today GitBuddy forces them to re-read the *entire* diff and manually spot what moved. The pieces to know better already exist and go unused:

- `Review.SubmittedAt` is tracked per reviewer, but never scopes the diff.
- The diff viewer (`FileDiffViewer`, `FileTree`) always renders the full PR diff.
- `UserFileViewedState` tracks "viewed", which is about eyes, not changes — viewing a file once doesn't mean it hasn't changed since.
- On the dashboard there's no signal for "this PR has new commits since you last looked at it" — reviewers can't triage which of their 15 in-review PRs actually need attention.

GitHub's web UI has a "code changes since last review" toggle; GitBuddy has nothing equivalent.

## Rough Approach

### Data Prerequisites

- **Track head SHA** — add `HeadSha` to `PullRequest`, synced in `CacheService.RefreshPullRequestsAsync` and on webhook pushes. (Not currently stored; branch names alone can't anchor a diff in time.)
- **Snapshot per review** — when a review is submitted, record the head SHA it was submitted against (`Review.ReviewedSha`). Backfill is impossible for history — accept "no delta available" for old reviews.

### Diff Scoping

- Toggle in the file-diff header: **All changes / Since my last review**.
- Delta diff = GitHub compare between `ReviewedSha` and current `HeadSha` (the repositories feature already exercises the compare API for branch comparison, so the Octokit plumbing pattern exists).
- Files unchanged since the review render collapsed/dimmed in the `FileTree` with a "changed" badge on the rest; per-hunk filtering is nice-to-have, per-file is the useful 90%.

### Attention Signals

- **FileTree badge** — dot on files touched since my last review.
- **Dashboard "new for you" marker** — a subtle indicator on `PRRow` when `HeadSha` differs from the SHA of my latest review and I'm a requested reviewer. Distinct from the existing unread-count signal, which is global activity, not *directed at me* activity.
- **Outdated-comment detection** — comments whose anchor predates `ReviewedSha` get an "outdated" style without an extra API call.

### Review Round Picker

If I've reviewed 3 times, "since last review" should default to my most recent review but allow picking an earlier round — a small dropdown next to the toggle. The `ReviewTimeline` component is the natural home for round selection too.

## Open Questions

- **Compare API cost/limits** — compare endpoints get expensive on large deltas; is per-file lazy loading (compare on file open) better than one big compare?
- **Where does the delta computation live** — server-side endpoint returning file-level changed paths (cheap, one compare), with the client fetching full file diffs individually as it already does?
- **Interaction with viewed-state** — should a new push auto-reset "viewed" on changed files so the existing viewed-state tracking stays honest?
- **Deleted reviews** — if my review is dismissed/deleted, fall back to previous round or hide the toggle?
- **No-review case** — for a reviewer who hasn't reviewed yet, could the same machinery show "since I opened this PR detail" (track last-visited SHA per user/PR)?
