# Automated Test Foundation

## Problem

The repository has no tests. Not a thin suite — none at all: no test project in the solution, no test runner in `gitbuddy-vue/package.json`, no CI workflow running one.

That is load-bearing for everything else in `docs/ideas/`:

- **Silent-failure surfaces.** `PullRequestStatusService` (status derivation), `PriorityService` (scoring), and `GitHubWebhookProcessor` (event fan-out) are pure logic with branchy rules and no coverage. A regression in status derivation shows up as PRs quietly appearing in the wrong dashboard group.
- **Refactoring is expensive.** `large-diff-performance` proposes reshaping the list API; `linked-issues` and `reviewer-availability` add fields to hot paths. Each is a "hope nothing broke" change today.
- **Migrations are unguarded.** Migrations auto-apply on startup via `MigrateAsync()`. Nothing verifies that the schema and model agree before that runs against a real database.
- **Manual verification only.** Every change is validated by running both apps and clicking. That cost is paid on every PR, forever, by a human.

The codebase is unusually ready for tests: services sit behind interfaces (`IGitHubService`, `ICacheService`, `IAnalyticsService`, `INotificationService`, `IWebhookService`), `SampleDataSeeder` already builds realistic fixtures, and the frontend is composable-heavy (`usePullRequests`, `usePRDetail`, `useUserPreferences`) — plain functions returning refs, trivially testable.

## Rough Approach

### Backend

- `GitBuddy.Tests` (xUnit) added to the solution. Start with pure domain logic and no infrastructure: status derivation, priority scoring, PR validation, webhook payload → action mapping.
- Integration tests over EF Core with a real Postgres (Testcontainers) rather than the InMemory provider — the app depends on Npgsql-specific behaviour, and InMemory quietly diverges. One test that runs `MigrateAsync` against an empty database catches the whole class of broken-migration failures.
- Fake `IGitHubService` for anything that would otherwise call GitHub. No test should ever touch the real API.

### Frontend

- Vitest plus `@vue/test-utils`. Composables first (they hold the logic), a handful of component tests for the ones with real branching: `PRRow`, `FileTree`, `CommentsPanel`.
- Mock the axios layer in `src/utils/api` at the module boundary; interceptors are part of the contract worth exercising.

### CI

- A GitHub Actions workflow running `dotnet test`, `npm run build` (which already type-checks via `vue-tsc -b`), and `vitest run` on every PR. This is also the first thing GitBuddy's own quality gates could read (see `pr-quality-gates`).

### Sequencing

- Do not chase coverage. Cover the three services above, add a regression test with every bug fix, and require tests for new logic-bearing code. Retrofitting a whole suite at once produces tests nobody trusts.

## Open Questions

- **Testcontainers vs. a CI service container** — Testcontainers needs Docker locally; a service container is CI-only and leaves local runs unverified. Which cost is preferable?
- **Where is the seam for GitHub?** Are the service interfaces clean enough to fake, or do controllers reach through to Octokit types in places that would need refactoring first?
- **Does `SampleDataSeeder` become the fixture source**, or do tests need their own builders? Sharing it couples test data to demo data.
- **E2E** — is a Playwright smoke test (log in, load dashboard, open a PR) worth the maintenance, or do unit plus integration cover enough?
- **Enforcement** — required CI check on GitBuddy's own repo, and if so, does the team want it blocking from day one?
- **Existing behaviour as spec** — where current behaviour is ambiguous, do tests codify what the code does today, or is that the moment to fix it?
