# PR Automation Rules

## Problem

Every "when X happens, do Y" need in this project currently gets its own bespoke implementation. `RepositoryRuleService`/`RepositoryRuleSyncWorker` hardcode ruleset syncing; the `bot-pr-automation` idea proposes its own dependency-bot policy engine; `stale-pr-management` proposes its own `StalePRDetectionWorker`, its own `StalenessRule` table, and its own `ReminderService`. Each is a fresh trigger→condition→action pipeline with its own schema, its own controller, and its own admin UI, even though the shape of the problem is identical every time: *some event happens on a PR, some conditions are checked, some actions fire.*

There's no single place a team lead can configure "when a PR is labeled `needs-design-review`, request review from the design team" or "when CI fails twice in a row, ping the author on Teams" without someone writing a new backend feature for it. The event sources already exist — `GitHubWebhookProcessor` receives PR/review/comment/check-run webhooks, and `PRRefreshService` polls for changes — but nothing lets a non-developer wire a condition to an action.

## Rough Approach

### Data model

```
AutomationRule
├── Id                    (int, PK)
├── RepositoryFullName    (string?, null = org-wide)
├── Name                  (string)
├── Enabled               (bool)
├── DryRun                (bool)              — log what would happen, take no action
├── TriggerType           (enum: PROpened, PRUpdated, LabelAdded, LabelRemoved,
│                                ReviewRequested, ReviewSubmitted, CheckRunCompleted,
│                                CommentAdded, MergeableStateChanged)
├── Conditions            (string, JSON — label name, check name/conclusion, author
│                          pattern, base branch, changed-file glob, PR size threshold)
├── Actions               (string, JSON — ordered list, see below)
├── CreatedBy             (int, FK to User)
├── CreatedAt / UpdatedAt (DateTime)
├── LastTriggeredAt       (DateTime?)
├── TriggerCount          (int)

AutomationRunLog
├── Id                    (int, PK)
├── AutomationRuleId      (int, FK)
├── PullRequestId         (int, FK)
├── TriggeredAt           (DateTime)
├── ConditionsMatched     (bool)
├── ActionsTaken          (string, JSON summary)
├── Error                 (string?)
```

### Actions catalog (v1, deliberately fixed — not a generic scripting engine)

`AddComment(template)`, `AddLabel`, `RemoveLabel`, `RequestReview(user | team | "suggested")`, `PostTeamsMessage(template)`, `SetPriority`. Comment/message templates reuse whatever token interpolation `CommentTemplates` already supports (`{{author}}`, `{{prTitle}}`, `{{prUrl}}`, `{{reviewers}}`) rather than inventing a second templating syntax.

### Evaluation engine

A single `AutomationRuleEngine.EvaluateAsync(TriggerContext)` called from the places events already surface — `GitHubWebhookProcessor` for webhook-driven triggers, `PRRefreshService` for polled-state-change triggers. This replaces each feature growing its own if/then logic: `bot-pr-automation` and `stale-pr-management`'s "automated actions" sections become pre-built rule templates seeded on top of this engine instead of parallel bespoke code paths.

### Safety

- **Dry-run per rule** — before enabling live, a rule can run in log-only mode (`AutomationRunLog` records "would have added label `stale` to #123") so an admin can sanity-check it against real traffic.
- **Loop prevention** — an action must not be able to re-trigger its own rule (e.g. `AddLabel` firing a `LabelAdded` trigger on the same rule); guard via a per-PR/per-rule idempotency check before acting.
- **Rate limiting** — reuse the same throttling pattern `stale-pr-management` already sketches (`LastTriggeredAt` + minimum interval) so a flapping condition can't spam comments.

### UI

A new "Automations" tab in Settings, admin-scoped like the repository-rules admin view: rule list, a builder form (trigger dropdown → condition fields → ordered action list), and a per-rule execution log for audit.

## Open Questions

- **v1 trigger scope** — start with webhook-driven triggers only, and treat time/staleness-based triggers as a v2 hook-in once `stale-pr-management`'s worker exists (or ships as the first consumer of this engine instead of building its own)?
- **Who can author rules** — admin-only org-wide rules via the existing allowlist/role model, or also personal per-user automations ("auto-snooze PRs opened by dependabot")? Likely two visibility tiers, not one table.
- **Generic engine vs. fixed catalog** — a fully generic condition/action DSL invites scope creep into an internal Zapier; is a small fixed catalog (above) enough for the actual asks, revisited only if a real need outgrows it?
- **Migration path** — if `bot-pr-automation` or `stale-pr-management` ship their own engines first, is retrofitting them onto this one worth the churn, or does this idea only make sense if built *before* either?
