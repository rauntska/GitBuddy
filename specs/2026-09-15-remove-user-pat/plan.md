# Plan

## 1. Domain

1.1. `GitBuddy.Domain/Models/User.cs` — remove the `PersonalAccessToken` property.

## 2. Migration

2.1. `cd GitBuddy.Api && dotnet ef migrations add RemoveUserPersonalAccessToken --project ../GitBuddy.Domain --startup-project .` (adjust exact invocation to match how existing migrations were generated — check `GitBuddy.Domain/Migrations/` history / a README if present). Migration should just drop the `PersonalAccessToken` column from `Users`.

## 3. Backend service layer

3.1. `GitBuddy.Api/Services/UserService.cs` (and `IUserService` interface section at the top of the same file):
   - Remove `GetUserSettingsAsync`, `UpdateUserSettingsAsync`, `GetUserPersonalAccessTokenAsync` from the interface.
   - Remove their implementations.
3.2. `GitBuddy.Api/DTOs/UserSettingsDto.cs` — delete the file (both `UserSettingsDto` and `UpdateUserSettingsRequest` records).

## 4. Backend controller

4.1. Delete `GitBuddy.Api/Controllers/UserSettingsController.cs` entirely.
4.2. Grep for any remaining reference to `/api/users/me/settings`, `UserSettingsDto`, `UpdateUserSettingsRequest`, `GetUserPersonalAccessTokenAsync`, `HasPersonalAccessToken` in `GitBuddy.Api/` and `GitBuddy.Domain/` (outside migrations, which stay as history) to confirm nothing else references them.

## 5. Frontend types & API client

5.1. `gitbuddy-vue/src/types/index.ts` — remove the `UserSettings` interface.
5.2. `gitbuddy-vue/src/services/api.ts` — remove `getUserSettings` and `updateUserSettings`.

## 6. Frontend composable

6.1. Delete `gitbuddy-vue/src/composables/useUserSettings.ts`.

## 7. Frontend components — delete PAT-only UI

7.1. Delete `gitbuddy-vue/src/views/SettingsModal.vue`.
7.2. `gitbuddy-vue/src/App.vue` — remove the `SettingsModal` import, the `showSettings` ref, `handleSettingsSaved`, the gear button that sets `showSettings = true`, and the `<SettingsModal ... />` usage.
7.3. Delete `gitbuddy-vue/src/components/settings/UserSettingsPanel.vue`.
7.4. `gitbuddy-vue/src/views/SettingsPage.vue` — remove the `UserSettingsPanel` import and the `isUserSettings` computed + its template branch.

## 8. Frontend — Dashboard PAT banner

8.1. `gitbuddy-vue/src/views/Dashboard.vue` — remove the PAT warning banner markup, the `showPATWarning` computed, `patWarningDismissed` ref, `dismissPATWarning` function, and the `useUserSettings` import/usage (`hasPersonalAccessToken`, `fetchUserSettings`).

## 9. Frontend — navigation fallout

9.1. `gitbuddy-vue/src/components/layout/AppSidebar.vue` — change the Settings icon: add `v-if="isAdmin"` (matching the Administration/Analytics icons in the same file) and change its `to` from `/settings` to `/settings/github-app`.
9.2. `gitbuddy-vue/src/components/settings/SettingsNav.vue` — remove the `{ id: 'user', label: 'User Settings', path: '/settings', ... }` menu item.
9.3. `gitbuddy-vue/src/router/index.ts` — change the bare `/settings` route from `component: SettingsPage` to `redirect: '/settings/github-app'`, matching the existing `/admin` → `/settings/admin` redirect pattern already in the file.

## 10. Validation

See `validation.md`.
