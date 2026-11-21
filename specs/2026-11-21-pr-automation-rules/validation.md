# PR Automation Rules v1 — Validation

## Automated
- `dotnet build -c Release` passes from repo root.
- `cd gitbuddy-vue && npm run build` passes.
- `dotnet test` if tests exist for touched areas; add unit tests for condition evaluators, "all required checks passed" resolution, loop prevention and rate limiting.

## Correctness checks
- Every endpoint in `AutomationRulesController` is `[Authorize(Roles = "Admin")]`; a non-admin gets 403.
- Invalid trigger/condition/action combinations are rejected with 400.
- The migration applies cleanly via `dotnet ef database update` on a fresh and an existing database.
- Dry-run rules perform **no** GitHub or Teams calls and still write an `AutomationRunLog`.
- An action's resulting webhook does not re-trigger the same rule (e.g. `AddLabel` → `labeled`).
- A flapping condition respects `MinIntervalSeconds`.
- Exceptions in one rule or action do not prevent other rules running or fail the webhook response.
- A revoked or missing creator token produces a logged error, not a crash.
- Required-check resolution: branch protection names used by default; the rule override list wins; no config falls back to all checks.

## Manual walkthrough
1. As admin, open Settings → Automations; as a non-admin confirm the tab is hidden.
2. Create a rule from template #1 (checks pass → request reviewers) in dry-run; open a PR in a test repo; confirm the run log shows "would request review from …" only after all required checks pass, not after the first green check.
3. Turn dry-run off and repeat; confirm the review request appears on GitHub and a second passing push does not re-request when reviewers already exist.
4. Create the size-label rule (#6); push changes crossing a threshold; confirm the old `size/*` label is replaced.
5. Add the label `needs-design-review` with rule #2; confirm the team is requested once.
6. Introduce a merge conflict; confirm #11 comments and labels, then clears the label on resolution.
7. Fail a check repeatedly with #9; confirm the Teams message is sent only after the configured failure count.

Edge cases: org-wide vs repo-specific rule on the same event; disabled rule; deleted rule with existing logs; PR closed before actions run; GitHub API rate-limit or 403; duplicate webhook deliveries.

## Definition of done
- All automated checks pass and the manual walkthrough succeeds.
- v1 templates (#1, #2, #3, #6, #7, #9–#13) can be created from the UI and behave as described.
- Dry-run, loop prevention, rate limiting and the run log are verified.
- `docs/ideas/pr-automation-rules/idea.md` is tagged/updated per `docs/README.md` once implemented.
