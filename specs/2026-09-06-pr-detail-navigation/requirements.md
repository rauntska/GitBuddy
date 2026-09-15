# PR Detail Navigation — Requirements

Branch: `feature/pr-detail-navigation`
Source idea: `docs/ideas/pr-detail-navigation/idea.md`

> **Supersedes the tabs design.** An earlier revision of this spec proposed routed
> Conversation / Files / Checks sections, and that version is implemented on this branch. It was
> rejected in review: tabs hide the priority/reviewer rail exactly when you are reading the diff,
> which inverts the idea doc's own goal of keeping decision-relevant facts visible *while* reading
> code — and Checks (a badge and a short list) does not earn a top-level destination. This revision
> keeps a single scroll and condenses the rail instead. Most of the component decomposition already
> done on the branch survives; see § Branch rework.

## The actual problem

Not "there is no navigation." Two narrower things:

1. **The context rail scrolls away.** Priority, reviewers, CI and the timeline sit in a normal-flow
   right column ([PRDetail.vue](gitbuddy-vue/src/views/PRDetail.vue), pre-refactor lines 392–482).
   By hunk three of file six they are two thousand pixels above you.
2. **The diff starts below a full-height description.** On a PR with a long body there is a lot of
   scrolling before any code appears.

## Scope

### In scope

**Frontend only.** No new API endpoints, no domain entities, no EF migration, no new SignalR
events. Everything needed is already served by `GET /api/pullrequests/{id}` and
`GET /api/pullrequests/{id}/reviewers`.

1. **Context rail, full size, on the first screen**
   - Priority, `ReviewerManager`, CI checks and `ReviewTimeline` stay where they are today: a
     `lg:w-96` right column in normal flow at the top of the page. First paint is unchanged.
   - Extracted into `pr-context-rail.vue` so the rail and its condensed form are one concept.

2. **Condensed strip when the rail scrolls off**
   - When the rail leaves the viewport, its facts reappear as a single ~32px row of chips inside
     the PR header, which is already `sticky top-20`:
     `⚑ High · 👤👤 · CI ✓4 · 💬 3 unresolved`.
   - Chips: priority (glyph + label), reviewers (`ReviewerAvatars.vue`, exists), CI
     (`CIBadge.vue`, exists), unresolved active threads.
   - Status, `2/3 approvals` and the unresolved count are **already** rendered in the header
     ([pr-detail-header.vue:19](gitbuddy-vue/src/components/pr-detail-header.vue:19)); the strip
     adds priority and reviewer avatars rather than re-inventing the row.
   - Once condensed, the rail's 384px goes back to the diff. The rail costs horizontal space only
     while you are reading the description, not while you are reading code.

3. **Chip click scrolls to the owning card**
   - Smooth-scroll the rail card into view and flash a brief highlight ring on it. No popovers.
   - Same behaviour for every chip, so there is nothing to learn per-chip.

4. **Description collapses for requested reviewers**
   - If the authenticated user is a requested reviewer, the description block starts collapsed as a
     one-line `▸ Description` summary that expands in place. Authors and everyone else see it
     expanded, as today.
   - Requested-reviewer check reuses `apiService.getReviewers(prId)`
     (`ReviewerStatus.isRequested === true`, case-insensitive `username` match against
     `authStore.username`). On failure, treat as "not a requested reviewer" and render expanded —
     a failed lookup must never hide content.
   - Known limitation: a review requested from a **team** (`ReviewerStatus.type === 'Team'`) cannot
     be matched to a username and will not trigger the collapse.

5. **`?path=` file deep link**
   - `/pr/:id?path=src/Program.cs` selects that file, expands it and scrolls to it.
   - Selecting a file in `FileTree` writes the path back with `router.replace` (replace, not push,
     so browsing the tree does not flood history).
   - A `path` matching no file in the PR is ignored — no error, no scroll.
   - Moves off `usePRSection` (deleted) into its own composable. Layout-independent.

6. **Sticky-offset correctness**
   - The header's height changes when the strip appears, and the file tree and its toggle rail
     stick beneath it. The `--pr-header-h` `ResizeObserver` already on this branch keeps them
     aligned; it was speculative under the tabs design and is load-bearing here.

### Out of scope

- Tabs, routed sections, per-section attention markers, lazy section mounting — rejected, removed.
- A pinned/always-sticky full rail: rejected, it permanently costs the diff 384px.
- Popovers on chips (see § Open questions for the CI case).
- Two-pane layouts, mobile breakpoint work (`docs/ideas/mobile-review`), virtualised diffs
  (`docs/ideas/large-diff-performance`), the full app-shell token pass beyond `--pr-header-h`.
- Any behaviour change to review submission, merge, priority, comments or viewed state — code moves
  between files, behaviour is identical.

## API contract

No new or modified endpoints. Consumed as-is:

| Endpoint | Use |
|---|---|
| `GET /api/pullrequests/{id}` | Already loaded by `usePRDetail.fetchPRDetail`; feeds rail, strip and diffs |
| `GET /api/pullrequests/{id}/reviewers` → `ReviewerStatus[]` | Requested-reviewer check for the description collapse (already used by `ReviewerManager`) |
| `GET /api/pullrequests/{id}/review-timeline` | Unchanged, called from `ReviewTimeline` inside the rail |
| `PUT /api/userpreferences` | Unchanged — `fileTreeWidth`, `commentsPanelWidth`, `diffViewMode`, `showContext`, `viewedFilesByPr` |

## Decisions

| Decision | Rationale |
|---|---|
| Condense the rail rather than tab it away or pin it | "Available" means *on the first screen*, not permanently sticky. Condensing keeps the facts reachable while giving the diff full width where it matters |
| Strip lives in the existing sticky header | The header is already `sticky top-20` and already renders three of the five facts — no new sticky machinery, no second sticky layer to fight the file tree |
| `IntersectionObserver` on a sentinel, not a scroll listener | No per-frame work, no layout thrash |
| Hysteresis baked into the observer threshold | A height-changing sticky header can oscillate: strip appears → content shifts up → sentinel re-enters → strip disappears → repeat. The condense threshold must already account for the strip's own height so the state change cannot undo its trigger |
| Chip click scrolls, no popovers | Uniform behaviour, no new component per chip, trivially reversible |
| Timeline gets no chip | It is history, not a decision input; an "N events" counter would not change what anyone does |
| Description collapse gated on requested-reviewer | The people who want the diff first are the people asked to review it; authors reopening their own PR want to see what they wrote |
| No `UserPreferences` field for any of this | No domain change, no migration; the collapse is derived per-visit from reviewer state, and the condense is pure scroll position |
| No backend work | Everything needed is already served; the branch stays independently shippable |

## Branch rework

The tabs implementation on this branch split `PRDetail.vue` along **content** lines, not tab lines,
so most of it stands. `PRDetail.vue` went 1,535 → 659 lines and stays there.

| Component | Fate |
|---|---|
| `pr-detail-header.vue` | Keep. Loses the section nav, gains `pr-context-strip.vue` |
| `pr-files.vue` | Keep. Loses `initialFilePath`/`pendingScroll` plumbing that existed only because Files could be unmounted |
| `pr-conversation.vue` | Keep, shrinks to branches + collapsible description + general comments; rail moves out |
| `pr-merge-button.vue`, `pr-review-modal.vue` | Keep, untouched |
| `pr-checks.vue` | Keep, restyled from a full-width section back to a rail card (`max-h-48 overflow-y-auto` restored) |
| `pr-section-nav.vue` | **Delete** (103 lines) |
| `usePRSection.ts` | **Delete** (98 lines); its reviewer lookup and `?path=` handling are salvaged into two new composables |
| `router/index.ts` | **Revert** to `/pr/:id`; `?path=` is a query and needs no route change |
| `--pr-header-h` observer | Keep, now load-bearing |

New: `pr-context-rail.vue`, `pr-context-strip.vue`, `useCondensedRail.ts`, `useFileDeepLink.ts`,
`useRequestedReviewer.ts`.

## Context

Conventions to follow:

- Vue 3 `<script setup lang="ts">`, Tailwind, `slate-*` dark palette — mirror the markup being
  moved rather than restyling it.
- New components use **kebab-case** filenames, matching `comment-thread.vue`,
  `file-diff-header.vue` and the components already added on this branch.
- Shared state stays in composables (`usePRDetail`, `useUserPreferences`), not a Pinia store — only
  `auth` is a store today.
- Anything a parent must call on a child is exposed with `defineExpose`, as `FileDiffViewer`
  already does.

Existing code the new work mirrors:

- [pr-detail-header.vue](gitbuddy-vue/src/components/pr-detail-header.vue) — the sticky header and the `--pr-header-h` observer
- [ReviewerAvatars.vue](gitbuddy-vue/src/components/ReviewerAvatars.vue) and [CIBadge.vue](gitbuddy-vue/src/components/CIBadge.vue) — the compact forms the chips need
- [useUserPreferences.ts](gitbuddy-vue/src/composables/useUserPreferences.ts) — persisted widths/modes
- [types/index.ts](gitbuddy-vue/src/types/index.ts) — `PRDetail`, `ReviewerStatus`, `UserPreferences`

## Open questions

1. **The collapse covers the description only.** The question that settled this showed a summary
   line reading `▸ Description · 8 general comments`, i.e. both blocks collapsed. Narrowed
   deliberately: general comments are the thing a returning reviewer most wants to see, and hiding
   them would defeat the purpose of an overview screen. Easy to widen if the overview still feels
   too tall in practice.
2. **CI is the one chip that might want a popover** — seeing *which* check failed without leaving
   your place in the diff is worth more than the other four. Deferred: ship scroll-to-card for all
   five, add a popover for CI only if it is missed.
3. **Strip contents on narrow screens.** Below `lg:` the rail already stacks rather than sitting in
   a column, so it scrolls off sooner and the strip matters more. Four chips fit at ~375px only if
   labels drop to glyphs; the header already uses `hidden sm:inline` for exactly this and the strip
   should follow suit.
