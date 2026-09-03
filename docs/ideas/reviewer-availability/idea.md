# Reviewer Availability & Review Handoff

## Problem

GitBuddy assumes every requested reviewer is at their desk. Nothing in the data model knows otherwise:

- `ReviewerManager` and `POST /api/pull-requests/{id}/reviewers` happily assign a reviewer who is on a two-week holiday. The PR then sits in AwaitingReview until a human notices.
- The nudge action (`POST /api/pull-requests/{id}/nudge`, Teams webhook via `TeamsNotificationService`) pings that same absent person — repeatedly, if `stale-pr-management` automates it.
- There is no OOO/away concept anywhere: `UserPreferences` carries quiet hours inside `NotificationPreferences`, which suppresses *my notifications* but does not tell *anyone else* that I am unavailable.
- No way to hand a review off: a reviewer who cannot get to a PR must remember to remove themselves and find a substitute manually.
- `reviewer-suggestions` answers "who is the right reviewer?" at assignment time from history and load. This idea is the complementary half: who is *available*, and what happens to reviews already assigned to someone who is not.

## Rough Approach

### Availability State

- `UserAvailability` on the user (or a small table for scheduled ranges): `Status` (available / limited / away), optional `From`/`Until`, optional `Message`, optional `DelegateUserId`.
- Set from the user menu ("I'm away until…") and from `SettingsPage`. Admins can set it for others — someone who left abruptly is exactly the case that matters.
- Cheap derived signal as a supplement, not a replacement: no reviews submitted and no comments posted in N days while requested on M PRs → surface a soft "possibly inactive" hint to admins only. Guessing at someone's availability and acting on it automatically would be worse than doing nothing.

### Effects on Assignment

- `ReviewerAvatars` and `ReviewerManager` show an away marker on the avatar plus the return date on hover.
- Assigning an away reviewer is allowed but warns inline ("back Mar 14 — assign anyway?"). Never blocked; people know their own team.
- Availability becomes an input to `reviewer-suggestions` scoring, and away reviewers drop out of automated nudge targeting (`stale-pr-management`, `bot-pr-automation`).

### Handoff

- **Delegate** — an optional standing delegate applied when going away: new review requests routed to them, existing ones flagged for reassignment rather than silently moved. Silent reassignment destroys accountability.
- **Hand off this review** — a per-PR action in `ReviewerManager`: pick a substitute, GitBuddy removes the original requested reviewer and adds the new one via the existing endpoints, and posts an optional explanatory comment.
- **Away digest on return** — "while you were out: 6 PRs requested your review, 3 merged without you". Pairs with the digest machinery in `notification-subscriptions`.

## Open Questions

- **Source of truth** — manual only, or read from an external calendar / HR system later? Manual is the only thing implementable without a new integration.
- **Delegation semantics** — does a delegate *replace* the away reviewer on GitHub, or get added alongside them? Replacement is cleaner for the queue; addition is safer for required-approval rules.
- **Team-level availability** — teams already exist as reviewers (see the separate user/team reviewer handling). Does a team need availability, or only individuals?
- **Trust and privacy** — is "possibly inactive" surfaced to everyone, admins, or nobody? Presence-tracking a colleague is a social cost, not just a feature.
- **Scheduled vs. instant** — support future-dated ranges in v1, or only "away now / back now"?
- **Analytics interaction** — should review-latency metrics exclude periods where the assigned reviewer was away, so the numbers measure the process rather than someone's holiday?
