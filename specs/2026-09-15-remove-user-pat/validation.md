# Validation

## Automated

- `dotnet build -c Release` passes from repo root (or `GitBuddy.Api/`).
- `dotnet ef database update` applies the new migration cleanly against a dev DB (or at minimum `dotnet ef migrations add` + `dotnet ef migrations script` for the new migration succeeds without errors and only touches `Users.PersonalAccessToken`).
- `cd gitbuddy-vue && npm run build` passes (catches leftover imports/type errors from deleted files).
- `cd gitbuddy-vue && npx vue-tsc --noEmit` (or equivalent already run by `npm run build`) has no dangling references to `UserSettings`, `useUserSettings`, `UserSettingsPanel`, `SettingsModal`.

## Correctness checks

- Grep the whole repo (excluding `GitBuddy.Domain/Migrations/*` history and `specs/`) for `PersonalAccessToken` — the only remaining hits should be `GitHubConfig.PersonalAccessToken` and its known consumers (`PullRequestsController.cs`, `ImagesController.cs`), plus the unrelated `docs/ideas/multi-org-aggregation/idea.md` mention.
- Grep for `hasPersonalAccessToken`, `HasPersonalAccessToken`, `useUserSettings`, `UserSettingsPanel`, `SettingsModal`, `getUserSettings`, `updateUserSettings` — zero hits outside git history.
- `/api/users/me/settings` no longer exists as a route (no controller registers it).
- Reviews / comments / file-viewed-state / merge still work end-to-end without any PAT set (they never needed one) — confirms the removed banner's claim was indeed false and nothing regresses.

## Manual walkthrough

1. Log in as a non-admin user. Confirm:
   - No PAT warning banner appears on the dashboard.
   - No gear/settings icon in the top bar (the old "Quick Settings" trigger) or, if `shouldShowDashboardControls` still shows other controls, confirm the PAT-specific button specifically is gone.
   - The left sidebar has no visible Settings icon at all (admin-only now).
   - Navigating directly to `/settings` in the URL bar redirects to `/settings/github-app` and then bounces to `/` (since the user isn't an admin) via the existing `requiresAdmin` router guard — not a dead "Page not found" page.
2. Log in as an admin user. Confirm:
   - The sidebar Settings icon is visible and links to `/settings/github-app`.
   - `SettingsNav` no longer lists "User Settings".
   - Navigating to `/settings` redirects to `/settings/github-app` and renders `GitHubAppPanel`.
3. Submit a PR review, post a comment, mark a file as viewed, and merge a PR (as an existing test user with no PAT ever set) — all succeed, proving these never depended on the removed column.

## Definition of done

- All items in `plan.md` implemented.
- `dotnet build -c Release` and `npm run build` both pass.
- Manual walkthrough steps above pass.
- No remaining references to the removed PAT feature outside migration history.
