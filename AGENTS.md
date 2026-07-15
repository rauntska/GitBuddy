# AGENTS.md

Guidelines for coding agents working in this repository.

## Project Overview

GitBuddy is a GitHub PR Dashboard with a .NET 9.0 Web API backend and Vue 3 frontend. It aggregates pull requests across repositories with real-time updates via SignalR.

## Build/Run Commands

### Backend (.NET API)

```bash
cd GitBuddy.Api

# Run in development (http://localhost:5247)
dotnet run

# Build for release
dotnet build -c Release

# Run database migrations
dotnet ef database update

# Create a new migration
dotnet ef migrations add <MigrationName>
```

The connection string is configured in `appsettings*.json` under `ConnectionStrings:DefaultConnection` (default `Host=localhost;Database=GitBuddy;Username=gitbuddy;Password=gitbuddy`).

### Frontend (Vue 3)

```bash
cd gitbuddy-vue

# Install dependencies
npm install

# Run development server (http://localhost:5173)
npm run dev

# Build for production (includes typecheck)
npm run build

# Preview production build
npm run preview
```

### Testing

No test framework is currently configured in this codebase.

## Code Style - Backend (.NET 9.0)

### Architecture Pattern

- **Controllers must be flat** - delegate all logic to MediatR commands/queries
- Use **CQRS pattern with MediatR** for handling requests
- Controllers should only: receive request → send MediatR command → return result
- Do not embed business logic in controllers

### General Conventions

- Use **primary constructors** for dependency injection
- Use **record types** for DTOs: `public record XyzDto(...)`
- Interfaces use `I` prefix: `IGitHubService`
- Nullable reference types enabled: use `string?` for nullable strings
- PascalCase for public members, methods, and classes
- Split longer methods into smaller ones with single responsibility
- No code comments unless explicitly requested

### Example Controller Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
public class PullRequestsController(ISender mediator) : BaseController()
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await mediator.Send(new GetPullRequestByIdQuery(id));
        return Ok(result);
    }
}
```

### Example MediatR Command/Query

```csharp
public record GetPullRequestByIdQuery(int Id) : IRequest<PRDetailDto>;

public class GetPullRequestByIdHandler(
    AppDbContext context,
    IGitHubService gitHubService) 
    : IRequestHandler<GetPullRequestByIdQuery, PRDetailDto>
{
    public async Task<PRDetailDto> Handle(GetPullRequestByIdQuery request, CancellationToken ct)
    {
        // Implementation here
    }
}
```

### DTOs

- Define in `DTOs/` folder
- Use record types with positional parameters
- Keep DTOs separate from domain models

## Code Style - Frontend (Vue 3 + TypeScript)

### Component Structure

- Use **Composition API** with `<script setup lang="ts">`
- Define props with `defineProps<{ ... }>()`
- Split longer methods into smaller helper functions

### Imports

- Use `import type` for type-only imports
- Group imports: Vue → External libraries → Local components → Types → Utils

### Naming Conventions

- Component files: `kebab-case.vue` (e.g., `pr-row.vue`, `file-diff-viewer.vue`)
- Components in templates: PascalCase (e.g., `<PRRow />`)
- Composables: `useXxx.ts` pattern
- Interfaces: PascalCase with descriptive names

### Types

- Prefer `interface` over `type` for object shapes
- Define shared types in `src/types/index.ts`
- Use TypeScript strictly - avoid `any`

### Styling

- Use **Tailwind CSS** classes
- Follow existing dark theme patterns (slate colors)
- Keep styles inline with Tailwind classes in templates

### Example Component

```vue
<template>
  <div class="flex items-center gap-2">
    <span class="text-slate-300">{{ title }}</span>
  </div>
</template>

<script setup lang="ts">
import type { PullRequest } from '../types';

const props = defineProps<{
  pr: PullRequest;
  compact?: boolean;
}>();

const compact = computed(() => props.compact ?? false);
</script>
```

## Error Handling

### Backend

- Use try-catch blocks for external API calls
- Return appropriate HTTP status codes with anonymous objects:
  - `NotFound(new { message = "Resource not found" })`
  - `BadRequest(new { message = "Invalid request", error = ex.Message })`
  - `StatusCode(500, new { message = "Operation failed", error = ex.Message })`
- Log errors using `ILogger<T>`

### Frontend

- Handle API errors in composables
- Use try-catch with async operations
- Axios interceptor handles 401 by clearing auth and reloading

## Architecture

### Backend Layers

```
Controllers → MediatR (Commands/Queries) → Services → Domain
```

- **Controllers**: Thin, delegate to MediatR
- **MediatR Handlers**: Contain request handling logic
- **Services**: Business logic, external integrations (GitHubService, etc.)
- **Domain**: Models, DbContext (GitBuddy.Domain project)

### Frontend Structure

```
src/
├── components/   # Reusable Vue components
├── views/        # Page-level components
├── composables/  # Vue composables (useXxx.ts)
├── stores/       # Pinia stores
├── services/     # API client
├── types/        # TypeScript definitions
├── utils/        # Helper functions
└── router/       # Vue Router config
```

## Documentation

Durable project documentation lives in `docs/`, organized into three maturity stages:
- `docs/ideas/<topic>/idea.md` — early-stage concepts, brain dumps, unstructured notes
- `docs/specs/<topic>/design.md` — refined designs, architecture decisions, technical details
- `docs/plans/<topic>/plan.md` — implementation plans with step-by-step tasks

Each topic gets its own folder; a topic flows from `ideas/` → `specs/` → `plans/` as it matures. Shipped ideas are tagged in place with a top-of-file `> **Status: Implemented**` marker. See `docs/README.md` for full convention details.

Execution specs produced by the `/feature-spec` skill live in `specs/YYYY-MM-DD-<feature>/` at the **repo root** (not under `docs/`) and contain `requirements.md`, `plan.md`, `validation.md`. Those are ephemeral — tied to a feature branch and consumed during implementation.

## CORS Policy

The backend allows frontend origins `http://localhost:5173` and `http://localhost:3000`. Update the `AllowVueDev` policy in `Program.cs` to permit different origins.

## Key Conventions

- All API endpoints require `[Authorize]` except `/api/auth/*`
- JWT tokens stored in localStorage, injected via axios interceptors
- SignalR hub at `/hubs/pr` for real-time updates
- PostgreSQL database (Npgsql) with EF Core migrations (auto-applied on startup)
- PR status determined by `PullRequestStatusService`: Draft, AwaitingReview, Approved, ChangesRequested, Reviewed
