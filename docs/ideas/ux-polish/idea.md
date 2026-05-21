# Visual Polish & Micro-interactions

## Problem

The dashboard is functional but visually flat. PR rows, groups, and detail views lack the micro-feedback and polish that make a tool feel responsive and pleasant to use daily. Everything works — but it doesn't *feel* alive.

Specific pain points:
- Hover states are just background color swaps — no elevation, no shadows, no lift
- Status indicators are static dots with no animation for active/stale states
- List items appear instantly with no entrance transitions — hard to track what changed
- Empty states are generic — no personality or guidance
- Skeleton loaders don't match the actual content structure

## Rough Approach

### 1. Micro-interactions & Transitions

Give interactive elements tactile feedback:

- **Hover lift** on `PRRow` — subtle `translateY(-1px)` + shadow increase on hover, smooth `transition-all duration-200`
- **Button press** — slight `scale(0.97)` on `:active`, spring-back on release
- **Staggered list entrance** — when PR groups load or update, fade+slide items in with staggered delays (50ms between items) via CSS `@keyframes` or a lightweight Vue `<TransitionGroup>`
- **Group expand/collapse** — replace current `max-height` hack with smooth height animation using `grid-template-rows: 0fr → 1fr` or a shared composable

### 2. Animated Status Indicators

Make statuses glanceable through motion:

- **Pulsing dot** for PRs awaiting review beyond a threshold (e.g. 24h) — a subtle CSS `pulse` animation on the status dot to draw attention to stale PRs
- **CI progress** — show an animated gradient sweep on the check run badge while CI is running (similar to GitHub's progress bar), solid green/red when done
- **Unresolved threads** — a small animated indicator (gentle bounce) on PRs with unresolved review threads
- **New activity** — brief highlight flash on a PR row when a SignalR update changes it (e.g. new comment, review submitted), then fade to normal

### 3. Better Empty States

Replace generic empty state text with something contextual:

- Per-group empty states: "No PRs awaiting review — nice work!" vs. "No approved PRs yet today"
- Include a subtle SVG illustration or animated icon (not a stock image — something minimal that fits the dark theme)
- Suggest an action: link to create a PR, or explain what this section means

### 4. Content-aware Skeleton Loaders

Match skeleton shapes to what will actually render:

- Dashboard loading: skeleton PR rows with the right structure (avatar circle, title bar, label chips, reviewer avatar row)
- PR detail loading: skeleton header, skeleton tabs, skeleton diff area
- Use the same shimmer animation but with varied widths to suggest real content
- Existing shimmer CSS can be extended — just need component-specific skeleton templates

### 5. Visual Hierarchy Improvements

Subtle structural improvements without a full redesign:

- **Card elevation** on PR detail sections (diff panel, comments panel, review timeline) — `bg-slate-800` with a subtle border + shadow to separate from the background
- **Stronger status color usage** — status badges with tinted backgrounds, not just colored text (e.g. approved = green text on green-tinted bg)
- **Better spacing rhythm** — audit and standardize to an 8px grid for margins/padding across components
- **Tooltip enrichment** — add tooltips on reviewer avatars (last active), file count badges (list changed files on hover), and time displays (exact timestamp)

## Open Questions

- **Performance**: Staggered animations on large lists (100+ PRs) could be janky. Cap the animation to the first N visible items or use `IntersectionObserver`.
- **Animation library**: Can we stay CSS-only for all of this, or do we need something like `@vueuse/motion` for the entrance/exit transitions?
- **Preference toggle**: Should there be a "reduce motion" setting for users who find animations distracting? Respects `prefers-reduced-motion` at minimum.
- **Skeleton vs. optimistic**: For SignalR updates, should we show a skeleton flash or just update in-place with the highlight animation?
