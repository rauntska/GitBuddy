# GitBuddy - GitHub PR Dashboard

A modern, dark-themed dashboard for tracking GitHub pull requests across an organization. Built with .NET 9.0 Web API and Vue 3, with real-time updates over SignalR and a full PR review experience (diffs, comments, reviews, analytics).

## Features

- GitHub OAuth sign-in with JWT sessions; role-based access (`Admin`, `Developer`)
- Pull request aggregation across all repositories in an organization
- Smart status grouping (Ready To Merge, Awaiting Review, Approved, Changes Requested, Reviewed, Draft) plus a paginated Merged / Closed section
- Full file diff viewer with file tree, syntax highlighting, minimap, and per-file "viewed" state
- Threaded comments with replies, resolve/unresolve, reactions, drafts, and reusable comment templates
- Pending review workflow (batch comments, then submit as Approved / Changes Requested / Comment)
- Reviewer management, review timeline, and PR timeline
- Inline-editable PR title/description, merge dialog (merge/squash/rebase), and draft publish
- Create-PR flow with branch comparison and a "branches without PRs" tracker
- Real-time updates via SignalR (PR/comment/thread/CI changes) with auto-reconnect
- Admin panel: user management, invitation system, and allowlist
- Analytics: throughput, reviewer load, and repository health (admin only)
- Browser notifications and favicon badge for unread PRs
- Auto-refresh with configurable intervals for PRs and branches, plus manual refresh
- Compact, responsive dark theme UI

## Architecture

### Backend (.NET 9.0)
- **GitBuddy.Api**: Web API with thin controllers delegating to MediatR commands/queries (CQRS)
- **GitBuddy.Domain**: Domain models and `AppDbContext` with EF Core migrations
- **GitHubService** (Octokit) + **GitHubGraphQLService**: REST and GraphQL GitHub API integration
- **CacheService**: Caches and serves PR data and orchestrates refreshes
- **PullRequestStatusService**: Determines PR status (Draft, AwaitingReview, Approved, ChangesRequested, Reviewed, ReadyToMerge)
- **WebhookService** + **GitHubWebhookProcessor**: Inbound GitHub webhook ingestion
- **SignalRNotificationService**: Broadcasts updates over the `PRHub`
- Background services: **PRRefreshService** (PR auto-refresh), **BranchWithoutPRRefreshService** (branch tracking), **RepositoryRuleSyncWorker** (hourly rule sync)
- **JwtService**: Issues JWTs after GitHub OAuth login

### Frontend (Vue 3)
- Vue 3 with Composition API and TypeScript
- Pinia for auth state; composables for all other state
- Tailwind CSS for dark theme styling
- Axios for API communication with JWT interceptor
- `@microsoft/signalr` for real-time updates
- TipTap rich-text editor (with @mentions, image upload), Chart.js for analytics

## Database

GitBuddy uses **PostgreSQL** (EF Core provider: `Npgsql.EntityFrameworkCore.PostgreSQL`). The database is auto-migrated on startup. Key tables include:

```
Users, UserPreferences, UserFileViewedStates, Invitations, AllowedUsers
GitHubConfigs, PullRequests, Reviews, ReviewThreads
Comments, CommentReactions, CommentDrafts, CommentTemplates
FileDiffs, CheckRuns, PendingReviews, PendingComments
RepositoryRules, BranchesWithoutPR
```

Migrations live in `GitBuddy.Domain/Migrations/`.

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Node.js 18+
- PostgreSQL (or use the Docker Compose stack, which provides it)
- A GitHub OAuth App (see [OAUTH_SETUP.md](./OAUTH_SETUP.md)) with scopes `user:email read:org repo`

### Backend Setup

1. Navigate to GitBuddy.Api:
```bash
cd GitBuddy.Api
```

2. Configure GitHub OAuth and JWT settings in `appsettings.json` (or `appsettings.Development.json`). At minimum, set `GitHub:ClientId`, `GitHub:ClientSecret`, `GitHub:RedirectUri`, and `Jwt:Key`. The connection string defaults to a local PostgreSQL instance:
```
Host=localhost;Database=GitBuddy;Username=gitbuddy;Password=gitbuddy
```

3. Build and run the API:
```bash
dotnet run
```

The API will start on `http://localhost:5247`. (Development config points the connection string at PostgreSQL on port `5233`, matching the Docker Compose stack.)

### Frontend Setup

1. Navigate to gitbuddy-vue:
```bash
cd gitbuddy-vue
```

2. Install dependencies:
```bash
npm install
```

3. Start the development server:
```bash
npm run dev
```

The frontend will start on `http://localhost:5173`. The Vite dev server proxies `/api` and `/hubs` requests to the backend at `http://localhost:5247`.

### Sign-in & Configuration

GitBuddy authenticates via GitHub OAuth:

1. Create a GitHub OAuth App (see [OAUTH_SETUP.md](./OAUTH_SETUP.md)) with authorization callback URL `http://localhost:5247/api/auth/github/callback`
2. Open the app in your browser and sign in with GitHub
3. The first user is automatically promoted to **Admin**; subsequent users must be on the allowlist or use an invitation link
4. Admins configure the GitHub organization / GitHub App / Personal Access Token under **Settings**
5. The app fetches and displays open PRs, with auto-refresh running in the background

## API Endpoints

All endpoints except `/api/auth/*` and `/api/webhooks/*` require JWT authentication (`[Authorize]`). Admin-only endpoints additionally require the `Admin` role.

### Auth
- `GET /api/auth/github` - Start GitHub OAuth flow
- `GET /api/auth/github/callback` - OAuth callback, issues JWT
- `GET /api/auth/me` - Current user
- `POST /api/auth/logout` - Logout

### Pull Requests (`/api/pullrequests`)
- `GET ` - PRs grouped by status
- `GET /stats`, `GET /unread-count`, `GET /merged` - Statistics and paginated merged PRs
- `POST /refresh` - Manual refresh
- `GET /{id}` - PR detail; `PATCH /{id}` - edit title/description
- `GET /{id}/files/{path}`, `GET /{id}/files/content` - File diffs and content ranges
- `POST /{id}/files/viewed`, `POST /{id}/file-diffs/refresh-viewed-states` - Viewed state
- `GET|POST /{id}/comments`, replies, thread resolve/unresolve - Comments
- `GET|POST /{id}/review`, `GET /{id}/reviewers`, `POST|DELETE /{id}/reviewers` - Reviews
- `GET /{id}/pending-review`, pending comments, submit, delete - Pending reviews
- `GET /{id}/timeline`, `GET /{id}/mentionable-users`, `GET /{id}/potential-reviewers` - Activity
- `POST /{id}/merge`, `GET /{id}/merge-options`, `POST /{id}/publish` - Merging / publishing drafts

### Comments, Drafts & Templates
- `/api/comments/{id}` (PUT/DELETE), `/reactions` - Comment CRUD and reactions
- `/api/comment-drafts` - Comment draft autosave
- `/api/comment-templates` - Reusable templates with tags and usage tracking

### Repositories
- `GET /api/repositories`, `/organization`, `/{owner}/{repo}/branches`, `/{owner}/{repo}/compare`
- `GET /api/repositories/branches-without-prs`, `POST /api/repositories/branches-without-prs/refresh`

### Users & Settings
- `GET|PATCH /api/userpreferences` - Per-user UI preferences
- `GET|PUT /api/users/me/settings` - Per-user PAT settings
- `GET|POST /api/settings` - Global GitHub configuration (Admin only)

### Admin (`/api/admin`, Admin role required)
- Users: `GET /users`, `PUT /users/{id}/role`, `DELETE /users/{id}`
- Invitations: `GET|POST /invitations`, `DELETE /invitations/{id}`
- Allowlist: `GET|POST /allowlist`, `DELETE /allowlist/{id}`
- Analytics: `GET /stats`, `/analytics/throughput`, `/analytics/reviewers`, `/analytics/health`

### Images & Webhooks
- `POST /api/images/upload`, `GET /api/images/proxy`, `GET /api/images/uploads/{filename}`
- `GET /api/health` - Health check
- `POST /api/webhooks/github` - GitHub webhook receiver

### SignalR
- Hub endpoint: `/hubs/pr` - Real-time PR, comment, review, and CI updates

## PR Status Determination

Handled by `PullRequestStatusService`:
- **Draft**: Marked as draft in GitHub
- **Awaiting Review**: Open PR with no reviews
- **Approved**: Has at least one approved review, no changes requested
- **Changes Requested**: Has at least one changes requested review
- **Reviewed**: Has reviews but neither approved nor changes requested
- **Ready To Merge**: Approved and mergeable

## Building for Production

### Backend
```bash
cd GitBuddy.Api
dotnet build -c Release
```

### Frontend
```bash
cd gitbuddy-vue
npm run build
```

## Docker

### Full Stack (All services in Docker)

```bash
cd deployment/local
docker-compose up
```

This brings up nginx, the API, the frontend, and PostgreSQL. Access the app at `http://localhost` - nginx routes:
- `/api/*` and `/hubs/*` → Backend API
- `/*` → Frontend static files

### Hybrid (API on host, nginx + PostgreSQL in Docker)

Run the API locally with nginx reverse-proxying and PostgreSQL running in Docker. The API block in `docker-compose.local.yml` is commented out by default.

```bash
# Terminal 1 - Run API on host
cd GitBuddy.Api
dotnet run

# Terminal 2 - Start nginx + PostgreSQL via Docker
docker-compose -f docker-compose.local.yml up
```

Access the app at `http://localhost`. PostgreSQL is exposed on host port `5233`.

## Development

### Run both together (Development mode)

In separate terminals:
```bash
# Terminal 1 - Backend
cd GitBuddy.Api
dotnet run

# Terminal 2 - Frontend
cd gitbuddy-vue
npm run dev
```

The Vite dev server proxies `/api` and `/hubs` requests to the backend automatically.

## Tech Stack

**Backend:**
- .NET 9.0
- ASP.NET Core Web API + SignalR
- MediatR (CQRS)
- Entity Framework Core + PostgreSQL (Npgsql)
- Octokit / Octokit.GraphQL
- JWT Bearer authentication + GitHub OAuth

**Frontend:**
- Vue 3 + TypeScript
- Vite
- Pinia + Vue Router
- Tailwind CSS
- Axios
- @microsoft/signalr
- TipTap (rich text) + Chart.js + Prism/lowlight (syntax highlighting)

## License

MIT
