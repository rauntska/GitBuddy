# PR Analytics — filter by author(s)

## Scope

### Included

Add a multi-select user filter to the PR Analytics panel ([AnalyticsPanel.vue](../../../gitbuddy-vue/src/components/analytics/AnalyticsPanel.vue) → [AnalyticsTab.vue](../../../gitbuddy-vue/src/components/analytics/AnalyticsTab.vue)). When one or more authors are selected, every section of the panel (Throughput, Reviewers, Health) scopes to PRs authored by those users.

**Filter semantics:** A PR counts toward the filtered view when its **author** is in the selected set. Reviewers/commenters are not part of the match condition.

**Affected sections:** All three — Throughput chart, Reviewer table, Health metrics.

**Dropdown source:** Derived from the analytics window — the list of authors who have at least one PR in the current date range. No new endpoint; the list ships alongside the existing reviewers response.

### API contract (changes to existing endpoints)

All three admin analytics endpoints gain an optional `authors` query parameter. `authors` empty or absent = no filter (unchanged behavior).

**`GET /api/admin/analytics/throughput`**
- Query params: `from?` (DateTime), `to?` (DateTime), `authors?` (string[], repeated: `?authors=a&authors=b`)
- `[Authorize(Roles = "Admin")]` — unchanged
- Response: `ThroughputAnalytics` — unchanged shape

**`GET /api/admin/analytics/reviewers`**
- Query params: `from?`, `to?`, `authors?`
- `[Authorize(Roles = "Admin")]` — unchanged
- Response: `ReviewerAnalytics` — **extended** with a new `Authors` field:
  ```csharp
  public record ReviewerAnalytics(
      IReadOnlyList<ReviewerStat> Reviewers,
      IReadOnlyList<AuthorOption> Authors);   // NEW

  public record AuthorOption(string Username, string? AvatarUrl);
  ```
  `Authors` is always populated from the unfiltered window (distinct PR authors in range), regardless of the `authors` filter value.

**`GET /api/admin/analytics/health`**
- Query params: `from?`, `to?`, `authors?`
- `[Authorize(Roles = "Admin")]` — unchanged
- Response: `HealthAnalytics` — unchanged shape

### Frontend surface

- New component `UserSelector.vue` in `gitbuddy-vue/src/components/analytics/` — multi-select with avatar chips, searchable dropdown.
- Rendered in `AnalyticsTab.vue` between `DateRangeSelector` and the chart sections.
- State lives in the existing `useAnalytics` composable alongside `preset`/`customFrom`/`customTo`.

### Not included

- URL/query-string persistence of the filter.
- Saving filter presets per user.
- Filtering by reviewer (rather than author) semantics.
- Cross-panel propagation (the Dashboard PR list is not affected).

## Decisions

- **Author-only match.** User confirmed: PR is "theirs" if they authored it. Reviewer/commenter activity alone does not bring a PR into scope.
- **All sections filter.** Throughput, Reviewers, and Health all scope to the selected authors' PRs.
- **Dropdown derived from window.** Authors with ≥1 PR in the current date range appear; the list updates when the range changes. This avoids a separate `/api/users` call and keeps the picker relevant to what's on screen.
- **Author list rides on the reviewers response.** Avoids an extra round-trip; the reviewers call already touches the PR table for the window.
- **Reviewer table semantics under filter:** the table shows, for each reviewer, their activity on the **selected authors' PRs** (reviews given, approvals, etc.) plus the selected author's own authored count. This keeps every metric in the panel computed over the same narrowed PR set.
- **Unifying rule:** every metric is computed over the narrowed PR set `{ pr in window | pr.Author ∈ selectedAuthors }`. No metric is computed per-author across unrelated PRs.

## Context

### Conventions to mirror

- Backend: `AnalyticsService` patterns at [AnalyticsService.cs](../../../GitBuddy.Api/Services/AnalyticsService.cs) — `AsNoTracking()`, `NormalizeWindow`, ordinal-ignore-case string compares in reviewer grouping.
- Controller: `[FromQuery] DateTime? from, [FromQuery] DateTime? to` style at [AdminController.cs:201-232](../../../GitBuddy.Api/Controllers/AdminController.cs).
- Composable: module-level refs + `watch()` auto-refresh, matching [useAnalytics.ts](../../../gitbuddy-vue/src/composables/useAnalytics.ts).
- Component styling: border-slate-800 / text-slate-300 dense buttons, matching [DateRangeSelector.vue](../../../gitbuddy-vue/src/components/analytics/DateRangeSelector.vue).
- Dropdown UX: borrow keyboard-nav / search ideas from [SearchableDropdown.vue](../../../gitbuddy-vue/src/components/SearchableDropdown.vue) but write fresh — `SearchableDropdown` is single-select.

### Open questions

- **Axios array serialization.** Confirm `apiService`'s configured `paramsSerializer` produces `?authors=a&authors=b` (ASP.NET `string[]?` binding). If it serializes as `?authors=a,b`, the backend will receive a single comma-joined string and the filter breaks. Verify before declaring done.
- **Selected-but-absent author.** When the date range changes and a previously selected author no longer appears in the window, the dropdown should keep them selected (chip visible, possibly dimmed) but they will contribute zero results. Acceptable behavior — flagged here as intentional.
