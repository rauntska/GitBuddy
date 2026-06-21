# Visual Style Guide — "Dense / Pro"

The shared visual language for the Graphite frontend. Apply these rules when restyling any view, component, or surface so the whole app feels consistent.

---

## 1. Principles

- **Signal over decoration.** Status is conveyed by small, precise marks (dots, glyphs, tints) — not saturated fills or glow shadows.
- **Typographic hierarchy carries meaning.** Font size and color do the work; reserve font-weight for rare emphasis.
- **Numbers are data.** Tabular monospace numerics everywhere a value will be compared across rows.
- **Calm surface.** Borders are tight and dark-on-dark; surfaces barely differ from the background.

---

## 2. Color

### Base
| Token | Value | Use |
|---|---|---|
| `bg-slate-900` | `#0f172a` | Page background |
| `bg-slate-800/40` | — | Hover surface |
| `border-slate-800` | — | Default border (tight, calm) |
| `border-slate-700` | — | Hover border |
| `text-slate-200` | `#e2e8f0` | **All text** (default color) |

Do **not** use `text-slate-300/400/500/600` for body text. Body text is always `text-slate-200`. Use opacity or weight for hierarchy, not dimmed text colors.

### Status row tints (very faint)
Used as the row's background fill, replacing the old neutral `bg-slate-800/50`:

| Status | Class |
|---|---|
| AwaitingReview | `bg-blue-950/20` |
| Approved | `bg-emerald-950/20` |
| ChangesRequested | `bg-orange-950/20` |
| Reviewed | `bg-violet-950/20` |
| Draft / neutral | `bg-slate-800/30` |

Helper: `getStatusTintClass(status)` in `src/utils/prHelpers.ts`.

### Semantic accent colors
Used on numerals, glyphs, and small indicators only — never as large fills.

| Meaning | Color |
|---|---|
| Additions / pass / success | `text-emerald-400` |
| Deletions / fail / error | `text-red-400` |
| Pending / waiting | `text-amber-400` |
| Pending comments | `text-orange-400` |
| Stale | `text-amber-400` |

---

## 3. Typography

- **Font weight:** default (`font-normal`) everywhere. Do not apply `font-medium` / `font-semibold` / `font-bold` to body text. Rare exceptions: group titles use `font-semibold` (uppercase, small).
- **Font family:** system sans default. Use `font-mono` for **all** numbers and code-like identifiers: PR numbers (`#482`), timestamps (`3h`, `1d`), line counts (`+128 −34`), file counts, thread counts, reviewer counts, stats totals.
- **Tabular numerics:** `tabular-nums` (or the global `tnum` class from `src/style.css`) on any column of numbers that should align across rows.
- **Sizes:**
  - Body / row content: `text-sm` (comfortable) or `text-xs` (compact)
  - Page titles / expanded rows: `text-base`
  - Section headings (group headers): `text-sm`, uppercase, `tracking-wider`
  - Tiny metadata / hints: `text-[11px]`

---

## 4. Status glyphs

One-character glyphs replace SVG icons in compact contexts. Helper: `getStatusGlyph(status)` and `getCIGlyph(checksStatus)`.

| State | Glyph | Color |
|---|---|---|
| Pass (CI) | `✓` | `text-emerald-400` |
| Fail (CI) | `✗` | `text-red-400` |
| Pending (CI) | `⋯` | `text-amber-400` |
| None (CI) | `–` | `text-slate-500` |
| Approved (review state) | `✓` | `text-emerald-400` |
| Commented | `◐` | `text-violet-400` |
| Changes requested | `✕` | `text-orange-400` |
| Awaiting review | `●` | `text-blue-400` |
| Draft | `○` | `text-slate-500` |
| Stale | `⚑` | `text-amber-400` |

Render glyphs in `font-mono` so they align across rows.

---

## 5. Borders, surfaces, hover

- **Row border:** `border border-slate-800 rounded`
- **Row hover:** `hover:bg-slate-800/40 hover:border-slate-700 hover:-translate-y-px transition-all duration-150 ease-out` — a subtle 1px lift with a brighter border. **Never use colored hover glows** (`hover:shadow-{color}-900/20`). Those are removed and `getStatusShadowClass` returns empty.
- **Section dividers:** thin top rule via `border-b border-slate-800`. Avoid card-on-card framing.
- **Group header:** `border-b border-slate-800 py-1 mb-1`, no card background.

---

## 6. Spacing rhythm

| Element | Spacing |
|---|---|
| Between groups | `space-y-3` (12px) |
| Group bottom margin | `mb-3` (12px) |
| Between rows inside a group | `space-y-1` (4px) |
| Row padding — compact | `py-1 px-2` |
| Row padding — comfortable | `py-2 px-3` |
| Row padding — expanded | `py-3 px-4` |

Tight, scannable. The eye should be able to track vertically without long jumps.

---

## 7. Density modes

All three modes share the language above. They differ only in padding and what metadata shows.

| Mode | Layout | Padding | Notes |
|---|---|---|---|
| Compact | single line | `py-1 px-2` | Secondary cols hide below `md` |
| Comfortable | single line | `py-2 px-3` | All cols visible |
| Expanded | two-line | `py-3 px-4` | Title row + metadata bar; avatar stack for reviewers |

Density toggle itself: a `border border-slate-800 bg-slate-900/60` segmented control with single-letter (`C` / `M` / `E`) buttons in `font-mono text-xs`.

---

## 8. Component patterns

### PRRow (canonical reference)
- Root: `border border-slate-800 rounded` + `getStatusTintClass(status)` + hover classes above
- Repo/PR# block: repo name `text-slate-200` (no weight), PR# in `font-mono`
- Title: `text-slate-200`, truncate
- Size badge: text-only label, color via `getSizeBadgeClass` (no fill)
- CI glyph: `CIBadge` (glyph-only in compact, glyph+label in comfortable)
- Reviewers: avatar stack (`ReviewerAvatars`) in all densities — do not replace with text dots
- Comments count: `font-mono tabular-nums`, orange when pending
- Files/lines: `font-mono tabular-nums`
- Timestamp: `font-mono`, right-aligned

### PRGroup
- Header: `text-sm font-semibold uppercase tracking-wider text-slate-300` with `w-2 h-2` status dot + count in `font-mono tabular-nums text-slate-500`
- **No card framing** — just `border-b border-slate-800`
- Empty state: inline, `py-0.5 px-2`, `text-[11px]`, `w-3 h-3` icon, dimmed (`text-slate-600`). Not a centered block.

### Stats summary
- No background cards. Horizontal strip of `<glyph> <number>` pairs.
- Numbers `text-xl sm:text-2xl font-semibold tabular-nums text-slate-100`
- Label `text-[11px] uppercase tracking-wider text-slate-500 mt-1`
- Glyphs: `font-mono text-sm` with semantic color

### Badges (StatusBadge)
- Chip: `bg-slate-800/60 border border-slate-700/60 rounded px-2 py-0.5 text-xs`
- Glyph + label, semantic color on both

### Buttons
- Primary: `bg-slate-200 text-slate-900 hover:bg-white`
- Secondary: `border border-slate-800 text-slate-300 hover:bg-slate-800`
- Avoid saturated brand-color fills (`bg-blue-600` etc.) unless the action is genuinely destructive or critical.

---

## 9. Animation

Keep motion minimal. Allowed:
- `fadeSlideIn` on list entrance (staggered, first 20 items, 50ms increments)
- `hover:-translate-y-px` for the 1px lift
- `stale-pulse` on `AwaitingReview` group dot when a PR is older than 24h (cycle is 3s, opacity 1→0.65)
- `activity-flash` on SignalR-driven row updates (0.8s blue flash)

All animations respect `prefers-reduced-motion` via the global override in `src/styles/animations.css`.

**Do not** add: glow pulse on hover, colored shadow sweeps, parallax, bouncing entrances.

---

## 10. Helpers in `src/utils/prHelpers.ts`

Reuse these — don't reinvent:

| Function | Returns |
|---|---|
| `getStatusTintClass(status)` | faint row background class |
| `getStatusBorderClass(status)` | `hover:border-slate-600` (neutral) |
| `getStatusGlyph(status)` | `{ char, color }` |
| `getCIGlyph(checksStatus)` | `{ char, color }` |
| `getStatusShadowClass(status)` | `''` (deprecated, returns empty) |
| `getSizeBadgeClass(totalLines)` | text-only color class |
| `getPRSize(totalLines)` | `'XS' \| 'S' \| 'M' \| 'L' \| 'XL'` |
| `isStale(createdAt)` | boolean (>7 days) |
| `formatAge(createdAt)` | `'today'`, `'3 days'`, `'2 weeks'`, etc. |

---

## 11. Reference files

The dashboard is the canonical implementation. When restyling another page, mirror what these do:

- `src/components/PRRow.vue` — row layout, density modes, glyph usage
- `src/components/PRGroup.vue` — section header, empty state
- `src/components/StatsSummary.vue` — flat stats strip
- `src/components/StatusBadge.vue` — chip pattern
- `src/components/CIBadge.vue` — glyph-based indicator
- `src/components/PRSizeBadge.vue` — text-only colored label
- `src/views/Dashboard.vue` — density toggle, group container rhythm
- `src/utils/prHelpers.ts` — all helpers
- `src/style.css` — global `tnum` class, font-feature-settings
- `src/styles/animations.css` — animation tokens
- `tailwind.config.js` — extended slate palette
