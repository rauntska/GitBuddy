# What's New Changelog — Requirements

## Summary

An in-app "what's new since your last visit" changelog for GitBuddy itself (the tool, not the tracked PRs). Maintainers hand-write entries in a `CHANGELOG.md` at the repo root as part of shipping a feature; the API parses that file into a DB table on startup; each user gets a modal the first time they load the dashboard after a new entry has been published, and a persistent history page for browsing later.

This is a product-changelog-for-the-app feature. It is unrelated to the existing `docs/ideas/release-notes/` idea, which digests *merged PRs from tracked repos* into release notes for external stakeholders — different content source, different audience, different feature.

## Scope

### In scope

- `CHANGELOG.md` at repo root, hand-maintained, parsed into a new `ChangelogEntry` table at API startup.
- Per-user `LastSeenChangelogAt` timestamp; new users default it to their account-creation time (not `null`) so signups never see historical backlog.
- `GET /api/changelog` — full entry history + the caller's `lastSeenChangelogAt`.
- `POST /api/changelog/mark-seen` — bulk-marks everything seen as of now.
- A modal that auto-opens on dashboard load when unseen entries exist, dismissed as one batch ("Got it").
- A `/whats-new` history page, linked from a small persistent header link, that also marks everything seen on visit.
- Category badges (`Feature` / `Improvement` / `Fix`) reusing the existing chip pattern.
- Markdown body per entry (including optional images via plain markdown image syntax), rendered via the existing `DescriptionRenderer` / `useProxiedHtml` pipeline.

### Out of scope

- Per-entry (as opposed to bulk) read/unread state.
- Any admin UI for authoring entries — content is authored by hand-editing `CHANGELOG.md` in a normal PR.
- Image upload plumbing — authors paste an existing image URL (e.g. a GitHub-hosted asset URL, the same way PR descriptions already do); no new upload endpoint.
- Periodic re-sync while running — parsing happens once at startup only.
- Any relation to `docs/ideas/release-notes/` (merged-PR digests) — not touched by this feature.

## API Contract

### `GET /api/changelog`

- `[Authorize]`
- Response `200 OK`:
  ```json
  {
    "entries": [
      {
        "slug": "2026-09-16-prose-diff-view-for-markdown-prs",
        "publishedOn": "2026-09-16",
        "category": "Feature",
        "title": "Prose diff view for markdown PRs",
        "body": "You can now view a rendered diff...\n\n![screenshot](https://...)"
      }
    ],
    "lastSeenChangelogAt": "2026-09-10T08:00:00Z"
  }
  ```
  `entries` is ordered newest-first (by `publishedOn` desc, then `Id` desc as a tiebreaker within the same date).
- No error responses beyond the standard `401` from `[Authorize]` — this endpoint never 404s or 400s; an empty changelog returns an empty `entries` array.

### `POST /api/changelog/mark-seen`

- `[Authorize]`
- No request body.
- Sets the current user's `LastSeenChangelogAt = DateTime.UtcNow`.
- Response `200 OK`: `{ "lastSeenChangelogAt": "2026-09-16T12:34:56Z" }`.

## Decisions

- **File location**: `CHANGELOG.md` stays at repo root (standard convention, reviewable in PRs like any other doc). Reaching the API's published output requires two build-file edits: a `<Content Include="../CHANGELOG.md" CopyToOutputDirectory="PreserveNewest" />` in `GitBuddy.Api.csproj`, and one additional `COPY CHANGELOG.md .` line in `GitBuddy.Api/Dockerfile` (which today only copies `GitBuddy.Api/` and `GitBuddy.Domain/`, not the repo root).
- **Parsing service**: new `GitBuddy.Api/Services/ChangelogService.cs`, invoked once in `Program.cs` right after `dbContext.Database.MigrateAsync()` — not a hosted/background service, since it runs once and exits. Upserts by a stable `Slug` (derived from `publishedOn` + slugified title) so re-running on every deploy only inserts genuinely new entries; editing an existing entry's body/title updates the row in place without touching `CreatedAt` or anyone's `LastSeenChangelogAt` (no read-state reset on edit).
- **Re-sync cadence**: startup-only. No periodic background re-parse — this app already ships changes via redeploy, so a new `CHANGELOG.md` entry always arrives with a restart. Matches the `MigrateAsync()` pattern rather than the `PRRefreshService` polling pattern.
- **Unseen derivation**: no separate `/unseen` endpoint. The frontend compares each entry's implicit "published" instant against `lastSeenChangelogAt` client-side. One payload, less API surface.
- **Bulk dismiss only**: no per-entry read state, no join table — just the one `LastSeenChangelogAt` column on `User`, mirroring how `UserPreferences` already hangs a single row off `User`.
- **Menu placement**: a small persistent link/icon in the app header (`App.vue`, alongside `UserMenu`/refresh/create-PR controls), always visible when authenticated — not tucked inside the settings modal. Router entry at `/whats-new`.
- **Modal mount point**: `ChangelogModal` mounts in `App.vue` (gated on `authStore.isAuthenticated`), not inside `Dashboard.vue`, so it can eventually be shown regardless of route without duplicating logic. For this iteration it still only fires its "should I be visible" check when the dashboard has loaded once (see Context below).
- **Images**: plain markdown image syntax in the `CHANGELOG.md` body (e.g. a GitHub-hosted asset URL pasted the same way PR descriptions already embed images). No new upload endpoint; the existing `useProxiedHtml` GitHub-asset proxy already handles rendering these.

## Context — conventions to follow

- Mirror `UserPreferencesController` / `IUserService` for the controller/service shape: `[Authorize]`, read `UserId` from `User.FindFirst("UserId")`, `AppDbContext` injected via primary constructor.
- Mirror `AppDbContext.OnModelCreating` patterns already used for single-row-per-user tables (`UserPreferences`): `HasIndex(e => e.UserId).IsUnique()` is not needed here since `LastSeenChangelogAt` is a column directly on `User`, not a separate table — simpler than `UserPreferences`.
- `ChangelogEntry` needs a unique index on `Slug` (idempotent upsert key), and an index on `PublishedOn` for ordering.
- Frontend: mirror `useUserPreferences` for the composable shape (load-once-on-mount, expose reactive state + mutator), `DescriptionRenderer.vue` for rendering entry bodies, `StatusBadge.vue`'s chip markup for the category badge (new colors needed — glyph/label table doesn't have Feature/Improvement/Fix, so this needs its own small badge component or a local color map, not a reuse of `getStatusGlyph`).
- Visual style: follow `docs/reference/visual-style.md` exactly — no card-on-card framing, `border-b border-slate-800` dividers between entries, existing `.prose prose-invert` markdown classes, buttons per the guide's primary/secondary patterns.
- Route registration follows the existing flat list in `src/router/index.ts`; `/whats-new` needs `meta: { requiresAuth: true }` like `/settings`.

## Open questions

None outstanding — all decisions above were confirmed during the interview.
