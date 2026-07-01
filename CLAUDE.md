# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GitBuddy is a GitHub PR Dashboard built with .NET 9.0 Web API backend and Vue 3 frontend. It aggregates pull requests across repositories in an organization with real-time updates via SignalR.

## Build and Run Commands

### Backend (.NET API)
```bash
# Navigate to API project
cd GitBuddy.Api

# Run in development
dotnet run

# Build for release
dotnet build -c Release

# Run database migrations
dotnet ef database update

# Create a new migration
dotnet ef migrations add <MigrationName>
```

The API runs on `http://localhost:5247` (development) and uses PostgreSQL (Npgsql). The connection string is configured in `appsettings*.json` under `ConnectionStrings:DefaultConnection` (default `Host=localhost;Database=GitBuddy;Username=gitbuddy;Password=gitbuddy`).

### Frontend (Vue 3)
```bash
# Navigate to frontend
cd gitbuddy-vue

# Install dependencies
npm install

# Run development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

The frontend runs on `http://localhost:5173` (development).

## Architecture

### Backend Structure

- **GitBuddy.Api/** - Web API project
  - `Controllers/` - API endpoints (PullRequests, Auth, Webhooks, Settings, UserSettings, UserPreferences, Comments, CommentDrafts, CommentTemplates, Repositories, Images, Admin; plus `BaseController` shared base)
  - `Services/` - Business logic (GitHubService, GitHubGraphQLService, CacheService, JwtService, PullRequestStatusService, PullRequestValidationService, SignalRNotificationService, UserService, WebhookService, AllowlistService, RepositoryRuleService, InvitationService, etc.)
  - `BackgroundServices/` - Hosted services (`PRRefreshService` for auto-refresh, `RepositoryRuleSyncWorker` for rule sync)
  - `Hubs/` - SignalR hub (PRHub for real-time updates)
  - `Processors/` - GitHub webhook processing (`GitHubWebhookProcessor`)
  - `DTOs/` - Data transfer objects
  - `Program.cs` - Application entry point with DI configuration

- **GitBuddy.Domain/** - Domain models and data layer
  - `Models/` - Entity classes (PullRequest, Review, ReviewThread, Comment, User, CheckRun, FileDiff, etc.)
  - `Data/` - EF Core DbContext (AppDbContext)
  - `Migrations/` - EF Core migrations

### Frontend Structure

- `src/components/` - Vue components (PRRow, PRGroup, FileDiffViewer, CommentsPanel, etc.)
- `src/views/` - Page views (Dashboard, PRDetail, SettingsModal, SettingsPage, AuthCallback, AccessDenied, AdminPanel)
- `src/composables/` - Vue composables (usePullRequests, usePRDetail, useAuth, useSettings, useUserSettings, useUserPreferences, useSignalR, useToast, useDraftAutosave, useImageUpload, useCreatePR, useBranchesWithoutPR, useFaviconBadge, useBrowserNotifications, etc.)
- `src/stores/` - Pinia stores (auth store)
- `src/services/` - API client
- `src/utils/` - Utility functions (prHelpers, diffHelpers, syntaxHighlight, api with axios interceptors)
- `src/types/` - TypeScript type definitions
- `src/router/` - Vue Router configuration

## Key Concepts

### Authentication
- GitHub OAuth 2.0 with JWT tokens
- All API endpoints require `[Authorize]` except `/api/auth/*`
- JWT tokens stored in localStorage, injected via axios interceptors
- SignalR hub auth via query string `access_token`

### PR Status Determination (PullRequestStatusService)
PRs are categorized based on reviews:
- **Draft**: PR is marked as draft
- **AwaitingReview**: No reviews yet
- **Approved**: At least one approval, no changes requested
- **ChangesRequested**: At least one changes requested review
- **Reviewed**: Has comments but no approvals or changes requested

### Real-time Updates
- SignalR hub at `/hubs/pr` broadcasts PR updates, comments, and review thread changes
- Frontend uses `useSignalR` composable for connection management
- Notifications sent via `SignalRNotificationService`

### Webhooks
- GitHub webhooks received at `/api/webhooks/github`
- Processed by `GitHubWebhookProcessor` to trigger cache updates

### GitHub Integration
- `GitHubService` - Octokit-based REST API calls
- `GitHubGraphQLService` - GraphQL queries for complex data (PR details, file diffs, review threads)
- User's OAuth token stored in `User.AccessToken` for authenticated GitHub requests

## Database
- PostgreSQL via Npgsql provider, configured in `Program.cs` and `appsettings*.json`
- EF Core Migrations — auto-applied on startup via `await dbContext.Database.MigrateAsync()`
- Key entities: PullRequest, Review, ReviewThread, Comment, User, CheckRun, FileDiff, UserFileViewedState, BranchWithoutPR

## Configuration
- `appsettings.json` - Production config
- `appsettings.Development.json` - Development config
- Required sections: `Jwt`, `GitHub` (ClientId, ClientSecret, WebhookSecret, RedirectUri)

## Documentation

Durable project documentation lives in `docs/`, organized into three maturity stages:
- `docs/ideas/<topic>/idea.md` — early-stage concepts, brain dumps, unstructured notes
- `docs/specs/<topic>/design.md` — refined designs, architecture decisions, technical details
- `docs/plans/<topic>/plan.md` — implementation plans with step-by-step tasks

Each topic gets its own folder; a topic flows from `ideas/` → `specs/` → `plans/` as it matures. Shipped ideas are tagged in place with a top-of-file `> **Status: Implemented**` marker. See `docs/README.md` for full convention details.

Execution specs produced by the `/feature-spec` skill live in `specs/YYYY-MM-DD-<feature>/` at the **repo root** (not under `docs/`) and contain `requirements.md`, `plan.md`, `validation.md`. Those are ephemeral — tied to a feature branch and consumed during implementation.

## CORS Policy
The backend allows frontend at `http://localhost:5173` and `http://localhost:3000`. Update `AllowVueDev` policy in `Program.cs` for different origins.
