# PR Quality Gates & Bulk Operations

## Problem

Two related pain points:

1. **Invisible merge readiness** — GitHub defines branch protection rules (required approvals, required CI checks, etc.) but you can't see at a glance which PRs meet those requirements without opening each one. There's no aggregated view of gate status across the dashboard.

2. **No multi-PR management** — Managing multiple PRs requires opening each one individually. There's no way to request the same reviewer on 5 PRs at once, add labels in batch, or dismiss stale reviews across a set of PRs.

## Rough Approach

### Quality Gates (GitHub as master)

GitHub is the source of truth for merge requirements. Graphite reads and surfaces those rules — it does not define its own.

**Fetch GitHub's branch protection rules** via the GitHub API (`GET /repos/{owner}/{repo}/branches/{branch}/protection`) and cache them:
- Required status checks (CI)
- Required approving review count
- Require conversation resolution before merge
- Required review dismissals on push

**Surface gate status visually:**
- Icons on `PRRow` — green check (all GitHub requirements met), yellow warning (partial), red X (blocked)
- Gate summary on `PRDetail` showing each GitHub requirement and whether it passes
- Cross-reference existing `CheckRun` data with required checks to show pass/fail per check

**No local policy model needed.** GitHub owns the rules; Graphite just reads and displays them. This avoids drift between GitHub and Graphite configs.

### Bulk Operations

**Multi-select UI:**
- Checkboxes on `PRRow` for multi-select
- Shift-click for range select
- "Select all" toggle in PR group headers

**Floating action bar** appears when PRs are selected:
- Request reviewer(s) on all selected PRs
- Add/remove labels
- Dismiss stale reviews
- Change PR status filters in bulk

**Backend:**
- New `BulkOperationController` endpoint accepting a list of PR IDs + action type + payload
- Rate-limit awareness — batch GitHub API calls with backoff to avoid hitting limits
- Return per-PR success/failure results so the UI can show partial failures

## Open Questions

- **Cache freshness**: How often should we refetch branch protection rules? They change infrequently, so a long cache TTL (hours) likely works.
- **Batch size limits**: What's the max batch size before GitHub API rate limits become a real concern? Start with 25 as a cap.
- **Audit logging**: Should bulk operations be logged for traceability? Useful for compliance but adds complexity.
- **Partial failures**: If a bulk action fails on 3 out of 10 PRs, what's the UX? Toast per-PR or a summary dialog?
- **GraphQL vs REST**: Branch protection rules are available via both — which API gives us the most efficient batch fetch?
