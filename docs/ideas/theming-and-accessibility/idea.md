# Theme Tokens & Accessibility

## Problem

The frontend has one hard-coded appearance and no accessibility story.

**Theming.** `docs/reference/visual-style.md` defines a deliberate dark "Dense / Pro" language, but it is expressed as literal Tailwind classes scattered through every component — `bg-slate-900`, `border-slate-800`, `text-slate-200`, status tints like `bg-blue-950/20`. There is no token layer, so:

- There is no light theme, and no way to add one without touching every component. Reviewing on a bright screen or a projector is genuinely uncomfortable.
- Status colors are duplicated across `PRRow`, `PRGroup`, `StatusBadge`, `CIBadge`, `prHelpers.ts`, and the analytics charts. Changing one means finding all of them.
- `prefers-color-scheme` is ignored; the app is dark regardless of system setting.

**Accessibility.** Spot-checking shows `aria-` attributes in roughly ten components and absent from the rest. Concretely:

- `ContextMenu` (a Teleport-based menu), `SearchableDropdown`, `MentionAutocomplete`, and the modals lack the roles, focus trapping, and arrow-key handling their patterns require.
- The style guide's "all text is `text-slate-200`, use opacity for hierarchy" rule means de-emphasised text is dimmed by alpha — which reduces contrast without any check that it stays above WCAG AA.
- Status is often conveyed by a colored dot alone, which is exactly the color-blind failure case.
- Keyboard navigation of the dashboard barely exists; `ux-polish` adds motion, and `prefers-reduced-motion` is already respected in `styles/animations.css` — a good precedent this idea would extend to the rest.

## Rough Approach

### Token Layer

- Define semantic CSS custom properties (`--surface`, `--surface-raised`, `--border`, `--text`, `--text-muted`, `--status-awaiting`, …) and map Tailwind's theme onto them, so existing class names keep working while the values become swappable.
- Migrate incrementally, surface by surface, rather than in one sweeping rename. `prHelpers.ts` helpers like `getStatusTintClass` are the natural first target — they already centralise the mapping.
- Themes become a values file: `dark` (today's exact palette, pixel-identical) and `light`. Selection via `UserPreferences` — `Theme: "system" | "dark" | "light"` — alongside the existing density and layout preferences.

### Accessibility Pass

- Contrast-audit the resulting token pairs against WCAG AA and adjust the light theme to pass; check the dark theme's opacity-dimmed text too, and record the results in the style guide.
- Pattern fixes where they matter most: focus trap and `Esc` in modals, roving focus in `ContextMenu` and `SearchableDropdown`, `aria-live` on `ToastContainer`, labelled icon-only buttons, visible focus rings that survive the dense aesthetic.
- Redundant encoding for status: keep the dot, add a glyph or letter so color is never the only channel.
- Extend the existing reduced-motion block to cover the animations `ux-polish` introduces.

## Open Questions

- **Is a light theme actually wanted?** The style guide is opinionated and dark by design. The token layer is worth doing regardless; the light theme is the part that needs a decision.
- **Migration blast radius** — a token layer touches nearly every component. Big-bang or per-surface over several PRs? Without the test suite from `automated-test-foundation`, verification is manual either way.
- **Density interaction** — do density modes need their own spacing tokens, or is that over-abstraction?
- **Charts** — `chart.js` colors are configured in JS, not classes. Do they read tokens at runtime, or keep a parallel palette?
- **How far on a11y** — full WCAG AA as a stated goal, or a pragmatic pass over keyboard and contrast? A stated target implies ongoing enforcement.
- **Sequencing with `ux-polish` and `mobile-review`** — both restyle broadly. Does the token layer land first so they build on it, or does that stall them?
