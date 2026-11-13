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
├── TriggerType           (enum: PROpened, PRUpdated, ReadyForReview, LabelAdded,
│                                LabelRemoved, ReviewRequested, ReviewSubmitted,
│                                CheckRunCompleted, ChecksStatusChanged,
│                                CommentAdded, MergeableStateChanged, Scheduled)
├── Conditions            (string, JSON — label name, check name/conclusion, all
│                          required checks passed, consecutive failure count,
│                          approval count, author pattern / is-bot, base branch,
│                          changed-file glob, PR size threshold, body contains /
│                          is empty, has requested reviewers, days inactive)
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

`AddComment(template)`, `AddLabel`, `RemoveLabel`, `SetLabelExclusive(group, label)` (swap one label within a group such as `size/*` or `waiting-on-*`), `RequestReview(user | team | "suggested")`, `PostTeamsMessage(template, recipient: author | reviewers | channel)`, `SetPriority`. Comment/message templates reuse whatever token interpolation `CommentTemplates` already supports (`{{author}}`, `{{prTitle}}`, `{{prUrl}}`, `{{reviewers}}`) rather than inventing a second templating syntax.

### Evaluation engine

A single `AutomationRuleEngine.EvaluateAsync(TriggerContext)` called from the places events already surface — `GitHubWebhookProcessor` for webhook-driven triggers, `PRRefreshService` for polled-state-change triggers. This replaces each feature growing its own if/then logic: `bot-pr-automation` and `stale-pr-management`'s "automated actions" sections become pre-built rule templates seeded on top of this engine instead of parallel bespoke code paths.

### Safety

- **Dry-run per rule** — before enabling live, a rule can run in log-only mode (`AutomationRunLog` records "would have added label `stale` to #123") so an admin can sanity-check it against real traffic.
- **Loop prevention** — an action must not be able to re-trigger its own rule (e.g. `AddLabel` firing a `LabelAdded` trigger on the same rule); guard via a per-PR/per-rule idempotency check before acting.
- **Rate limiting** — reuse the same throttling pattern `stale-pr-management` already sketches (`LastTriggeredAt` + minimum interval) so a flapping condition can't spam comments.

### UI

A new "Automations" tab in Settings, admin-scoped like the repository-rules admin view: rule list, a builder form (trigger dropdown → condition fields → ordered action list), and a per-rule execution log for audit.

## Use Cases

Concrete rules the engine should be able to express. Each could ship as a pre-built template. Items marked ⚠️ depend on a trigger or condition that is new in this revision or on another idea.

### Routing and reviewers

1. **Checks pass → request reviewers.** `ChecksStatusChanged` + "all required checks passed" + "has no requested reviewers" → `RequestReview("suggested")`. The "no requested reviewers" condition stops re-requests when later pushes pass CI again. Per-check `CheckRunCompleted` is not enough here, because it would fire on the first green check.
2. **Label → specialist review.** `LabelAdded` (`needs-design-review`, `security`, `db-migration`) → `RequestReview(team)`.
3. **File path → owner.** `PROpened`/`PRUpdated` + changed-file glob `**/Migrations/**` → `RequestReview(user)` + `AddLabel(migration)`. A lighter-weight alternative to CODEOWNERS that can be configured per repo in the app.
4. **Leaving draft → reviewers.** `ReadyForReview` → `RequestReview("suggested")`.
5. ⚠️ **Rebalance after decline.** When a requested reviewer is unavailable, re-request from someone else. Depends on [reviewer-availability](../reviewer-availability/idea.md).

### Labeling and sorting

6. **Size labels.** `PRUpdated` + size thresholds → `SetLabelExclusive(size/*, size/S|M|L)`.
7. **Area labels.** Changed files only under `gitbuddy-vue/**` → `AddLabel(frontend)`; only under `GitBuddy.Api/**` → `AddLabel(backend)`.
8. **Target branch.** Base branch `release/*` → `AddLabel(hotfix)` + `SetPriority(high)`.

### Keeping the author moving

9. **CI failed → notify the author.** `CheckRunCompleted` + conclusion failure + consecutive failure count ≥ N → `PostTeamsMessage(recipient: author)` with the failing check's name. The count threshold avoids spamming on flaky checks.
10. **Changes requested → whose turn.** `ReviewSubmitted` (changes requested) → `SetLabelExclusive(waiting-on-*, waiting-on-author)`. This gives the dashboard a clear "whose turn is it" signal.
11. **Merge conflict.** `MergeableStateChanged` → conflicting: `AddComment("needs rebase")` + `AddLabel(conflict)`; when the conflict clears: `RemoveLabel(conflict)`.

### Keeping reviewers moving

12. **Approved and green → ready to merge.** `ReviewSubmitted`/`ChecksStatusChanged` + approval count ≥ 1 + all required checks passed → `AddLabel(ready-to-merge)` + `PostTeamsMessage(recipient: channel)`.
13. **Re-review after a push.** `PRUpdated` (new commits) + approval count ≥ 1 → `RemoveLabel(ready-to-merge)` + `PostTeamsMessage(recipient: reviewers)`.
14. **Second approval for risky changes.** Changed-file glob `**/Auth/**` + approval count = 1 → `RequestReview(team: security)`.

### Quality gates (warn only)

15. **Empty description.** `PROpened` + body empty or only the PR template → `AddComment` asking for context.
16. ⚠️ **No linked issue.** `PROpened` + body has no `#123` or issue key → `AddComment`. Overlaps with [linked-issues](../linked-issues/idea.md).
17. **Migration without a rollback note.** Migration file changed + body does not contain "rollback" → `AddComment` reminder.

### Bots and maintenance

18. **Dependency bot patch bumps.** Author is a bot + semver patch bump → `AddLabel(deps)` + `RequestReview` from one rotating reviewer, or skip review when checks pass. Absorbs [bot-pr-automation](../bot-pr-automation/idea.md).
19. ⚠️ **Stale PRs.** `Scheduled` + days inactive ≥ N → `AddComment` + `AddLabel(stale)`; close after M more days. Absorbs [stale-pr-management](../stale-pr-management/idea.md). Needs a time-based evaluation loop (see Open Questions).

### Personal automations (if per-user rules are added)

20. Snooze PRs opened by bots until their checks pass.
21. "Notify me on Teams when a PR I approved gets new commits."

### Additions these use cases need

Compared with the first version of this idea, the use cases above needed the following. They are already reflected in the data model and actions catalog:

- **Triggers:** `ReadyForReview`, `ChecksStatusChanged` (aggregate check state, not per check run), `Scheduled` (time-based).
- **Conditions:** all required checks passed, consecutive failure count, approval count, author is a bot, body contains / is empty, has requested reviewers, days inactive.
- **Actions:** `SetLabelExclusive` for mutually exclusive label groups, and a `recipient` on `PostTeamsMessage`.

## Open Questions

- **v1 trigger scope** — start with webhook-driven triggers only, and treat time/staleness-based triggers as a v2 hook-in once `stale-pr-management`'s worker exists (or ships as the first consumer of this engine instead of building its own)?
- **Who can author rules** — admin-only org-wide rules via the existing allowlist/role model, or also personal per-user automations ("auto-snooze PRs opened by dependabot")? Likely two visibility tiers, not one table.
- **Generic engine vs. fixed catalog** — a fully generic condition/action DSL invites scope creep into an internal Zapier; is a small fixed catalog (above) enough for the actual asks, revisited only if a real need outgrows it?
- **Migration path** — if `bot-pr-automation` or `stale-pr-management` ship their own engines first, is retrofitting them onto this one worth the churn, or does this idea only make sense if built *before* either?
