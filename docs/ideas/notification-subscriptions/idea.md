# Notification Subscriptions & Digests

## Problem

Notification preferences today are a single global layer per event type (`NotificationPreferences` JSON on `UserPreferences`: prCreated, reviewAdded, commentAdded, … plus quiet hours). That binary produces two familiar failures at org scale:

- **Notification floods** — a high-traffic repo (CI bots, dependency bumps) fires `prCreated`/`checkFailed` at everyone; users respond by muting notifications globally and then miss the ones that mattered.
- **No catch-up mode** — everything is push-in-the-moment (browser notifications via `useBrowserNotifications`, plus the admin Teams webhook). Miss a day and there's no "what happened while I was away" summary; the unread-count badge is a count, not a story.
- No per-repo or per-PR control exists anywhere: no "mute this spammy PR", no "only my team's repos".

## Rough Approach

### Subscription Model

Per-user subscription decisions, evaluated *before* any notification fires:

| Level | Options |
|---|---|
| Global default | `all` / `involved` (author, reviewer, requested reviewer, mentioned) / `custom` |
| Per-repo override | watch / default / mute |
| Per-PR override | watch / default / mute |

- Storage: extend the `NotificationPreferences` JSON blob (consistent with quiet hours living there); `involved` is computable from existing `PullRequest`/`Review`/requested-reviewer data already synced.
- Careful boundary with `pr-snooze`: snooze defers *attention*; subscriptions shape *notifications*. Keep them separate settings but document the interaction (a muted PR ignores snooze-wake notifications entirely).

### Evaluation Pipeline

- Today notifications are triggered client-side off SignalR events. The cleanest cut: a `NotificationFilterService` (client-side composable initially) that takes (event, PR, user subscriptions) → fire/suppress. The backend Teams path applies the same rules server-side so both channels agree.

### UI

- Bell toggle on `PRRow` quick-actions and on `PRDetail` (watch/mute this PR, one click).
- Notification settings panel gains a repo list with per-repo watch/mute and the global-default selector.
- Muted PRs show a small muted-bell icon on the row — suppressed ≠ invisible.

### Daily Digest

- **In-app "While you were away"** panel — on first dashboard load of the day (or after N hours idle), a dismissible summary: PRs opened/merged involving me, reviews received on my PRs, threads resolved, CI failures on my PRs. Built entirely from data the client already has via SignalR + the DB.
- **Teams digest (phase 2)** — a scheduled morning card to the existing `TeamsWebhookUrl` channel: yesterday's activity rollup. No new infrastructure needed since the webhook integration already exists.
- **Email** — explicitly deferred: no email-sending infrastructure exists in the codebase, and adding one is its own project (see open questions).

## Open Questions

- **Default mode for existing users** — switching the effective default to `involved` changes behavior for everyone on upgrade; safe rollout = keep `all` as default and prompt users to choose?
- **Where does filtering live** — client-side only is simple but inconsistent across devices/browsers; server-side filtering requires routing notifications through the backend per user. Start client-side, migrate?
- **Digest timing** — fixed morning time vs. idle-based? Timezone handling for the Teams digest (single org-wide time?)
- **Quiet hours interplay** — should quiet-hours-suppressed notifications roll up into the next digest, or vanish as they do today?
- **Digest channel priorities** — is the Teams card actually wanted, or does the in-app panel cover the need with less noise?
