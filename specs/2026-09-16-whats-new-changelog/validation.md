# What's New Changelog — Validation

## Automated

- `dotnet build -c Release` passes from repo root.
- `cd gitbuddy-vue && npm run build` passes (type-checks the new composable/components/types).
- No existing test suite covers `UserService`/`UserPreferencesController` today, so no new automated tests are required to match existing coverage conventions — but if adding `ChangelogService` tests is cheap, cover: slug generation/stability, upsert-not-duplicate on re-parse, and update-in-place-without-resetting-CreatedAt.

## Correctness checks

- `ChangelogController`'s both endpoints carry `[Authorize]` and resolve `userId` from the `UserId` claim the same way `UserPreferencesController` does (401 if missing/unparseable) — no endpoint is reachable without auth.
- `GET /api/changelog` and `POST /api/changelog/mark-seen` only ever read/write the *authenticated caller's* `User` row — never accept a userId from the client.
- `dotnet ef database update` applies the new migration cleanly against an existing dev DB (verifies the `LastSeenChangelogAt` backfill for pre-existing users doesn't fail as a non-nullable column add).
- Re-running `ChangelogService.SyncFromFileAsync()` twice against the same `CHANGELOG.md` (e.g. two app restarts without a file change) produces no duplicate `ChangelogEntry` rows (unique `Slug` upsert works).
- Editing an existing entry's body in `CHANGELOG.md` and restarting updates the row without changing its `CreatedAt` or bumping it to "unseen" for users who already dismissed it (their `LastSeenChangelogAt` is untouched by the sync).
- Missing `CHANGELOG.md` (e.g. a dev environment where the build-wiring step didn't copy it) logs a warning and does not crash startup.

## Manual walkthrough

1. Add two entries to `CHANGELOG.md` under two different `##` dates, one `feature` and one `fix`, then `dotnet run` the API.
2. Log in as a user whose `LastSeenChangelogAt` predates both entries (or a fresh signup — confirm they do *not* see the modal, since new users start caught-up).
3. Log in as an *existing* user (one who was in the DB before this feature shipped, so their `LastSeenChangelogAt` was backfilled to their prior `CreatedAt`) — confirm the modal auto-opens on dashboard load, showing both entries with correct category badges and rendered markdown (including an image, if one is added to a test entry).
4. Click "Got it" — confirm the modal closes and does not reopen on a subsequent page reload.
5. Click the header "What's new" link — confirm `/whats-new` renders the full history (including entries already dismissed).
6. Add a third `CHANGELOG.md` entry, restart the API, reload the dashboard as the same user — confirm only the new entry appears as unseen (modal shows just the new one, not the two already-dismissed ones).
7. Edge case: empty `CHANGELOG.md` (or the file entirely absent) — dashboard loads normally, no modal, `/whats-new` shows an empty state, no console/network errors.
8. Edge case: an entry published "today" for a user who last dismissed earlier today but before this entry existed — confirm it still shows as unseen (same-day ordering handled correctly per the `useChangelog` composable's date comparison).

## Definition of done

- All items in "Manual walkthrough" verified locally.
- `dotnet build -c Release` and `npm run build` both pass.
- New migration reviewed and confirmed to backfill `LastSeenChangelogAt` for existing users rather than defaulting them to `DateTime.MinValue` (which would make every historical entry "unseen" for every existing user at once).
- Visual review against `docs/reference/visual-style.md`: no card-on-card framing, category badge colors match the semantic-accent table, entries separated by `border-b border-slate-800` only.
