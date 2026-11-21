# PR Automation Rules v1 — Plan

Work top to bottom; each group should leave the build green.

## 1. Domain
1. `AutomationRule`, `AutomationRunLog` in `GitBuddy.Domain/Models/`; enums for trigger/condition/action types.
2. Add `BaseBranch`, `Labels` (and requested reviewers if needed) to `PullRequest`; add required check names to `RepositoryRule`.
3. Register entities, relationships and indexes (`RepositoryFullName`, `AutomationRuleId + TriggeredAt`) in `AppDbContext`.

## 2. Migration
1. `dotnet ef migrations add AddAutomationRules` (run from `GitBuddy.Api`).
2. Verify it applies on a clean database and on an existing one.

## 3. GitHub data ingestion
1. Populate labels, base branch and requested reviewers from the GraphQL refresh and webhook handlers.
2. Extend `RepositoryRuleService.SyncRepositoryRulesAsync` to store required check names.
3. Add `check_run` handling in `GitHubWebhookProcessor` → `WebhookService.HandleCheckRunEventAsync` (upsert `CheckRun`, broadcast as needed).
4. Handle `labeled` / `unlabeled` / `synchronize` / `review_requested` PR actions so label and reviewer state stays current.

## 4. Engine
1. `TriggerContext` (trigger type, PR, event facts such as label name / check run / review state, `IsAutomationOriginated`).
2. Condition evaluators (one class per condition type behind a small interface), including aggregate "all required checks passed" (branch rules → rule override → all checks).
3. Action executors (one per action type) calling GitHub with the rule creator's token; dry-run short-circuits to a logged description.
4. `AutomationRuleEngine.EvaluateAsync`: load matching enabled rules (repo-specific + org-wide), apply rate limit and idempotency, evaluate, execute, write `AutomationRunLog`, update `LastTriggeredAt` / `TriggerCount`. An error in one rule never blocks others.
5. `ChecksStatusChanged` / `MergeableStateChanged` detection by comparing before/after state at the point of update.
6. Template renderer for comment/Teams text.
7. Call the engine from `WebhookService` handlers in a background scope.

## 5. Teams
1. Add a general `SendMessageAsync` to `ITeamsNotificationService` with recipient types author / reviewers / channel; keep `SendNudgeAsync` unchanged.

## 6. Admin API
1. `AutomationRulesController` per `requirements.md`, with request validation against the catalog.
2. Templates endpoint returning the v1 template set (#1, #2, #3, #6, #7, #9–#13).
3. Catalog endpoint for the builder UI.

## 7. Real-time
1. Optional: broadcast a rule-run event via `SignalRNotificationService` so the log view updates live. Skip if it adds more than a small change.

## 8. Frontend types and API client
1. `src/types/automation.ts`.
2. Methods in `src/services/api.ts`.

## 9. Frontend composable
1. `useAutomationRules` (list, create, update, delete, toggle, runs, templates, catalog).

## 10. Frontend UI
1. `AutomationsPanel.vue` in `src/components/settings/`, wired into `SettingsNav.vue`, visible to admins only.
2. Rule list (enabled toggle, dry-run badge, last triggered, trigger count).
3. Builder form: trigger → conditions → ordered actions, template picker, dry-run toggle.
4. Per-rule run log view.

## 11. Validation
Follow `validation.md`.
