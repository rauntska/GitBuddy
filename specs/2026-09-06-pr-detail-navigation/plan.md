# PR Detail Navigation — Plan

Frontend-only. No Domain / Migration / Service / Controller / SignalR groups — see
`requirements.md` § API contract.

This branch already carries the rejected tabs implementation, so group 1 removes it before anything
new is built. Work the groups top to bottom; each leaves the app building.

---

## 1. Remove the tab machinery

1.1 Delete `gitbuddy-vue/src/components/pr-section-nav.vue`.

1.2 Delete `gitbuddy-vue/src/composables/usePRSection.ts`. Two pieces are salvaged, not discarded —
its reviewer lookup becomes group 6, its `?path=` handling becomes group 7. Read it before deleting.

1.3 Revert `gitbuddy-vue/src/router/index.ts` to the `master` version: path `/pr/:id`, props
`(route) => ({ id: Number(route.params.id) })`, no `isPRSection` import. `?path=` is a query and
needs no route entry.

1.4 In `types/index.ts`, delete `PRSection`, `PR_SECTIONS`, `isPRSection` and `PendingScroll`. Keep
`MergeMethod` — `pr-merge-button.vue` uses it.

1.5 In `views/PRDetail.vue`, remove the `section` prop, the `usePRSection` wiring, the `KeepAlive`
section outlet, `pendingScroll`, `filesRef` and `requestFilesScroll`. Render the overview block and
the files block stacked in one scroll instead.

1.6 In `pr-detail-header.vue`, remove the `section` prop, the `update:section` emit and the
`<pr-section-nav>` block. Keep the `--pr-header-h` `ResizeObserver` — group 4 depends on it.

1.7 In `pr-files.vue`, remove `pendingScroll`, `update:pendingScroll`, `consumePendingScroll` and
the `onActivated` hook. All three existed only because Files could be unmounted; it is always
mounted now. Keep `defineExpose({ scrollToFile, scrollToThread })` — the comments panel still needs
it. Keep `applyDeepLink`, it is reused in group 7.

1.8 Build. The app should be the pre-branch layout, minus the monolith.

---

## 2. `components/pr-context-rail.vue`

2.1 Move out of `pr-conversation.vue`: the Priority card, the `ReviewerManager` card, the
`ReviewTimeline` card. Add the `pr-checks.vue` card (group 3) between reviewers and timeline —
checks belong above history.

2.2 Props: `prDetail: PRDetail`, `settingPriority: boolean`. Emits: `priority-change`,
`reviewer-error`, `timeline-error` — pass-through, the shell keeps the handler bodies.

2.3 Give each card a stable id for group 5's scroll target: `pr-card-priority`,
`pr-card-reviewers`, `pr-card-checks`. Only one PR renders at a time, so ids are safe and avoid
drilling refs through two levels.

2.4 Add a `highlight(cardId)` method exposed via `defineExpose` that scrolls the card into view and
applies a `ring-2 ring-slate-500` class for ~1.2s. Keep the layout `lg:w-96 flex-shrink-0 space-y-4`
it has today.

2.5 Render a zero-height sentinel `<div ref="sentinelRef" />` as the rail's last child. Group 4
observes it.

---

## 3. `components/pr-checks.vue` — back to a card

3.1 Restore the original heading (`text-xs text-slate-400 uppercase tracking-wider`) and the
`mt-3 space-y-1 max-h-48 overflow-y-auto` cap on the run list. The full-width section styling added
for the Checks tab goes away.

Render it **content-only** — no `p-4 border rounded` on its own root — and let `pr-context-rail.vue`
supply the card chrome, the way it already wraps `ReviewerManager`. If the card owned its own
border, the rail's highlight ring (2.4) would fight it, since Tailwind class order in the built
stylesheet decides which `border-*` wins, not attribute order on the element.

3.2 Keep the empty state ("No checks reported for this PR") — it was an improvement over the
original, which rendered nothing.

3.3 Props and the `checksStatusLabel` / `getCheckStatusFromCheckRun` helpers are unchanged.

---

## 4. `composables/useCondensedRail.ts`

4.1 Signature: `useCondensedRail(sentinel: Ref<HTMLElement | null>)` → `{ condensed: Ref<boolean> }`.

4.2 `IntersectionObserver` on the sentinel with a negative top `rootMargin`, so the sentinel counts
as "gone" once it passes under the sticky chrome rather than at the viewport edge.

4.3 **Hysteresis — the thing that breaks this if skipped.** When the strip appears the header grows
by its own height and everything below shifts up, which can push the sentinel back into view and
un-condense, which shifts it down again, and so on: a flicker loop. The top `rootMargin` must
therefore be `-(appHeaderH + prHeaderH + stripH)` — i.e. it already accounts for the strip's height,
so the state change cannot undo its own trigger. Read the current header height from
`--pr-header-h` (group 1.6 keeps it) and use a constant for the strip.

4.4 Disconnect the observer in `onUnmounted`.

4.5 Guard `typeof IntersectionObserver === 'undefined'` by leaving `condensed` false — degrades to
today's behaviour rather than throwing.

---

## 5. `components/pr-context-strip.vue`

5.1 Props: `prDetail: PRDetail`. Emits: `jump-to(cardId: string)`.

5.2 A single `flex items-center gap-3 px-3 sm:px-5 py-1.5 text-xs` row of chip buttons, each
`hover:text-slate-100 transition-colors`:

| Chip | Content | Jumps to |
|---|---|---|
| Priority | `getPriorityGlyph` + `getPriorityColor` from `utils/prHelpers`, label hidden below `sm:` | `pr-card-priority` |
| Reviewers | `ReviewerAvatars.vue` (exists) | `pr-card-reviewers` |
| CI | `CIBadge.vue` (exists), `:compact="true"`, plus the run count | `pr-card-checks` |
| Unresolved | `💬 N` where N is unresolved **and not outdated**, hidden when 0 | `pr-card-reviewers` |

5.3 Labels collapse to glyphs below `sm:` using the `hidden sm:inline` idiom the header already
uses — see `requirements.md` § Open questions 3.

5.4 Wrap in a `<Transition>` with an opacity fade only. Do **not** animate height: an animating
sticky header re-triggers the observer mid-transition.

5.5 Accessibility: `role="toolbar"`, each chip a real `<button>` with an `aria-label` spelling out
the value (`Priority: High, jump to priority`).

---

## 6. `composables/useRequestedReviewer.ts`

6.1 Lift the reviewer check out of the deleted `usePRSection.resolveDefaultSection`:
`useRequestedReviewer(prId)` → `{ isRequestedReviewer: Ref<boolean>, check(): Promise<void> }`.

6.2 `check()` calls `apiService.getReviewers(prId)` and sets the flag when an entry has
`isRequested === true` and a case-insensitive `username` match against `authStore.username`.

6.3 try/catch → leave the flag `false` and log. A failed lookup must never hide the description.

6.4 Called once from the shell's `onMounted`, after `fetchPRDetail`.

---

## 7. `composables/useFileDeepLink.ts`

7.1 Lift `selectedFilePath` / `setSelectedFilePath` out of the deleted `usePRSection`, minus the
section params: read `route.query.path`, write it with
`router.replace({ name: 'pr-detail', params: { id }, query })`.

7.2 `pr-files.vue` keeps its `initialFilePath` prop and `applyDeepLink` watcher from group 1.7; the
shell now feeds it from this composable and handles `@select-file` by writing the query back.

---

## 8. `pr-conversation.vue` — collapsible description

8.1 Remove the right column entirely (moved to `pr-context-rail.vue` in group 2). What remains:
branches, description, general comments.

8.2 New prop `collapseDescription: boolean`, used only as the **initial** value of a local
`descriptionOpen` ref — once the user toggles it, the prop must not stomp their choice.

8.3 Collapsed state renders a single clickable row in place of `EditableDescription`:
`▸ Description` with the existing `text-xs font-semibold text-slate-300 uppercase tracking-wider`
styling, plus a `text-slate-500` hint of length. Expanding is instant, no height animation.

8.4 The "Merged - read only" indicator stays on the header row of the block, visible in both states.

---

## 9. `views/PRDetail.vue` — assemble

9.1 Render the overview block and the files block stacked, single scroll:
```
<pr-detail-header … />
<div class="border-b border-slate-800">
  <div class="flex flex-col lg:flex-row gap-4 lg:gap-6 p-4 sm:p-6">
    <pr-conversation … class="flex-1 min-w-0" />
    <pr-context-rail ref="railRef" … />
  </div>
</div>
<pr-files … />
```

9.2 Wire `useCondensedRail(railSentinel)` and pass `condensed` down to `pr-detail-header.vue`, which
renders `<pr-context-strip>` when true.

9.3 Handle `@jump-to` by calling the rail's exposed `highlight(cardId)`.

9.4 Wire `useRequestedReviewer` → `:collapse-description` on `pr-conversation`.

9.5 Wire `useFileDeepLink` → `:initial-file-path` on `pr-files`, and `@select-file` back to the
composable's setter.

9.6 The comments panel's `scrollToComment` / `scrollToThread` simplify back to direct calls on the
`pr-files` ref — it is always mounted, so the `pendingScroll` handshake removed in 1.7 is not
replaced by anything.

---

## 10. Validation

10.1 `cd gitbuddy-vue && npm run build` (runs `vue-tsc`) — zero errors.
10.2 `dotnet build -c Release` from the repo root — proves the branch stayed frontend-only.
10.3 Work the manual walkthrough in `validation.md` end to end, the condense/flicker checks first.
