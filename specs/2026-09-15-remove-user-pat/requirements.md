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
- PAT warning banner in `Dashboard.vue` (`showPATWarning`, `dismissPATWarning`, `patWarningDismissed`, the banner markup, and the `hasPersonalAccessToken`/`fetchUserSettings` import from `useUserSettings`)
- `useUserSettings.ts` composable — delete entirely (no other consumers once the above are gone)
- `apiService.getUserSettings` / `apiService.updateUserSettings` in `services/api.ts`
- `UserSettings` interface in `types/index.ts`
- The PAT section only of `UserSettingsPanel.vue` (see correction below) — form markup, `localPAT`/`showPAT`/`savingPAT`/`patMessage`/`patMessageType`/`hasExistingPAT`, `loadUserSettings`/`savePAT`/`clearPAT`, and the `apiService` import.

**Not in scope — do not touch:**
- `GitHubConfig.PersonalAccessToken` — a separate, unrelated app-level config field, genuinely used in `PullRequestsController.cs:156` and `ImagesController.cs:97`.
- `docs/ideas/multi-org-aggregation/idea.md`'s mention of a `PersonalAccessToken` field on a future `GitHubOrgConnection` table — unrelated future idea, not this feature.
- **`UserSettingsPanel.vue`'s Desktop Notifications section and the `/settings` route/nav entry that leads to it.** Corrected after initial implementation: this spec originally assumed `UserSettingsPanel.vue` was entirely a PAT form (based on reading only its first ~60 lines) and deleted the whole file, the `/settings` route, the `UserSettingsPanel`/"User Settings" nav entries, and de-admin-gated the sidebar Settings icon. That was wrong — the file's second half is the real, working Desktop Notifications settings UI (permission toggle, per-event toggles, quiet hours, test notification), unrelated to PAT. The fix: keep `UserSettingsPanel.vue`, `/settings`, `SettingsNav`'s "User Settings" entry, and the always-visible (non-admin-gated) sidebar Settings icon exactly as they were before this feature — only strip the PAT-specific markup/script out of the panel.

## Decisions

1. **Migration drops the column outright.** The value was never actually usable as an auth token for anything, so there's no meaningful data to preserve.
2. **No replacement messaging.** The banner's claim that a PAT is required for reviews/comments/viewed-state/merge is false today — those already work via OAuth `AccessToken`. Deleting the banner requires no replacement copy.
3. **Navigation fallout — superseded.** The original spec assumed removing `UserSettingsPanel.vue` entirely required de-admin-gating and repointing the sidebar Settings icon, dropping the "User Settings" nav entry, and redirecting `/settings`. Since `UserSettingsPanel.vue` is *not* being deleted (see Scope correction above) — only its PAT section — none of that navigation surgery is needed. `/settings`, `SettingsNav`, and `AppSidebar.vue` are left exactly as they were on `master`.

## Context

- No new dependencies, no schema additions — this is a pure removal.
- Follow existing EF migration conventions (see `GitBuddy.Domain/Migrations/`) — one migration, descriptive name.
- `SettingsModal.vue`'s gear button in `App.vue` currently only exists to open the PAT modal (`shouldShowDashboardControls` gated) — removing it removes that whole quick-access affordance, which is correct since it had no other purpose.
- Open question flagged for the future, not resolved here: if there's ever a real need for a distinct token from the OAuth session (e.g. higher rate limits, org PAT vs. personal OAuth scopes), that should be raised as a fresh idea in `docs/ideas/`, not by resurrecting this dead column.
