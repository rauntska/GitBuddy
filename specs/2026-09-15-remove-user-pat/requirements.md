# Remove unused User Personal Access Token (PAT)

## Problem

`User.PersonalAccessToken` is dead code. It is fully wired up on the write side (settings UI lets a user save/clear a PAT, backend persists it, a dashboard banner nags users who haven't set one), but the value is **never read** by any GitHub API call:

- Every operation the UI claims "requires a PAT" (submit review, comment, mark file viewed, merge) actually resolves its token through `PullRequestValidationService.GetRequiredUserWithTokenAsync`, which returns `user.AccessToken` — the GitHub OAuth token from login — never `user.PersonalAccessToken`.
- `UserService.GetUserPersonalAccessTokenAsync(int userId)` is the only place the stored PAT value is ever read back out, and it has zero callers anywhere in the codebase.

Since PAT was the *only* thing the "user settings" feature (`/api/users/me/settings`) exposed, removing it removes that feature end-to-end, not just the column.

## Scope

**In scope — remove entirely:**

Backend:
- `User.PersonalAccessToken` property (`GitBuddy.Domain/Models/User.cs`) + EF migration dropping the column
- `UserSettingsController` (`GitBuddy.Api/Controllers/UserSettingsController.cs`) — the whole `/api/users/me/settings` endpoint, since PAT was its only purpose
- `UserSettingsDto.cs` (`UserSettingsDto`, `UpdateUserSettingsRequest`)
- `IUserService`/`UserService`: `GetUserSettingsAsync`, `UpdateUserSettingsAsync`, `GetUserPersonalAccessTokenAsync`

Frontend:
- `SettingsModal.vue` — the top-bar "Quick Settings" modal, which is entirely a PAT form. Delete the component and its trigger (gear button, `showSettings` ref, `handleSettingsSaved`, import) in `App.vue`.
- `UserSettingsPanel.vue` — the `/settings` page panel, also entirely PAT. Delete the component.
- PAT warning banner in `Dashboard.vue` (`showPATWarning`, `dismissPATWarning`, `patWarningDismissed`, the banner markup, and the `hasPersonalAccessToken`/`fetchUserSettings` import from `useUserSettings`)
- `useUserSettings.ts` composable — delete entirely (no other consumers once the above are gone)
- `apiService.getUserSettings` / `apiService.updateUserSettings` in `services/api.ts`
- `UserSettings` interface in `types/index.ts`

**Not in scope — do not touch:**
- `GitHubConfig.PersonalAccessToken` — a separate, unrelated app-level config field, genuinely used in `PullRequestsController.cs:156` and `ImagesController.cs:97`.
- `docs/ideas/multi-org-aggregation/idea.md`'s mention of a `PersonalAccessToken` field on a future `GitHubOrgConnection` table — unrelated future idea, not this feature.

## Decisions

1. **Migration drops the column outright.** The value was never actually usable as an auth token for anything, so there's no meaningful data to preserve.
2. **No replacement messaging.** The banner's claim that a PAT is required for reviews/comments/viewed-state/merge is false today — those already work via OAuth `AccessToken`. Deleting the banner requires no replacement copy.
3. **Navigation fallout (resolved during spec-writing, not a re-ask):** `UserSettingsPanel` was the only thing rendered at the bare `/settings` route, and that route is reachable via an always-visible sidebar gear icon (`AppSidebar.vue`) shown to *every* authenticated user, not just admins. Removing the panel without touching navigation would leave that icon pointing at a "Page not found" fallback for every non-admin user. Resolution, following the existing admin-gating pattern already used for the other icons in the same file:
   - Gate the sidebar Settings icon behind `isAdmin` (same as the Administration/Analytics icons beside it) and repoint it at `/settings/github-app`.
   - Remove the "User Settings" entry from `SettingsNav.vue`'s menu.
   - Remove the `isUserSettings` branch from `SettingsPage.vue`.
   - Redirect the bare `/settings` route to `/settings/github-app` in `router/index.ts`, mirroring the existing `/admin` → `/settings/admin` redirect already there, so old bookmarks/links don't dead-end.

## Context

- No new dependencies, no schema additions — this is a pure removal.
- Follow existing EF migration conventions (see `GitBuddy.Domain/Migrations/`) — one migration, descriptive name.
- `SettingsModal.vue`'s gear button in `App.vue` currently only exists to open the PAT modal (`shouldShowDashboardControls` gated) — removing it removes that whole quick-access affordance, which is correct since it had no other purpose.
- Open question flagged for the future, not resolved here: if there's ever a real need for a distinct token from the OAuth session (e.g. higher rate limits, org PAT vs. personal OAuth scopes), that should be raised as a fresh idea in `docs/ideas/`, not by resurrecting this dead column.
