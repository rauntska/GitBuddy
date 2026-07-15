# Priority System

How GitBuddy prioritizes pull requests and surfaces the most important ones first. Covers the hybrid priority model, how the effective value is computed, where it's stored, and how the UI consumes it.

---

## 1. Model: hybrid (manual override + derived score)

Priority is **hybrid**: every PR has an *effective* priority that is either a manual override (when set) or a score auto-derived from the PR's signals (when not set).

```
effectivePriority = manualOverride ?? deriveScore(pr)
```

- `PullRequest.Priority` is a nullable `int?`. `null` ⇒ derive; a set value is the manual override.
- The effective value is what the frontend displays and sorts by. The DTO carries both `Priority` (effective) and `PriorityOverridden` (bool) so the UI can show the override/derived distinction.

This keeps the dashboard correctly ordered by *default* (broken CI, conflicts, and stale-awaiting-review surface to the top automatically) while letting a human pin a PR as Urgent when intent matters — e.g. "this is for the release".

---

## 2. Priority scale

`PrPriority` constants live in `GitBuddy.Domain/Classifier/PrPriority.cs`:

| Value | Name   | Glyph | Tailwind color   |
|-------|--------|-------|------------------|
| `0`   | Low    | `▽`   | `text-slate-500` |
| `1`   | Normal | —     | `text-slate-400` |
| `2`   | High   | `▲`   | `text-amber-400` |
| `3`   | Urgent | `⏫`   | `text-red-400`   |

Normal (1) is the derived default. Low (0) is only reachable via explicit manual set.

---

## 3. Derived score logic

`PriorityService.GetEffectivePriorityStatic` (static so the static `MappingExtensions` can call it without DI) computes the score from **existing** PR columns — no extra GitHub calls:

- base `Normal (1)`
- `+1 signal` for each of:
  - `MergeableState == "conflicting"`
  - `ChecksStatus == "FAILURE"`
  - `Status == AwaitingReview` AND `UpdatedAt` > 3 days ago
- `1` signal ⇒ `High (2)`; `≥2` signals ⇒ `Urgent (3)`; else `Normal (1)`.

The derived value is always fresh — it recomputes on every DTO mapping from current DB state.

---

## 4. Manual override

A user sets/clears the override through:

- **Dashboard context menu** (right-click / "…" on a row) — *Set Priority: Urgent / High / Normal / Low*, plus *Clear Priority Override (Auto)*.
- **PR detail view** (`PRDetail.vue`) — a `<select>` in the right sidebar with `Auto (derived) / Low / Normal / High / Urgent`.

Both call `PATCH /api/pullrequests/{id}/priority` with `{ priority: int | null }` (`null` clears the override → back to derived). The backend (`SetPriorityHandler`) updates the row and broadcasts `PRPriorityChanged` over SignalR so every open view updates live.

---

## 5. Sorting

Within a status group, rows sort by effective priority (desc) then `UpdatedAt` (desc). The sort is applied in `Dashboard.vue`'s `sortedGroup(status)` helper, gated by the `prioritySort` user preference (default on). A toggle button in the dashboard toolbar (amber when active) flips it off to fall back to the API's natural order.

Group-level ordering (ReadyToMerge → AwaitingReview → …) is unchanged by priority; priority only reorders *within* a group.

---

## 6. Backend architecture

### Storage
- `PullRequest.Priority` (`int?`) — manual override; null = derive.
- `PullRequest.LastNudgedAt` (`DateTime?`) — rate-limit marker for the nudge flow (see the [Reviewer Nudge](#8-reviewer-nudge) section).
- `AppDbContext` indexes `Priority` for the sort.
- Migration `20260715131101_AddPriorityAndTeams`.

### Service
- `IPriorityService` / `PriorityService` (`GitBuddy.Api/Services/PriorityService.cs`)
  - `int GetEffectivePriority(PullRequest pr)` — instance method (DI use)
  - `bool IsOverridden(PullRequest pr)`
  - `static int GetEffectivePriorityStatic(PullRequest pr)` — used by `MappingExtensions` to avoid threading DI through every `ToDto` call site.

### DTOs + mapping
Effective priority is mapped in `MappingExtensions.ToDto`, `ToPRListUpdateDto`, and `ToDetailDto`. These records carry it:
- `PullRequestDto(int Priority, bool PriorityOverridden)`
- `PRDetailDto(...)`
- `PRListUpdateDto(int Priority, bool PriorityOverridden)` — the SignalR list-push shape

### Endpoint
`PATCH /api/pullrequests/{id}/priority` → `SetPriorityCommand` → `SetPriorityHandler`. Body `{ priority: int? }`. Returns `{ priority, overridden }`.

### SignalR
`BroadcastPRPriorityChangedAsync(int prId, int priority, bool overridden)` emits `PRPriorityChanged` (to `Clients.All`, dashboard list) and `PRPriorityChangedDetail` (to the PR detail group).

---

## 7. Frontend

### Types
`PullRequest.priority?: number` and `PullRequest.priorityOverridden?: boolean` (`src/types/index.ts`). `PRDetail extends PullRequest`, so detail inherits both.

### Helpers — `src/utils/prHelpers.ts`
| Export | Returns |
|---|---|
| `PRIORITY_LOW / NORMAL / HIGH / URGENT` | `0 / 1 / 2 / 3` constants |
| `getPriorityLabel(priority)` | `'Low' \| 'Normal' \| 'High' \| 'Urgent'` |
| `getPriorityGlyph(priority)` | `'▽' \| '' \| '▲' \| '⏫'` |
| `getPriorityColor(priority)` | tailwind text color class |

### Components
- **`PRRow.vue`** — a priority column at the front of the metadata strip. The column *always reserves its width* (for header alignment); the glyph only renders inside a `<template v-if="showPriority">` where `showPriority` = priority ≥ High. An override indicator dot (`•`) appears next to the glyph when `priorityOverridden`.
- **`PRColumnHeaders.vue`** — a matching `Prio` header column with the same `w-[24px]`/`w-[32px]` widths.
- **`PRDetail.vue`** — the priority `<select>` panel in the right sidebar (Auto / Low / Normal / High / Urgent), with an `auto`/`override` badge.

### State + real-time
- `useSignalR.ts` — `PRPriorityNotification` type, `onPRPriorityChanged` handler ref, and `connection.on('PRPriorityChanged' / 'PRPriorityChangedDetail', …)`.
- `usePullRequests.ts` — the handler patches the affected PR's `priority`/`priorityOverridden` in place across status buckets; reactivity re-sorts.
- `useUserPreferences.ts` — `prioritySort` preference (default `true`) + `setPrioritySort` setter.
- `api.ts` — `setPRPriority(prId, priority)` → `PATCH /pullrequests/{id}/priority`.

---

## 8. Reviewer nudge

Priority is paired with a reviewer nudge action that drives attention to a PR. (Nudge reaches reviewers via GitHub's native re-request review and, optionally, a Microsoft Teams card — see the Teams configuration in the admin GitHub settings panel.)

- **UI**: per-reviewer bell button and a "Nudge all" button in `ReviewerManager.vue` (shown in the PR detail sidebar). Dashboard context menu also has a *Nudge Reviewers* item.
- **Endpoint**: `POST /api/pullrequests/{id}/nudge` body `{ reviewers: string[], alsoComment?: bool }` → `NudgeReviewerHandler`.
- **Flow**: resolve user+token → rate-limit check on `LastNudgedAt` (60 min/PR) → optional `@mention` comment → Teams card → record `LastNudgedAt` → broadcast `ReviewerNudged`.
- **Rate-limit UX**: a rate-limited nudge returns a 400 with a message like *"This PR was nudged recently. Please wait ~32 minute(s)…"*, surfaced as a blue `toast.info`.

---

## 9. Reference files

- `GitBuddy.Domain/Classifier/PrPriority.cs` — priority constants
- `GitBuddy.Domain/Models/PullRequest.cs` — `Priority`, `LastNudgedAt` columns
- `GitBuddy.Api/Services/PriorityService.cs` — derived-score + effective logic
- `GitBuddy.Api/Features/PullRequests/SetPriority/` — set/clear override command
- `GitBuddy.Api/Features/PullRequests/NudgeReviewer/` — nudge command
- `GitBuddy.Api/Extensions/MappingExtensions.cs` — priority mapping
- `gitbuddy-vue/src/utils/prHelpers.ts` — priority helpers/constants
- `gitbuddy-vue/src/components/PRRow.vue` — priority badge column
- `gitbuddy-vue/src/components/PRColumnHeaders.vue` — `Prio` header
- `gitbuddy-vue/src/components/ReviewerManager.vue` — nudge buttons
- `gitbuddy-vue/src/views/Dashboard.vue` — sort + context menu
- `gitbuddy-vue/src/views/PRDetail.vue` — priority `<select>` panel
