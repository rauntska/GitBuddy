# Saved Filters & Smart Views

## Problem

The dashboard currently groups PRs by review status (ReadyToMerge, AwaitingReview, Approved, ChangesRequested, Reviewed, Draft) with support for reordering and hiding groups. But there's no way to create custom filtered views. As organizations and PR volume grow, a single flat status-grouped list becomes noisy.

Common workflows that the current grouping doesn't support:

- "Show me PRs where I'm a reviewer and haven't commented yet" — needs reviewer + activity filtering
- "Show me PRs from the frontend team that have been open for > 3 days" — needs author group + age filtering
- "Show me only PRs with failing CI" — needs check status filtering
- "Show me PRs in the `api-service` repo that are ready to merge" — needs repo + status cross-filter
- "Show me PRs I've authored that still need approvals" — needs author + approval status filtering

Users have to visually scan the entire dashboard and mentally filter, which doesn't scale.

## Rough Approach

### Filter Bar

A filter bar at the top of the dashboard with combinable criteria:

- **Author**: Filter by PR author (select from org members)
- **Reviewer**: Filter by assigned reviewer (select from org members)
- **Repository**: Filter by repository (select from tracked repos)
- **Status**: Filter by PR status (multi-select from existing statuses)
- **Draft**: Include/exclude drafts
- **Age**: PR age filter (e.g. "opened in last 24h", "older than 3 days", "older than 7 days")
- **CI Status**: Filter by check run status (passing, failing, pending, no checks)
- **Labels**: Filter by GitHub labels (multi-select)
- **Has unresolved threads**: Show only PRs with unresolved review threads
- **Has merge conflicts**: Show only PRs with merge conflicts

All filters combine with AND logic. The bar should be collapsible to save space when not in use.

### Saved Views

Allow users to save a filter combination as a named "View":

- **Save current filters** as a named view (e.g. "My Stale Reviews", "Frontend Team PRs", "Ready to Merge This Week")
- **View tabs**: Saved views appear as tabs above the PR list, alongside the default "All" view
- **Quick switch**: Click a tab to instantly apply that filter set
- **Default view**: Users can set one saved view as their default (opens automatically on dashboard load)
- **Sharing**: Optionally share views with other team members (read-only, they can duplicate to customize)

### Preset Smart Views

System-provided views that don't require configuration:

- **Needs My Attention**: PRs where the current user is a reviewer and hasn't approved/changes-requested yet, or PRs the user authored that have new reviews/comments
- **Stale PRs**: PRs with no activity (comments, reviews, commits) in the last N days (configurable, default 3)
- **Ready to Merge**: PRs where all checks pass, required approvals met, no unresolved threads, no merge conflicts
- **Blocked**: PRs with failing CI, merge conflicts, or unresolved change requests
- **Recently Updated**: PRs with new activity in the last 24 hours

### Data Model Sketch

```
SavedFilterView
├── Id                    (int, PK)
├── UserId                (int, FK to User)
├── Name                  (string, e.g. "My Stale Reviews")
├── Filters               (string, JSON-serialized filter criteria)
├── IsDefault             (bool)
├── IsShared              (bool)
├── SortOrder             (int)
├── CreatedAt             (DateTime)
├── UpdatedAt             (DateTime)

FilterCriteria (JSON structure stored in Filters column)
├── Authors               (string[]?, GitHub usernames)
├── Reviewers             (string[]?, GitHub usernames)
├── Repositories          (string[]?, repo full names)
├── Statuses              (string[]?, PR status enum values)
├── IncludeDrafts         (bool?, default true)
├── MinAgeDays            (int?, minimum PR age in days)
├── MaxAgeDays            (int?, maximum PR age in days)
├── CiStatus              (string?, "passing"/"failing"/"pending"/"none")
├── Labels                (string[]?, GitHub label names)
├── HasUnresolvedThreads  (bool?)
├── HasMergeConflicts     (bool?)
```

### Backend Changes

- New `SavedFilterViewsController` with CRUD endpoints: `GET`, `POST`, `PUT`, `DELETE /api/filter-views`
- New `SavedFilterView` entity and EF Core migration
- Extend `PullRequestsController.GetAll()` to accept filter query parameters (most can be applied in-memory on the already-fetched PRs, or translated to DB queries for performance)
- Smart view "Needs My Attention" requires knowing the current user's GitHub username (available from `User` model)

### Frontend Changes

- New `FilterBar.vue` component with dropdown/collapsible filter controls
- New `FilterViewTabs.vue` component for saved view tab bar
- Extend `Dashboard.vue` to apply active filters to the grouped PR data
- Extend `useUserPreferences.ts` (or new `useFilterViews.ts` composable) for saved view management
- Store active filters in URL query params for shareable links (e.g. `?view=my-stale-reviews` or `?author=alice&status=AwaitingReview`)

### Performance Considerations

- Filters should operate on the already-loaded PR data (client-side filtering) for instant response with small-to-medium PR counts
- For orgs with 500+ PRs, consider server-side filtering with query parameters to avoid loading everything
- Saved views load the filter config, then the existing data pipeline runs — no separate data fetching needed
- URL-based filter state enables deep linking and bookmarking

## Open Questions

- **Server-side vs. client-side filtering**: Start with client-side (simpler, instant), but add server-side for scale. What's the threshold? 200 PRs? 500?
- **Filter persistence**: Should unsaved filter state persist across page reloads (e.g. in localStorage), or reset to default view each time? Likely persist in URL + localStorage.
- **Smart view customization**: Should users be able to edit the preset smart views (e.g. change "Stale" from 3 days to 7 days), or are those fixed? Likely allow duplicating and customizing.
- **Dashboard group interaction**: When filters are active, do status groups still show? Options: (a) filter within groups, (b) flatten results into a single list, (c) show a "Filtered" group. Filtering within existing groups seems most natural.
- **Cross-org filtering**: If the app ever supports multiple orgs, filters need an org dimension. Should the data model account for this now? Likely yes — add optional `Organization` to filter criteria.
- **Negative filters**: Should users be able to exclude (e.g. "NOT author:alice")? Adds complexity but is useful. Likely v2.
- **Saved view permissions**: If views are shared, should the owner be able to prevent others from editing, or is it copy-on-write (others duplicate to customize)?
