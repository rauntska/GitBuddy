# Validation

## Automated

- `dotnet build -c Release` passes from repo root (or `GitBuddy.Api/`).
- `dotnet ef database update` applies the new migration cleanly against a dev DB (or at minimum `dotnet ef migrations add` + `dotnet ef migrations script` for the new migration succeeds without errors and only touches `Users.PersonalAccessToken`).
- `cd gitbuddy-vue && npm run build` passes (catches leftover imports/type errors from deleted files).
- `cd gitbuddy-vue && npx vue-tsc --noEmit` (or equivalent already run by `npm run build`) has no dangling references to `UserSettings`, `useUserSettings`, `UserSettingsPanel`, `SettingsModal`.

## Correctness checks

- Grep the whole repo (excluding `GitBuddy.Domain/Migrations/*` history and `specs/`) for `PersonalAccessToken` — the only remaining hits should be `GitHubConfig.PersonalAccessToken` and its known consumers (`PullRequestsController.cs`, `ImagesController.cs`), plus the unrelated `docs/ideas/multi-org-aggregation/idea.md` mention.
- Grep for `hasPersonalAccessToken`, `HasPersonalAccessToken`, `useUserSettings`, `SettingsModal`, `getUserSettings`, `updateUserSettings` — zero hits outside git history. (`UserSettingsPanel` itself still exists and is expected to appear — it now holds only the Desktop Notifications UI.)
- `/api/users/me/settings` no longer exists as a route (no controller registers it).
- Reviews / comments / file-viewed-state / merge still work end-to-end without any PAT set (they never needed one) — confirms the removed banner's claim was indeed false and nothing regresses.

## Manual walkthrough

1. Log in as any user (admin or not). Confirm:
   - No PAT warning banner appears on the dashboard.
   - No gear/settings icon in the top bar (the old "Quick Settings" trigger) — the top-bar controls that remain (Create PR, Refresh, login/user menu) are unaffected.
   - The left sidebar's Settings icon is still present (unchanged — not admin-gated) and navigates to `/settings`.
2. On `/settings` ("User Settings" in `SettingsNav`), confirm:
   - No Personal Access Token form is present.
   - The Desktop Notifications section still works: permission status indicator, enable button, per-event toggles, quiet hours, and "Send test notification" all behave as before this change.
3. Submit a PR review, post a comment, mark a file as viewed, and merge a PR (as an existing test user with no PAT ever set) — all succeed, proving these never depended on the removed column.

## Definition of done

- All items in `plan.md` implemented.
- `dotnet build -c Release` and `npm run build` both pass.
- Manual walkthrough steps above pass.
- No remaining references to the removed PAT feature outside migration history.
