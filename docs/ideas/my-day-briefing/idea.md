# My Day Briefing

## Problem

The dashboard's main view is shared and objective — every open PR, grouped by repository and status, the same for every user. Personalization today is scattered and partial: `PinnedPrIds` pins individual PRs but doesn't summarize anything, group hiding/reordering (`dashboard-layout-customization`) changes layout, not content, and `pr-snooze`'s proposed "Needs my review" is a filtered slice of the same repo-grouped list — still a table to scan, just a shorter one.

None of these answer, in one glance right after login, "what actually needs me today" across the handful of ways a PR can be *on* a person: reviews they owe, PRs they're waiting on others for, and things quietly going wrong (failing CI, conflicts) on work they care about. Today that synthesis happens in the user's head, every visit.

## Rough Approach

### A synthesized briefing, not another list

A compact section — candidate placement: a collapsible panel pinned above the existing repo-grouped dashboard, not a replacement for it — that reduces state into a few named buckets instead of rows:

- **Blocked on you** — PRs where the user is a requested reviewer with no review submitted yet. This is the same signal `pr-snooze`'s "Needs my review" proposes; that idea's bucket should live here rather than as a separate personal filter (see Open Questions).
- **Blocking others** — PRs authored by the user, currently `AwaitingReview` or `ChangesRequested`, where a review-request timestamp shows someone else is waiting on them.
- **At risk** — the user's own or reviewed-by-them PRs with a failing `CheckRun`, a conflicting `MergeableState`, or (once/if it ships) a `stale-pr-management` staleness flag.
- **Recently unblocked** — a review the user was waiting on just landed, or CI just went green — sourced from the same events `useSignalR.ts` already receives (`PRUpdated`, `ReviewAdded`).

Presentation: a single counter row ("3 waiting on you · 2 blocking others · 1 at risk"), each bucket expandable into a short list reusing the existing `PRRow` component — no new row rendering to build or maintain.

### Data sourcing

Entirely derived from data already synced — `PullRequest`, `Review`, `ReviewThread`, `CheckRun` — no new GitHub API calls, the same principle `stale-pr-management`'s detection worker uses. Start as a purely client-side computed composable (`useMyDay.ts`) against the existing `usePullRequests` store; only move computation server-side if it turns out too expensive to do per-client.

## Open Questions

- **Merge with `pr-snooze`** — should "Needs my review" ship as this briefing's "Blocked on you" bucket rather than a separate top-of-dashboard filter, so there's one personal-triage surface instead of two competing ones? Leaning yes.
- **Additive vs. replacing the default view** — replacing `Dashboard.vue`'s landing view is a bigger change (and risk to existing muscle memory) than an additive collapsible panel; start additive, revisit only if usage shows the full list is rarely needed once this exists.
- **"At risk" depends on staleness detection that doesn't exist yet** — ship a reduced v1 (failing CI + conflicts only) and extend once/if `stale-pr-management` lands, rather than blocking this idea on that one.
- **Personal only, or coverage-aware** — is this always "my" data, or does a team lead ever want to view a teammate's briefing (e.g. covering for someone OOO, tying into `reviewer-availability`)? Start single-user; revisit only if asked for.
