# Graphite - GitHub PR Dashboard

A modern, dark-themed dashboard for tracking GitHub pull requests across an organization. Built with .NET 9.0 Web API and Vue 3.

## Features

- Pull request aggregation across all repositories in an organization
- Smart status grouping (Awaiting Review, Approved, Changes Requested, Draft, Reviewed)
- Reviewer visualization with status indicators
- Comment tracking and line change statistics
- Auto-refresh with configurable interval (5-60 minutes)
- Manual refresh capability
- Compact, responsive dark theme UI
- Click PRs to open directly in GitHub

## Architecture

### Backend (.NET 9.0)
- **Graphite.Api**: Web API with controllers for PRs and settings
- **Graphite.Domain**: Domain models and DbContext with EF Core
- **GitHubService**: Octokit integration for GitHub API calls
- **CacheService**: SQLite caching with PR status determination
- **PRRefreshService**: Background service for auto-refresh

### Frontend (Vue 3)
- Vue 3 with Composition API and TypeScript
- Tailwind CSS for dark theme styling
- Axios for API communication
- Component-based architecture with reusable UI elements

## Database Schema

```sql
GitHubConfigs (GitHub API configuration)
PullRequests (PR data with status)
Reviews (PR review information)
Comments (PR comment counts)
```

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Node.js 18+
- GitHub Personal Access Token with `repo` and `read:org` scopes

### Backend Setup

1. Navigate to Graphite.Api:
```bash
cd Graphite.Api
```

2. Build and run the API:
```bash
dotnet run
```

The API will start on `http://localhost:5000`

### Frontend Setup

1. Navigate to graphite-vue:
```bash
cd graphite-vue
```

2. Install dependencies:
```bash
npm install
```

3. Start the development server:
```bash
npm run dev
```

The frontend will start on `http://localhost:5173`

### Configuration

1. Open the app in your browser
2. Click the Settings (gear icon) in the top-right
3. Enter your GitHub organization name
4. Paste your Personal Access Token
5. Set the auto-refresh interval (default: 5 minutes)
6. Click Save Settings
7. The app will automatically fetch and display open PRs

### Creating a GitHub Personal Access Token

1. Go to https://github.com/settings/tokens/new
2. Select scopes: `repo` and `read:org`
3. Click "Generate token"
4. Copy the token (it won't be shown again)
5. Paste it in the Settings modal

## API Endpoints

### Pull Requests
- `GET /api/pullrequests` - Get all PRs grouped by status
- `GET /api/pullrequests/stats` - Get PR statistics
- `POST /api/pullrequests/refresh` - Manually refresh PR data

### Settings
- `GET /api/settings` - Get current configuration
- `POST /api/settings` - Save configuration

## PR Status Determination

- **Awaiting Review**: Open PR with no reviews
- **Approved**: Has at least one approved review, no changes requested
- **Changes Requested**: Has at least one changes requested review
- **Reviewed**: Has reviews but neither approved nor changes requested
- **Draft**: Marked as draft in GitHub

## Building for Production

### Backend
```bash
cd Graphite.Api
dotnet build -c Release
```

### Frontend
```bash
cd graphite-vue
npm run build
```

## Development

### Run both together

In separate terminals:
```bash
# Terminal 1 - Backend
cd Graphite.Api
dotnet run

# Terminal 2 - Frontend
cd graphite-vue
npm run dev
```

### Database
The SQLite database (`graphite.db`) is created automatically on first run. Migrations are located in `Graphite.Api/Migrations/`.

## Tech Stack

**Backend:**
- .NET 9.0
- Entity Framework Core
- SQLite
- Octokit (GitHub API client)

**Frontend:**
- Vue 3
- TypeScript
- Vite
- Tailwind CSS
- Axios

## License

MIT