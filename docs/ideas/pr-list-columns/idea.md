# Configurable PR List Columns

## Problem

The dashboard PR list is a table with a fixed, hard-coded column set. `PRColumnHeaders.vue` declares them literally — Repo, Author, Title, Prio, Size, CI, Merge, Reviews, Threads — each with a pinned pixel width (`w-[100px] sm:w-[140px]`) that `PRRow.vue` must mirror exactly for the columns to line up.

That produces three distinct problems:

- **No user control over what's shown.** A team that never uses priority still gives it a column; someone who cares only about CI and merge readiness cannot drop Author and Threads to give titles more room. `view-density-modes` changes how *tall* a row is and `dashboard-layout-customization` changes group order and visibility — neither changes *which columns* exist.
- **Responsive hiding is a guess.** Columns disappear at breakpoints via `hidden md:flex` / `hidden lg:flex`, so what you see is decided by window width rather than by what you care about. On a narrow window the reviewer column vanishes whether or not it was the point.
- **Every new signal fights for space.** `linked-issues` wants an issue chip, `stacked-pr-support` wants a stack indicator, `reviewer-availability` wants an away marker, `merge-collision-detection` wants a conflict warning. Each addition means editing two files in lockstep and squeezing the title further. The row is already at its budget.

## Rough Approach

### Column Registry

- Define columns once as data — key, label, width per density, minimum breakpoint, renderer — and drive both `PRColumnHeaders` and `PRRow` from that list. This alone removes the mirror-editing hazard, independent of any user-facing feature.
- New signals become a registry entry plus a small renderer component rather than surgery on two templates.

### User Selection

- A column picker in the dashboard toolbar: checkboxes to show/hide, drag to reorder within the metadata section. Title stays pinned as the flexible column; it is the one thing nobody wants to lose.
- Persist as `DashboardColumns` (JSON) on `UserPreferences`, alongside the existing `DashboardGroupOrder` and `HiddenDashboardGroups` — same shape, same pattern, no new storage mechanism.
- Sensible defaults preserve today's layout exactly, so users who never open the picker notice nothing.

### Overflow Behaviour

- Replace fixed breakpoint hiding with priority-based overflow: each column declares an importance, and the row drops the least important ones when space runs short — respecting the user's explicit choices first.
- Dropped values stay reachable in the row's hover card or the existing `ContextMenu`, so hiding a column never destroys information.

### Sorting

- Once columns are data, sortable headers are nearly free: click to sort by size, age, or priority. Note this interacts with the `PrioritySort` preference, which currently owns row ordering — an explicit column sort should override it and say so, not silently compete with it.

## Open Questions

- **Is this real demand or speculative flexibility?** The fixed set is defensible and preference surfaces have a cost. The column *registry* is worth doing regardless for the maintenance win; the picker may not be.
- **Per-group columns?** A Draft group arguably wants different columns than ReadyToMerge. Probably over-engineering, but worth asking once.
- **Interaction with `view-density-modes`** — expanded mode already renders a metadata bar rather than columns. Does column config apply there at all, or only to compact and comfortable?
- **Where does the picker live** — the dashboard toolbar is already carrying a density toggle, a column-headers toggle, and a customize button, and every idea adds another control. Should these consolidate into one "View" menu first?
- **Sort persistence** — is an explicit sort a saved preference, a session-scoped state, or part of a `saved-filters` definition?
