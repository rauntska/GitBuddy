# PR Analytics & Metrics

## Problem

Teams have no visibility into their review pipeline health. Questions like "how long are PRs sitting before someone reviews them?" or "is review load balanced across the team?" require manually checking each PR. There's no way to spot trends, identify bottlenecks, or measure improvement over time.

## Rough Approach

### Data Collection

- Track timestamps on PR lifecycle events: `created` -> `first_review` -> `last_review` -> `merged`/`closed`
- Extend the existing `PullRequest` model with metric fields, or add a dedicated `PRMetrics` table
- A background service (similar to `PRRefreshService`) computes and refreshes metrics periodically

### Analytics View

Add a new "Analytics" section to the frontend with:

- **Velocity charts** — average time-to-first-review, time-to-merge, trends over configurable time windows (7d, 30d, 90d)
- **Team workload** — reviewer distribution chart showing who's reviewing what, flagging overloaded vs. underutilized reviewers
- **Bottleneck detection** — highlight PRs stuck in "Awaiting Review" beyond a configurable threshold (e.g. 48h)

### Data Model Sketch

```
PRMetrics
├── PullRequestId (FK)
├── TimeToFirstReview   (nullable TimeSpan)
├── TimeToMerge         (nullable TimeSpan)
├── ReviewCount         (int)
├── CommentCount        (int)
├── ComputedAt          (DateTime)
```

## Open Questions

- **Scope**: Should analytics be per-user, per-team, or org-wide? Likely all three with a toggle.
- **Time range**: What default windows for trend charts? Start with 7d and 30d.
- **Export**: Should data be exportable (CSV/PDF) or is in-app viewing enough for v1?
- **Privacy**: Do individual reviewer metrics need an opt-out? Some teams are sensitive about performance tracking.
- **Retention**: How long to keep historical metrics data?
