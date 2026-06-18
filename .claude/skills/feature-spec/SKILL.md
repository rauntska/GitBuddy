---
name: feature-spec
description: Kicks off a new feature by gathering a feature name and short description from the user, creating a git branch, interviewing the user about scope/decisions/context, and writing a dated spec directory under specs/ containing plan.md, requirements.md, and validation.md. Trigger when the user says "feature spec", "next phase", "start the next feature", or invokes /feature-spec.
---

# Feature Spec

## Workflow

### 1. Gather the feature

Ask the user (if they haven't already provided it):
- **Feature name** — short kebab-case slug, e.g. `saved-filters`, `pr-analytics`
- **One-line description** — what the feature does

### 2. Create the branch

Branch from an up-to-date `master`:

```
git checkout master
git pull
git checkout -b feature/<feature-name>
```

### 3. Read guidance files — BEFORE the interview

Read these to ground the spec in existing patterns:
- `CLAUDE.md` — project architecture, build commands, conventions
- `docs/specs/<topic>/design.md` — if a related design spec already exists for this feature
- The closest existing code in the layer you'll touch:
  - **Backend**: nearest controller in `Graphite.Api/Controllers/`, its service in `Graphite.Api/Services/`, the domain model in `Graphite.Domain/Models/`, and the EF configuration in `Graphite.Domain/Data/AppDbContext.cs`
  - **Frontend**: nearest component in `src/components/`, its composable in `src/composables/`, the type in `src/types/`, and the API call in `src/services/`

Reading this first makes interview questions sharper and grounds the spec in existing patterns.

### 4. Interview the user — BEFORE writing any files

Use `AskUserQuestion` with exactly **3 questions in one call**:

| Header       | Question focus |
|--------------|----------------|
| **Scope**    | What the feature does — API endpoints (route, method, request/response DTO), domain behavior, data shape, which layers it touches (Domain / Api / Vue) |
| **Decisions**| Key implementation choices — entity/migration changes, new vs existing service, new SignalR hub events, frontend state (composable vs Pinia store), where in the UI it surfaces |
| **Context**  | Constraints — performance, backward compatibility, real-time update behavior, open questions |

Do **not** write any files until the user has answered all three questions.

### 5. Create the spec directory

Name: `specs/YYYY-MM-DD-<feature-name>/` at the repo root, using today's date.

#### `requirements.md`
- **Scope** section: what is and is not included. For a new endpoint, include the full **API contract** (route, method, request/response DTO, `[Authorize]` attribute, error responses) — this is the seam the frontend builds against.
- **Decisions** section: choices made and why (drawn from the user's answers).
- **Context** section: conventions to follow, existing code to mirror, open questions.

#### `plan.md`
Numbered task groups appropriate to the feature. Typical full-stack order:

1. **Domain** — new/modified entities in `Graphite.Domain/Models/`, `AppDbContext` registration
2. **Migration** — `dotnet ef migrations add <Name>`
3. **Service** — business logic in `Graphite.Api/Services/` (follow existing patterns like `GitHubService`, `PullRequestStatusService`)
4. **Controller** — endpoint in `Graphite.Api/Controllers/` with `[Authorize]`
5. **Real-time** — SignalR hub events in `Graphite.Api/Hubs/PRHub.cs` + `SignalRNotificationService` calls (if the feature broadcasts updates)
6. **Frontend types** — `src/types/`
7. **Frontend API client** — `src/services/api.ts`
8. **Frontend composable / store** — `src/composables/` or `src/stores/`
9. **Frontend component(s)** — `src/components/` and/or `src/views/`
10. **Validation** — manual walkthrough, build commands, edge cases

Each group has numbered sub-tasks and should be independently implementable.

#### `validation.md`
- **Automated**: `dotnet build -c Release` passes from repo root; `cd graphite-vue && npm run build` passes; `dotnet test` if tests exist for the touched area
- **Correctness checks**: new endpoints have `[Authorize]`; queries are scoped to the authenticated user (existing pattern); EF migrations apply cleanly via `dotnet ef database update`; SignalR events fire on the relevant state change
- **Manual walkthrough**: list the exact click-path / API call to exercise the feature, plus edge cases (empty state, error state, concurrent updates)
- **Definition of done**

### 6. Implement the spec

Only after the spec is written. Work `plan.md` top to bottom — implement each task group in order, keeping each independently shippable — following `CLAUDE.md` and the code read in step 3. Validate against `validation.md` before committing. In a fresh session, re-read the spec dir (`requirements.md` → `plan.md` → `validation.md`) to resume.

## Constraints

- Respect the existing stack documented in `CLAUDE.md`: .NET 9 + EF Core + SQLite, Vue 3 + Pinia + Vue Router + SignalR client. No new dependencies without user approval.
- Follow existing conventions — see `CLAUDE.md` and the code read in step 3.
- Keep feature scope focused and independently shippable.
- Real-time updates belong on the SignalR hub at `/hubs/pr` — use `SignalRNotificationService`, don't invent a new channel.
