# What's New Changelog — Implementation Plan

Work top to bottom; each numbered group is independently implementable and buildable.

## 1. Domain

1. `GitBuddy.Domain/Models/ChangelogEntry.cs`:
   ```csharp
   public class ChangelogEntry
   {
       public int Id { get; set; }
       public string Slug { get; set; } = string.Empty;
       public DateOnly PublishedOn { get; set; }
       public string Category { get; set; } = string.Empty; // "Feature" | "Improvement" | "Fix"
       public string Title { get; set; } = string.Empty;
       public string Body { get; set; } = string.Empty; // markdown
       public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
   }
   ```
2. `GitBuddy.Domain/Models/User.cs`: add `public DateTime LastSeenChangelogAt { get; set; }` (non-nullable — always set at creation, see Service group).
3. `GitBuddy.Domain/Data/AppDbContext.cs`:
   - Add `public DbSet<ChangelogEntry> ChangelogEntries { get; set; }`.
   - In `OnModelCreating`, add:
     ```csharp
     modelBuilder.Entity<ChangelogEntry>(entity =>
     {
         entity.HasKey(e => e.Id);
         entity.HasIndex(e => e.Slug).IsUnique();
         entity.HasIndex(e => e.PublishedOn);
     });
     ```

## 2. Migration

1. From `GitBuddy.Api/`: `dotnet ef migrations add AddChangelogEntries`.
2. Review the generated migration: it should add the `ChangelogEntries` table and the `User.LastSeenChangelogAt` column. Since the column is non-nullable and `User` may already have rows, set its default in the migration to `DateTime.UtcNow` (or backfill existing rows to their own `CreatedAt` via a raw SQL `UPDATE` in the migration's `Up()`, which is more correct — existing users shouldn't see all historical entries as unseen either).
3. Do not run `dotnet ef database update` — leave that to the user/CI per existing project convention.

## 3. Build wiring for CHANGELOG.md

1. Create `CHANGELOG.md` at repo root with a couple of seed entries (at minimum, one describing this feature once shipped) following the format documented in `requirements.md`:
   ```markdown
   ## 2026-09-16

   ### feature: What's new changelog
   GitBuddy now shows you what's changed in the app since your last visit.
   ```
2. `GitBuddy.Api/GitBuddy.Api.csproj`: add
   ```xml
   <ItemGroup>
     <Content Include="../CHANGELOG.md" CopyToOutputDirectory="PreserveNewest" Link="CHANGELOG.md" />
   </ItemGroup>
   ```
3. `GitBuddy.Api/Dockerfile`: add `COPY ["CHANGELOG.md", "."]` in the build stage (before `dotnet publish`) so it's present in `/app/publish`, mirroring the existing explicit per-file `COPY` style already used there.

## 4. Service

1. `GitBuddy.Api/Services/ChangelogService.cs`:
   - `interface IChangelogService { Task SyncFromFileAsync(); Task<ChangelogResponseDto> GetChangelogAsync(int userId); Task<DateTime> MarkSeenAsync(int userId); }`
   - `SyncFromFileAsync()`: reads `CHANGELOG.md` from `AppContext.BaseDirectory` (falls back to a no-op with a logged warning if the file is missing — don't throw and block startup). Parses `##` date headings and `###` `category: title` sub-headings, capturing everything until the next `###`/`##`/EOF as the body (trimmed). Computes `Slug` from `{publishedOn:yyyy-MM-dd}-{slugified title}`. For each parsed entry, upserts by `Slug`: insert if missing; if present, update `Category`/`Title`/`Body`/`PublishedOn` in place (never touch `CreatedAt`).
   - `GetChangelogAsync(userId)`: returns all `ChangelogEntries` ordered by `PublishedOn` desc then `Id` desc, plus the user's `LastSeenChangelogAt`.
   - `MarkSeenAsync(userId)`: sets `User.LastSeenChangelogAt = DateTime.UtcNow`, saves, returns the new value.
   - Mirror `UserService`'s constructor-injection style: `public class ChangelogService(AppDbContext context) : IChangelogService`.
2. Register in `Program.cs`: `builder.Services.AddScoped<IChangelogService, ChangelogService>();`
3. In `Program.cs`, right after the existing `MigrateAsync()` block, add a call to `SyncFromFileAsync()` using the same scope:
   ```csharp
   using (var scope = app.Services.CreateScope())
   {
       var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
       await dbContext.Database.MigrateAsync();

       var changelogService = scope.ServiceProvider.GetRequiredService<IChangelogService>();
       await changelogService.SyncFromFileAsync();
   }
   ```
4. Update `UserService.GetOrCreateGitHubUserAsync` (and `GetOrCreateDefaultUserAsync`) to set `LastSeenChangelogAt = DateTime.UtcNow` when creating a new `User`, so new signups start caught up.

## 5. DTOs

1. `GitBuddy.Api/DTOs/ChangelogDtos.cs`:
   ```csharp
   public record ChangelogEntryDto(string Slug, DateOnly PublishedOn, string Category, string Title, string Body);
   public record ChangelogResponseDto(IReadOnlyList<ChangelogEntryDto> Entries, DateTime LastSeenChangelogAt);
   public record MarkSeenResponseDto(DateTime LastSeenChangelogAt);
   ```

## 6. Controller

1. `GitBuddy.Api/Controllers/ChangelogController.cs`, mirroring `UserPreferencesController`:
   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   [Authorize]
   public class ChangelogController(IChangelogService changelogService) : ControllerBase
   {
       [HttpGet]
       public async Task<ActionResult<ChangelogResponseDto>> GetChangelog() { /* resolve userId from claim, 401 if missing, call service */ }

       [HttpPost("mark-seen")]
       public async Task<ActionResult<MarkSeenResponseDto>> MarkSeen() { /* same */ }
   }
   ```

## 7. Frontend types

1. `src/types/changelog.ts`:
   ```ts
   export interface ChangelogEntry {
     slug: string;
     publishedOn: string;
     category: 'Feature' | 'Improvement' | 'Fix';
     title: string;
     body: string;
   }
   export interface ChangelogResponse {
     entries: ChangelogEntry[];
     lastSeenChangelogAt: string;
   }
   ```

## 8. Frontend API client

1. Add to wherever `src/services/` (or `src/utils/api.ts`) already defines typed calls for other endpoints:
   - `getChangelog(): Promise<ChangelogResponse>` → `GET /api/changelog`
   - `markChangelogSeen(): Promise<{ lastSeenChangelogAt: string }>` → `POST /api/changelog/mark-seen`

## 9. Frontend composable

1. `src/composables/useChangelog.ts`, mirroring `useUserPreferences`'s load-once shape:
   - State: `entries: Ref<ChangelogEntry[]>`, `lastSeenChangelogAt: Ref<string | null>`.
   - `load()`: calls `getChangelog()`, populates state. Called once from `App.vue` on mount (guarded on `authStore.isAuthenticated`, same as other post-login loads).
   - `unseenEntries = computed(() => entries.value.filter(e => new Date(e.publishedOn) > new Date(lastSeenChangelogAt.value ?? 0)))`.

     Note: `publishedOn` is a date-only string; compare against `lastSeenChangelogAt` (a full timestamp) by treating the entry's effective instant as end-of-day UTC on `publishedOn`, so an entry published "today" is still unseen for a user who last visited earlier today. Simplest correct approach: compare `lastSeenChangelogAt` against `publishedOn` at UTC midnight *of the day after* `publishedOn` (i.e. `publishedOn < today` is definitely seen-eligible; same-day entries stay unseen until dismissed).
   - `markSeen()`: calls `markChangelogSeen()`, updates local `lastSeenChangelogAt` to the returned value.

## 10. Frontend components

1. `src/components/ChangelogEntryCard.vue`: one entry — category badge (local color map: Feature=blue, Improvement=violet, Fix=emerald, matching the existing semantic-accent conventions in `docs/reference/visual-style.md`), title, `DescriptionRenderer` for the body, `border-b border-slate-800` divider, no card framing.
2. `src/components/ChangelogModal.vue`: reuses whatever existing modal shell component other modals use (check `create-pr-modal.vue` for the pattern). Renders a list of `ChangelogEntryCard` for `unseenEntries`, single "Got it" primary button that calls `markSeen()` and closes. Mounted in `App.vue`, `v-if="authStore.isAuthenticated && unseenEntries.length > 0"`.
3. `src/views/ChangelogPage.vue`: full `entries` list via `ChangelogEntryCard`, calls `markSeen()` on mount (`onMounted`).
4. `App.vue`: add a small header link/icon (next to `UserMenu`) `router-link` to `/whats-new`, visible whenever `authStore.isAuthenticated` (not gated to dashboard route like the create-PR/refresh controls). Mount `<ChangelogModal />` alongside `<CreatePRModal>`.

## 11. Router

1. `src/router/index.ts`: add
   ```ts
   {
     path: '/whats-new',
     name: 'whats-new',
     component: ChangelogPage,
     meta: { requiresAuth: true },
   },
   ```

## 12. Validation

See `validation.md`.
