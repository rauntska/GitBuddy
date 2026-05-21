# Visual Polish & Micro-interactions

## Context

The dashboard is functional but visually flat. Hover states are background swaps with no elevation. Status dots are static circles. Lists appear instantly with no entrance rhythm. Empty states are generic. Skeleton loaders are simple shimmer bars that don't match the PR row structure. The goal is to make the existing UI feel responsive and alive without a redesign — just polish layered on top of the current dark theme.

## New/Modified Files

| File | Action | Purpose |
|------|--------|---------|
| `src/styles/animations.css` | **New** | Shared keyframes and animation utilities (pulse, fade-slide, shimmer sweep) |
| `src/components/SkeletonPRRow.vue` | **Modify** | Restructure to match actual PR row layout |
| `src/components/PRRow.vue` | **Modify** | Add hover lift, activity flash highlight |
| `src/components/PRGroup.vue` | **Modify** | Fix collapse animation, add empty state per-group |
| `src/components/StatusDot.vue` or inline in PRGroup | **Modify** | Add pulse animation for stale PR groups |
| `src/views/Dashboard.vue` | **Modify** | Add `<TransitionGroup>` for staggered entrance, update empty states |
| `src/views/PRDetail.vue` | **Modify** | Add card elevation on sections, better loading skeleton |
| `src/components/ReviewTimeline.vue` | **Modify** | Add entry fade-slide animation |
| `src/components/ReviewerAvatars.vue` | **Modify** | Replace `title` attributes with proper tooltips |

## Architecture

### 1. Shared Animation Utilities (`animations.css`)

A single CSS file with `@keyframes` and utility classes consumed across components. No JS animation library — CSS handles everything.

```css
/* Fade + slide up for list item entrance */
@keyframes fadeSlideIn {
  from { opacity: 0; transform: translateY(8px); }
  to   { opacity: 1; transform: translateY(0); }
}

/* Subtle pulse for stale status dots */
@keyframes subtlePulse {
  0%, 100% { opacity: 1; }
  50%      { opacity: 0.5; }
}

/* Horizontal shimmer sweep for CI running state */
@keyframes shimmerSweep {
  0%   { background-position: -200% 0; }
  100% { background-position: 200% 0; }
}

/* Flash highlight for new activity */
@keyframes activityFlash {
  0%   { background-color: rgba(59, 130, 246, 0.15); }
  100% { background-color: transparent; }
}

/* Fade in for review timeline entries */
@keyframes fadeIn {
  from { opacity: 0; transform: translateX(-4px); }
  to   { opacity: 1; transform: translateX(0); }
}
```

Import once in `src/style.css`:
```css
@import './styles/animations.css';
```

All animations respect `prefers-reduced-motion`:
```css
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    transition-duration: 0.01ms !important;
  }
}
```

### 2. Hover Lift on PRRow

Current: `hover:bg-slate-800 hover:shadow-lg transition-all duration-200 ease-out`

Change to add subtle vertical lift and shadow:
- `hover:-translate-y-px` (1px upward)
- `hover:shadow-xl` (stronger shadow than current `shadow-lg`)
- Keep existing status-colored shadow: `hover:shadow-{color}-900/20`

The effect is barely noticeable individually but creates a tactile feel when scanning rows.

### 3. Activity Flash Highlight

When a SignalR update modifies a PR row (new comment, review submitted, status change), briefly flash the row background:

- Add a reactive `flashKey` ref on `PRRow.vue` that increments when SignalR data changes for that PR
- Watch `flashKey` → apply `.activity-flash` class → remove after 800ms via `setTimeout`
- `.activity-flash` uses the `activityFlash` keyframe from `animations.css`

This replaces the current behavior where rows silently update with no visual feedback.

### 4. Staggered List Entrance

When PR groups load or update, fade+slide items in sequentially:

- Wrap PR list in Vue `<TransitionGroup name="pr-list">`
- Each `PRRow` gets `style="animation-delay: ${index * 50}ms"` via inline style binding
- `.pr-list-enter-active` uses `fadeSlideIn` animation with the per-item delay
- Cap at first 20 items — items beyond that appear instantly (avoids jank on large lists)
- `<TransitionGroup>` handles add/remove/move animations for reorders

### 5. Smooth Group Collapse

Current: `max-height` hack with hardcoded pixel values in `PRGroup.vue`.

Replace with `grid-template-rows` approach — no JS measurement needed:

```css
.collapse-wrapper {
  display: grid;
  grid-template-rows: 1fr;
  transition: grid-template-rows 0.3s ease-out;
}
.collapse-wrapper.collapsed {
  grid-template-rows: 0fr;
}
.collapse-wrapper > .collapse-inner {
  overflow: hidden;
}
```

Remove the existing `<Transition name="collapse">` CSS and use this pattern instead. Eliminates the need for a hardcoded `max-height` value.

### 6. Stale Status Pulse

PR groups where all PRs have been in "Awaiting Review" for >24h get a pulsing status dot:

- In `PRGroup.vue`, compute `isStale` based on the oldest PR's `CreatedAt` in the group
- When `isStale`, apply `subtlePulse` animation to the status dot (`w-3 h-3 rounded-full` element)
- Pulse interval: 2s cycle, subtle opacity oscillation (not a jarring blink)

### 7. Content-aware Skeleton Loaders

Current `SkeletonPRRow` is generic shimmer bars. Restructure to match actual PR row layout:

```
┌──────────────────────────────────────────────────────────┐
│  ○  [═══════════ title ═══════════]  [label]  [label]    │
│     repo/name  •  2h ago  •  +128/-42                    │
│     [avatar][avatar] [+2]                [badge] [badge] │
└──────────────────────────────────────────────────────────┘
```

- Avatar placeholder: `w-5 h-5 rounded-full bg-slate-700 shimmer`
- Title bar: `h-4 bg-slate-700 rounded shimmer` with randomized width (60-80%)
- Label chips: 2 small rounded rectangles with different widths
- Reviewer row: 3 small circles with `-space-x-1`
- Right side badges: 2 small rectangles

Same shimmer animation from `Dashboard.vue`, just structured to match real content.

### 8. Better Empty States

Replace generic "No PRs" with per-group contextual messages:

| Group | Empty Message |
|-------|---------------|
| Awaiting Review | "All caught up — no PRs waiting for review" |
| Changes Requested | "No PRs with changes requested" |
| Approved | "No approved PRs ready to merge" |
| Draft | "No draft PRs" |

Each empty state:
- Uses the group's status color as a subtle accent (e.g. blue icon for Awaiting Review)
- Shows a small SVG icon (checkmark, coffee cup, etc.) — minimal, dark-theme-appropriate
- No illustration library — inline SVG, 2-3 paths max

### 9. PR Detail Card Elevation

Current detail sections blend into the background. Add subtle card treatment:

- Sections (diff panel, comments panel, review timeline): `bg-slate-800/50 border border-slate-700/30 rounded-xl`
- Already partially done in PRDetail.vue (gradient cards exist) — extend consistently to ALL sections
- Add `shadow-sm` to create a slight elevation from the `bg-slate-900` page background

### 10. Review Timeline Entry Animations

When review timeline entries load or new ones arrive via SignalR:

- Each entry gets `fadeIn` animation with 30ms stagger delay
- Existing entries remain static — only new entries animate in
- Keep current timeline line and dot styling unchanged

### 11. Reviewer Avatar Tooltips

Current: native `title` attributes on avatar `<img>` tags. These have a ~1s hover delay and can't be styled.

Replace with a lightweight tooltip approach:
- Use Vue's built-in `<Teleport>` to render a single shared tooltip component at body level
- Show on `mouseenter` / hide on `mouseleave` on each avatar
- Display: reviewer name, review state, and time since review
- Style: `bg-slate-700 text-sm text-slate-200 rounded px-2 py-1 shadow-lg`
- No library needed — a tiny `useTooltip` composable that tracks mouse position and content

## Scope Exclusions

- **No light theme** — dark only
- **No keyboard shortcuts** — separate concern
- **No drag-and-drop reordering** — separate concern
- **No layout restructuring** — all changes are additive CSS/template tweaks
- **No new dependencies** — CSS animations + Vue built-in transitions only
- **No color palette changes** — use existing Tailwind slate/status colors

## Verification

1. **Hover lift**: Hover over a PR row → row lifts 1px and shadow deepens → smooth transition
2. **Activity flash**: Have a collaborator submit a review on a visible PR → row flashes blue briefly → fades to normal
3. **Staggered entrance**: Refresh dashboard → PR groups fade in sequentially, rows within groups stagger at 50ms intervals
4. **Collapse animation**: Click a PR group header → smooth height collapse using grid rows → no max-height jump
5. **Stale pulse**: Find/create a PR in "Awaiting Review" for >24h → group's status dot pulses subtly
6. **Skeleton structure**: Load dashboard on slow connection → skeleton rows match PR row layout (avatar, title, labels, reviewers, badges)
7. **Empty states**: Navigate to a group with no PRs → contextual message with icon appears
8. **Card elevation**: Open PR detail → sections have visible card borders and shadow separating them from background
9. **Timeline animations**: Open PR detail with review timeline → entries fade in with stagger → new reviews animate in
10. **Avatar tooltips**: Hover a reviewer avatar → styled tooltip appears immediately with name + state
11. **Reduced motion**: Enable `prefers-reduced-motion` in OS settings → all animations disabled, UI still functional
