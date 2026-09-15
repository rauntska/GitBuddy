# PR Detail Navigation — Validation

This branch reworks a 1,535-line component with no test coverage, twice. The manual walkthrough
below is the safety net — run all of it.

## Automated

```bash
cd gitbuddy-vue && npm run build
```

```bash
dotnet build -c Release
```

- `npm run build` runs `vue-tsc`; type errors fail the build. Zero errors, zero new warnings.
- The .NET build must be unaffected — it is run to prove the branch is frontend-only.
- No `dotnet test` step: no test project covers this area (see `docs/ideas/automated-test-foundation`).

## Correctness checks

- [ ] `git diff master --stat` shows changes only under `gitbuddy-vue/src/` and `specs/`. No
      `GitBuddy.Domain/Migrations/`, no `GitBuddy.Api/`.
- [ ] `router/index.ts` is identical to `master` — the tabs route was reverted.
- [ ] `pr-section-nav.vue` and `usePRSection.ts` are gone; `PRSection`, `PR_SECTIONS`,
      `isPRSection` and `PendingScroll` are gone from `types/index.ts`.
- [ ] No new entries in `UserPreferences` (`types/index.ts`,
      `GitBuddy.Domain/Models/UserPreferences.cs`).
- [ ] No new `apiService` methods; `getReviewers` is reused.
- [ ] No `console.log` in the new or moved code.
- [ ] `useCondensedRail` disconnects its observer in `onUnmounted`.
- [ ] Every callback the diff viewer needs still reaches it — `grep` `pr-files.vue` for
      `onAddComment`, `onDeletePendingComment`, `onReplyToThread`, `onResolveThread`,
      `onEditComment`, `onDeleteComment`, `onToggleViewed`.

## Manual walkthrough

Start `dotnet run` in `GitBuddy.Api` and `npm run dev` in `gitbuddy-vue`, sign in, and pick a PR
with a long description, several changed files, at least one review thread, and CI checks.

### The condense behaviour — do this first

1. Open the PR. The rail is full size on the first screen: priority, reviewers, checks, timeline.
   No strip in the header.
2. Scroll slowly until the rail's bottom passes under the header. The strip fades in; the rail's
   384px is released and the diff column widens.
3. **Flicker check.** Scroll slowly back and forth across the transition point ten times, then do
   it fast. The strip must not oscillate, and the page must not jitter. This is the hysteresis in
   `useCondensedRail`; if it is wrong, it shows here and nowhere else.
4. Scroll back to the top. The strip fades out, the rail is full size again.
5. With the strip showing, resize the window narrower and wider across `lg:` — no flicker, no
   overlap between the strip and the header's buttons.
6. At ~375px wide, the chips show as glyphs without wrapping the header into three rows.

### Chip behaviour

7. With the strip showing, click the priority chip → scrolls to the top and the Priority card
   flashes a highlight ring.
8. Repeat for reviewers, CI, and the unresolved chip. Each lands on its own card.
9. Set priority to High from the rail card, scroll down → the chip reads High. Change it from
   another browser session → the chip updates live over SignalR.
10. Re-run a check on GitHub (or replay a `check_run` webhook) → the CI chip and the rail card both
    update without a reload.
11. Resolve a thread from another session → the unresolved chip decrements, and disappears at 0.

### Description collapse

12. Open a PR **where you are a requested reviewer**, with a long description → description starts
    collapsed as `▸ Description`; general comments are still visible; the diff is roughly one
    scroll away.
13. Click the summary row → expands in place, no scroll jump.
14. Open a PR where you are **not** a requested reviewer → description starts expanded.
15. Open your own PR → expanded.
16. Stop the API, then open a PR (so `getReviewers` fails) → description renders **expanded**, page
    otherwise fine. A failed lookup must never hide content.
17. Expand the description, then scroll down and back up → it stays expanded (the prop must not
    stomp the local toggle).

### File deep link

18. Click a file in the tree → URL gains `?path=<file>`, diff scrolls to it. Reload → same file
    selected and scrolled.
19. Copy that URL into a new tab → opens with that file selected.
20. `?path=does/not/exist.cs` → page loads normally, nothing scrolls, no console error.
21. Browse ten files in the tree, then press Back once → leaves the PR rather than stepping back
    through ten file selections (`router.replace`, not `push`).

### Regression sweep

22. **Files** — rail toggle hides/shows the tree and persists across reload; drag the resize handle,
    reload, width persists; toggle a file viewed → checkmark in the tree, survives reload; diff
    settings switch Unified/Split and toggle context; add an inline comment then delete it from the
    review modal's draft list; reply to a thread and resolve/unresolve it.
23. **Comments panel** — opens from the header, resize persists across reload; clicking a comment
    and a review thread both scroll the diff to the right place.
24. **Header** — edit and save the title; merge dropdown opens, lists allowed methods, shows the
    right ready/blocked text, closes on outside click; Publish works on a draft PR; "Refresh"
    updates viewed checkmarks.
25. **Review** — submit a Comment review with an empty body → blocked; with a body → success toast
    and the Review button loses its badge.
26. Scroll deep into a long diff → header, strip and file tree stay aligned with no gap or overlap
    (this is what `--pr-header-h` protects).
27. Open a PR the API 500s on → error state with a working Retry.

## Definition of done

- Both builds pass clean.
- Every checkbox and numbered step above verified, step 3 especially.
- `PRDetail.vue` stays at roughly its current 659 lines or below, and contains no `FileDiffViewer`,
  `FileTree`, `GeneralComments`, `ReviewerManager`, `ReviewTimeline` or `CIBadge` usage.
- No tab machinery remains anywhere in the tree.
- `docs/ideas/pr-detail-navigation/idea.md` is promoted to
  `docs/specs/pr-detail-navigation/design.md` with a `> **Status: Implemented**` marker, its
  segmented-control proposal replaced by the condensing rail, and its open questions resolved or
  restated — per the `docs/README.md` lifecycle.
