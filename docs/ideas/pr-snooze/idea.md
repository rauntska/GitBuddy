# PR Snooze & Personal Triage

## Problem

The dashboard is a shared, objective view — every open PR in every group, the same for everyone. But review attention is personal and time-constrained, and there's no way to manage *your own* list:

- PRs you can't act on yet (waiting on the author, blocked by an upstream team, just not this week's priority) clutter the same groups as the ones you should look at now.
- The only personalization primitives are `PinnedPrIds` (the *opposite* signal — keep at top) and group hiding (removes entire status groups, not individual PRs).
- Notable asymmetry: the stale-pr-management idea is about nudging *other people* into action; nothing manages *my* attention.

Result: experienced users mentally filter the dashboard every single visit — exactly the failure mode `saved-filters` describes, but personal and temporal rather than a recurring saved view.

## Rough Approach

### Snooze Action

- New entry in the quick-actions `ContextMenu`: **Snooze…** with presets — until tomorrow, until Monday, until activity, custom date — plus a reason (optional, free text).
- Snoozed PRs collapse out of their normal groups into a dedicated **Snoozed** section at the bottom of the dashboard (count badge in the header), not fully hidden — the goal is triage, not forgetting.

### Storage

- Follow the `PinnedPrIds` pattern: per-user JSON on `UserPreferences` (`SnoozedPrs`: PR id → until/reason). A dedicated table only becomes interesting if we later want snooze history for analytics.

### Wake Conditions

- **Expiry** — client-side evaluation on dashboard load (until-date passed → unsnooze).
- **Activity wake** — the `useSignalR` handlers already receive `PRUpdated`, `ReviewAdded`, `CommentChanged`; on any of these for a snoozed PR, wake it *if* the snooze mode allows ("until activity" wakes on everything; "until Monday" can be strict or wake early on new reviews — configurable per snooze, default: new review wakes, general comment doesn't).

### My Queue (the payoff view)

- A compact personal filter at the top of the dashboard: **Needs my review** = PRs where I'm a requested reviewer (or on a reviewing team), no review from me yet, not snoozed — i.e. the daily triage list, one glance.
- Cross-link: this can be implemented as a preset smart view if/when `saved-filters` lands; the snooze state then composes with any filter.

## Open Questions

- **Snooze vs. notification mute** — snoozing should almost certainly suppress that PR's browser notifications for me too, which touches the notification-preferences pipeline. Coupled or separate (`notification-subscriptions` proposes watch/mute separately — snooze = temporary attention deferral, mute = standing notification policy)?
- **Who sees snoozes** — purely private per user, or visible ("Rauno deferred this until Monday") for team coordination? Private is simpler and safer for v1.
- **Wake-signal granularity** — is "new review" the right wake line, or should a new *commit* also wake? Probably yes for reviewers.
- **Snooze nagging** — cap consecutive re-snoozes (GitHub's own "snooze" ends up as indefinite deferral in practice) or accept it as the user's call?
- **Server-side evaluation** — expiry on dashboard load only means stale snoozes linger for users who don't reload; is that acceptable (probably — SignalR keeps active sessions fresh)?
