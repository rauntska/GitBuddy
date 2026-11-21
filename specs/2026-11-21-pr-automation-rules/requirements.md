# PR Automation Rules v1 — Requirements

Source idea: `docs/ideas/pr-automation-rules/idea.md` (see its **Decisions** section).

## Scope

### Included
- A single rule engine, `AutomationRuleEngine.EvaluateAsync(TriggerContext)`, that evaluates enabled rules when GitHub webhook events arrive: trigger → conditions → ordered actions.
- **Webhook-driven triggers only**: `PROpened`, `PRUpdated`, `LabelAdded`, `LabelRemoved`, `ReviewSubmitted`, `CheckRunCompleted`, `ChecksStatusChanged`, `MergeableStateChanged`.
- **Fixed conditions**: label name, check name/conclusion, all required checks passed, consecutive failure count, approval count, has requested reviewers, author pattern, base branch, changed-file glob, PR size threshold.
- **Fixed actions**: `AddComment`, `AddLabel`, `RemoveLabel`, `SetLabelExclusive`, `RequestReview` (user | team; `"suggested"` see Context), `PostTeamsMessage`, `SetPriority`.
- Per-rule **dry-run**, **loop prevention**, **rate limiting** (minimum interval), and an **AutomationRunLog** for audit.
- Admin-only CRUD + run-log API, and an **Automations** tab in Settings (rule list, builder form, per-rule log).
- Pre-built **templates** for the v1 use cases: #1, #2, #3, #6, #7, #9–#13 (see idea doc). A template pre-fills the builder; it is not a separate engine path.
- New webhook handling for `check_run`, and storing PR labels and base branch.
- Extending `RepositoryRuleService` to store **required check names** so "all required checks passed" works as decided.

### Not included (deferred)
- `Scheduled` triggers and everything depending on them (stale PRs).
- `ReadyForReview` trigger and use cases #4, #5.
- Quality-gate templates (#15–#17), dependency-bot templates (#18), personal per-user rules (#20, #21).
- A generic condition/action DSL.
- Retrofitting `bot-pr-automation` / `stale-pr-management` (they become templates on this engine later).

## API contract

All endpoints: `[Authorize(Roles = "Admin")]`, controller `AutomationRulesController`, route prefix `/api/admin/automation-rules`.

| Method | Route | Request | Response |
|---|---|---|---|
| GET | `/` | `?repository=` (optional) | `AutomationRuleDto[]` |
| GET | `/{id}` | – | `AutomationRuleDto` / 404 |
| POST | `/` | `UpsertAutomationRuleRequest` | 201 `AutomationRuleDto` / 400 on validation errors |
| PUT | `/{id}` | `UpsertAutomationRuleRequest` | `AutomationRuleDto` / 404 / 400 |
| DELETE | `/{id}` | – | 204 / 404 |
| POST | `/{id}/enabled` | `{ enabled: bool }` | `AutomationRuleDto` |
| GET | `/{id}/runs` | `?take=50&skip=0` | `AutomationRunLogDto[]` |
| GET | `/templates` | – | `AutomationRuleTemplateDto[]` |
| GET | `/catalog` | – | triggers, condition types, action types (drives the builder UI) |

`UpsertAutomationRuleRequest`: `repositoryFullName?`, `name`, `enabled`, `dryRun`, `triggerType`, `minIntervalSeconds`, `conditions[]` (`{ type, value }`), `actions[]` (`{ type, params }`). The server validates the trigger/condition/action combination against the fixed catalog and rejects unknown types.

## Data model

- `AutomationRule`: Id, RepositoryFullName (null = org-wide), Name, Enabled, DryRun, TriggerType, Conditions (JSON), Actions (JSON), MinIntervalSeconds, CreatedBy (FK User), CreatedAt/UpdatedAt, LastTriggeredAt, TriggerCount.
- `AutomationRunLog`: Id, AutomationRuleId (FK, cascade), PullRequestId (FK), TriggeredAt, ConditionsMatched, DryRun, ActionsTaken (JSON summary), Error.
- `PullRequest`: add `BaseBranch`, `Labels` (stored list), and requested reviewers if not already derivable.
- `RepositoryRule`: add required check names (JSON or comma-separated, matching local convention).

## Decisions

- **Webhook-only v1, admin-only authoring, fixed catalog, engine first** — carried over from the idea doc.
- **Required checks: both.** Default to required check names synced from branch protection / rulesets; a rule may supply an override list; if neither exists, fall back to all checks on the PR.
- **Check events:** add `check_run` handling to `GitHubWebhookProcessor`; `ChecksStatusChanged` is derived from stored `CheckRun`s and fires only when the aggregate state of the PR changes.
- **Action identity: the rule creator's stored OAuth token** (`User.AccessToken`), as `GitHubService` already does for user-initiated writes. If the token is missing or revoked, the action fails and is recorded in `AutomationRunLog.Error`; the rule is not auto-disabled in v1.
- **Scope:** backend + admin API + Settings UI in this spec, with templates seeded for the v1 set.

## Context

- Mirror `RepositoriesController` / `AdminController` conventions (`[Authorize(Roles = "Admin")]`, `BaseController`).
- Engine entry points: `WebhookService` handlers (called from `GitHubWebhookProcessor`). Evaluation must not block the webhook response for long and must use a fresh DI scope (see the `serviceScopeFactory` use in `WebhookService`).
- **Loop prevention:** actions performed by automation produce webhooks that come back to us. Record an idempotency key (rule, PR, action, target) and ignore events caused by our own actions (e.g. `AddLabel` → `LabelAdded` must not re-trigger the same rule).
- **Gaps found in the codebase that the plan must close:**
  - `GitHubWebhookProcessor` has no `check_run` handling.
  - `PullRequest` has no labels or base branch stored.
  - `RepositoryRule` stores only `RequiresStatusChecks` (bool), no required check names.
  - `ITeamsNotificationService` only has `SendNudgeAsync(pr, reviewers, nudgedBy)`; `PostTeamsMessage` needs a general method with author / reviewers / channel recipients.
  - `CommentTemplate` has **no** token interpolation (the idea doc assumed `{{author}}` etc. exist). v1 introduces a small template renderer (`{{author}}`, `{{prTitle}}`, `{{prUrl}}`, `{{reviewers}}`, `{{checkName}}`) used only by automation actions.
- **Open questions:**
  - `RequestReview("suggested")` depends on the unbuilt reviewer-suggestions idea. Proposal: v1 supports user and team only; template #1 uses an explicit team/user until suggestions exist.
  - Settings already has a `GitHubAppPanel`; if actions should later run as the GitHub App instead of a user token, revisit the identity decision.
  - Requesting team reviews needs org-level permission on the creator's token.

## Implementation notes (deviations from the plan above)

- **Failure streaks:** the refresh path deletes CheckRun rows from older commits, so "failed N times in a row" cannot be derived from CheckRun history. A small `CheckFailureStreak` table (PR + check name + count) is updated from `check_run` webhooks instead.
- **Teams recipients:** Teams is a single channel webhook. `author` and `reviewers` recipients add a "To: ..." line to the channel message; they do not send direct messages.
- **Run log volume:** matched runs are always logged. Non-matches are logged only for dry-run rules, to avoid one row per rule per webhook.
- **PROpened / PRUpdated** fire after the per-PR background refresh so conditions see current files, checks and mergeable state. **MergeableStateChanged** is detected by comparing mergeable state before and after that refresh (GitHub sends no webhook for it), so it only fires for PRs refreshed after a webhook event.
- **Labels** are stored on `PullRequest.Labels` from PR webhooks. `SetLabelExclusive` reads live labels from GitHub before changing them.
- **GitHub App webhook subscription:** the app must be subscribed to the `check_run` event for `ChecksStatusChanged` and `CheckRunCompleted` to fire.
- There is no test project in the repo, so no automated tests were added; pure matching logic (globs, comparisons, check aggregation, template rendering, catalog validation) was checked with a throwaway console project that was not committed.
