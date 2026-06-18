# Stale PR Management & Automated Reminders

## Problem

The dashboard already shows some staleness signals — `PRGroup.vue` pulses when "AwaitingReview" PRs are older than 1 day, and `prHelpers.ts` color-codes PR age (yellow at 3 days, red at 7 days). But these are passive visual hints that require the user to be looking at the dashboard. Nobody gets notified when a PR goes stale, and there's no mechanism to nudge reviewers or authors into action.

Common stale PR scenarios with no current handling:

- **No review activity** — A PR sits in "AwaitingReview" for days because nobody picks it up. The author doesn't know if reviewers are busy or just haven't noticed it.
- **Unresolved review threads** — A reviewer left comments weeks ago, the author never addressed them, and both sides forgot.
- **CI failures ignored** — Checks fail on a PR and nobody re-runs or fixes them. The PR sits in a broken state indefinitely.
- **Merge conflicts** — A PR falls behind the target branch and gets conflicts, but nobody rebases.
- **Abandoned drafts** — Draft PRs that haven't been touched in weeks, cluttering the dashboard.

The current `PRRefreshService` syncs data every 5 minutes but doesn't evaluate staleness or trigger any actions. The `NotificationPreferences` model already has `quietHours` support but nothing to send notifications *about*.

## Rough Approach

### Staleness Detection

A new background service (`StalePRDetectionWorker`) that periodically evaluates PRs against configurable staleness rules:

**Staleness categories:**

| Category | Criteria | Example Threshold |
|----------|----------|-------------------|
| No review activity | PR in "AwaitingReview" with no reviews | > 2 days |
| Stale review threads | Unresolved threads with no new comments | > 5 days |
| Stale after changes requested | "ChangesRequested" status with no new commits | > 4 days |
| CI failure unresolved | Check runs failing with no new commits/pushes | > 1 day |
| Merge conflict | `MergeableState = "conflicting"` | > 2 days |
| Abandoned draft | Draft PR with no activity | > 14 days |
| General inactivity | No comments, reviews, or commits on any open PR | > 7 days |

All thresholds configurable per-repository or org-wide, with sensible defaults.

**Detection logic:**

- Run every 30 minutes (configurable), independent of `PRRefreshService`
- Evaluate against the already-synced PR data in the database — no additional GitHub API calls needed
- Compare against `PullRequest.UpdatedAt`, review timestamps, thread timestamps, and `CheckRun` statuses already tracked
- Tag detected PRs with their staleness reasons for display and action targeting

### Automated Actions

Configurable rules that fire when staleness is detected:

**Nudge actions:**

- **Auto-comment**: Post a configurable reminder comment on the stale PR (e.g. "This PR has been awaiting review for 3 days. @reviewer, could you take a look?")
- **Re-request review**: Automatically re-request review from assigned reviewers after a configurable period
- **Ping author**: Notify the PR author that their PR needs attention (e.g. unresolved threads, merge conflicts)
- **Escalation**: After a secondary threshold, notify a team lead or add a specific reviewer

**Cleanup actions (opt-in):**

- **Stale label**: Automatically add a "stale" label to PRs meeting staleness criteria
- **Close warning**: Comment a warning that the PR will be closed if no activity occurs within N days
- **Auto-close**: After the warning period elapses with still no activity, close the PR (disabled by default — must be explicitly enabled per repo)

**Rate limiting:**

- Don't send duplicate reminders — track when the last reminder was sent per PR (`LastRemindedAt`)
- Minimum interval between reminders (configurable, default 24 hours)
- Respect `NotificationPreferences.quietHours` — don't send during quiet hours, queue for after

### Notification Channels

Extend the existing notification infrastructure:

**In-app (SignalR):**
- New notification types: `StalePRDetected`, `StalePRReminder`, `StalePRWarning`
- Notifications appear in real-time for users viewing the dashboard
- Badge count on the "Stale" smart view (if saved filters feature is implemented)

**Email (new):**
- Add email notification infrastructure:
  - `IEmailService` interface with SMTP implementation
  - SMTP configuration in `appsettings.json` (host, port, credentials, from address)
  - HTML email templates for stale PR reminders
- Users opt in via `NotificationPreferences` (extend existing structure)
- Daily digest option: one email per day summarizing all stale PRs instead of per-PR emails
- Respect quiet hours — batch and send after quiet period ends

**Browser push (new):**
- Extend the existing browser notification setup (already partially in `useSignalR.ts`) to include stale PR alerts
- Clicking the notification navigates to the PR detail

### Stale PR Dashboard View

A dedicated view within the dashboard for stale PR management:

- **Stale PR list**: All currently stale PRs grouped by staleness reason (no review, unresolved threads, CI failure, etc.)
- **Severity indicators**: Color-coded staleness levels (yellow = approaching threshold, red = well past threshold, orange = warning issued)
- **Quick actions**: Buttons to manually nudge, re-request review, or dismiss the staleness alert for a PR
- **Trend chart**: Show stale PR count over time (are we getting better or worse?)
- **Dismiss/snooze**: Allow users to dismiss a stale alert with an optional snooze period ("remind me again in 2 days")

### Data Model Sketch

```
StalenessRule
├── Id                    (int, PK)
├── RepositoryFullName    (string?, null = org-wide default)
├── Category              (enum: NoReviewActivity, StaleThreads,
│                                StaleAfterChangesRequested, CIFailure,
│                                MergeConflict, AbandonedDraft, Inactivity)
├── ThresholdDays         (double)
├── Enabled               (bool)
├── Actions               (string, JSON — list of actions to take)
├── CreatedAt             (DateTime)
├── UpdatedAt             (DateTime)

StalePRRecord
├── Id                    (int, PK)
├── PullRequestId         (int, FK)
├── Category              (enum — same as above)
├── DetectedAt            (DateTime)
├── LastActionAt          (DateTime?)
├── LastRemindedAt        (DateTime?)
├── WarningIssuedAt       (DateTime?)
├── IsDismissed           (bool)
├── DismissedBy           (int?, FK to User)
├── DismissedAt           (DateTime?)
├── SnoozedUntil          (DateTime?)

NotificationLog
├── Id                    (int, PK)
├── UserId                (int, FK)
├── StalePRRecordId       (int?, FK)
├── PullRequestId         (int, FK)
├── Channel               (enum: InApp, Email, BrowserPush)
├── Type                  (string, e.g. "StalePRReminder")
├── Title                 (string)
├── Body                  (string)
├── SentAt                (DateTime)
├── Status                (enum: Pending, Sent, Failed)
├── Error                 (string?)
```

### Integration Points

- New `StalePRDetectionWorker` background service (follow pattern of `RepositoryRuleSyncWorker`)
- New `StalenessService` for detection logic and rule evaluation
- New `ReminderService` for action execution (comments, re-requests, labels)
- New `IEmailService` / `SmtpEmailService` for email notifications
- Extend `INotificationService` with stale PR notification methods
- Extend `NotificationPreferences` with stale PR notification settings and daily digest toggle
- Extend `PullRequestStatusService` or add staleness as a separate dimension
- New API endpoints: `GET /api/stale-prs`, `POST /api/stale-prs/{id}/dismiss`, `POST /api/stale-prs/{id}/nudge`, `GET/POST/PUT/DELETE /api/staleness-rules`
- New frontend view: `StalePRDashboard.vue` or section within existing Dashboard
- Extend `useSignalR.ts` to handle stale PR notification events

## Open Questions

- **Granularity**: Should rules be per-repository, per-team, or org-wide only? Per-repo is most flexible but creates more config surface. Start with org-wide defaults + per-repo overrides.
- **Comment identity**: When auto-commenting on stale PRs, should the bot use the GitHub App identity or a specific bot account? GitHub App is cleaner (shows "app" badge) but requires app installation.
- **Email provider**: SMTP is simplest but deliverability can be poor. Should we also support SendGrid/Mailgun/etc.? Start with SMTP, make `IEmailService` swappable.
- **Digest vs. per-PR**: For email, should the default be one email per stale PR or a daily digest? Digest is less noisy but per-PR is more actionable. Offer both, default to digest.
- **Historical tracking**: Should `StalePRRecord` track the full history (detected → reminded → warned → closed) for analytics, or just current state? Full history is better for the analytics feature (already in ideas) but more storage.
- **Merge with analytics**: There's overlap with the PR Analytics idea (bottleneck detection, time-to-first-review metrics). Should staleness detection feed into analytics, or stay separate? Feed into analytics — `StalePRRecord` provides the raw events.
- **Conflict with bulk operations**: The Quality Gates & Bulk Operations idea proposes bulk actions. Should stale PR nudges use that bulk infrastructure? Yes — reminder actions should go through the same bulk operation path.
