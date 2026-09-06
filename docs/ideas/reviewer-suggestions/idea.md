# Smart Reviewer Suggestions

## Problem

Every PR starts in "AwaitingReview" partly because assigning the right reviewer is guesswork:

- `ReviewerManager` lets you *pick* reviewers, but offers no hint who fits — you mentally replay "who knows this part of the codebase?" every time.
- PRs opened with zero reviewers (common when authors aren't sure) sit until someone notices them organically.
- Review load is invisible at assignment time: the one person who knows the module is also the person with 9 PRs already waiting on them — the reviewer-latency data in the analytics panel proves it after the fact, but nothing uses it *before* assignment.

The data to answer "who should review this?" already exists in the database: every `Review` (who reviewed what, when), every `Comment` and `ReviewThread` (who commented on which `Path`), collaborators, and analytics' reviewer-load stats.

## Rough Approach

### Suggestion Sources (scored, blended)

- **Code familiarity** — people who previously reviewed or commented on the same files/paths (`Comment.Path`, `ReviewThread.Path` history per user; joined per repo).
- **Repo familiarity** — review frequency per user per repo, from synced `Review` history.
- **CODEOWNERS respect** — fetch the repo's CODEOWNERS file and include owners for changed paths as a strong signal (GitHub's own answer, blended rather than absolute, so a better-fitting non-owner can outrank).
- **Availability / load** — penalize users with many open PRs awaiting *their* review right now (the analytics reviewer query already computes this), and users currently in quiet hours / on vacation marker if one ever exists.

### Surfacing

- **In `ReviewerManager`** — a "Suggested" chip row above the picker: top 3 candidates with a one-line reason ("reviewed 12 of these files", "CODEOWNERS: `/src/api`", "light load: 1 open review"), one click to add.
- **Empty-reviewer hint** — PRs sitting in AwaitingReview with no reviewers get a dashboard-level suggestion (in `PRDetail`, or the nudge flow suggesting "add Sarah?").
- **Nudge integration** — the existing nudge action can prefer suggesting an under-loaded suggested reviewer instead of just poking already-assigned ones.

### Trust & Transparency

- Every suggestion carries its reason; no silent algorithmic ranking. This is a *hint in the UI*, never an auto-assignment — auto-assigning reviewers without opt-in burns trust fast.

## Open Questions

- **Commit-history signal** — file *authorship* (git blame) is the strongest familiarity signal but needs commit data we don't sync; is review/comment history a good enough proxy, or worth one new sync pass (commits-by-path, cached with TTL)?
- **Staleness weighting** — a review 18 months ago shouldn't count like last week's; time-decay on the familiarity score.
- **Team-level suggestions** — GitHub supports team reviewers (already handled by the reviewer commands); suggest teams when individual signals are weak?
- **Cold start** — new repos with no review history: fall back to CODEOWNERS + collaborators, order by recency of any activity?
- **Measurement** — if a suggestion is accepted and the review lands fast, did we help? The analytics data can actually answer this — worth instrumenting suggestion acceptance.
