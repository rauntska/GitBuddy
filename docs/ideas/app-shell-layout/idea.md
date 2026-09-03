# App Shell & Scroll Model

## Problem

The app's chrome — sidebar, headers, sticky panels — is held together by hard-coded pixel offsets that every component has to know about independently.

In `PRDetail.vue` alone:

- the header is `sticky top-20`,
- the file tree toggle rail is `sticky top-[8.5rem]`,
- the file tree is `sticky top-[6.75rem] h-[calc(100vh-6.75rem)]`.

Three magic numbers, all encoding "however tall the app header plus the PR header happens to be right now". Nothing enforces the relationship, so:

- **Any chrome change breaks alignment silently.** Adding a row to the header — which `linked-issues`, `pr-detail-navigation`, and `review-queue-sessions` all want to do — leaves sticky panels overlapping or floating, with no compile-time or test-time signal.
- **Banners push everything down.** `Dashboard.vue` stacks the error banner and the PAT warning above the content, shifting the whole page as they appear and dismiss.
- **The page scrolls, not the panels.** The document is the scroll container, so the file tree's `h-[calc(100vh-...)]` is an approximation of a viewport it does not actually own. Independent panel scrolling — read the diff while the tree stays put — works only by coincidence of these offsets.
- **`min-h-screen` is repeated per view** rather than owned by a shell, so each view re-derives its own full-height behaviour.

This is invisible to users right now and expensive for everything built on top of it.

## Rough Approach

### Declare the Chrome

- Publish chrome heights as CSS custom properties on the shell (`--app-header-h`, `--view-header-h`, `--banner-h`) and have sticky elements offset from them: `top: calc(var(--app-header-h) + var(--view-header-h))`. One source of truth, and headers can change height without a hunt through templates.
- Where a height is genuinely dynamic, measure it once in the shell with `ResizeObserver` and write it back to the property.
- This is the same token-layer argument as `theming-and-accessibility`, applied to space instead of colour — the two should probably land as one layout/token pass.

### Own the Scroll

- Give the shell a fixed viewport-height grid: header row, then a content row that scrolls. Views become scroll containers instead of contributing to one long document scroll.
- Panels (file tree, diff, comments) each get their own overflow region, which makes the existing resize handles behave predictably and gives `large-diff-performance` a real container to virtualize against.

### Non-Displacing Notifications

- Move transient banners (error, PAT warning) into a dedicated shell region that overlays rather than reflows, or collapse them into a single dismissible status chip that expands on click. Content should not jump because a warning appeared.

## Open Questions

- **Scope creep risk.** This touches every view and improves nothing the user can name. Is it worth doing on its own, or only as groundwork bundled into `pr-detail-navigation`?
- **Fixed-viewport shell vs. document scroll** — a fixed shell is cleaner but changes browser find-in-page and anchor-scroll behaviour, and mobile browsers handle `100vh` badly (`100dvh` helps, partly). Does `mobile-review` want the same model?
- **How dynamic is the chrome really?** If header heights are effectively fixed per breakpoint, static CSS variables beat `ResizeObserver` — measure before adding machinery.
- **Sidebar behaviour** — should `AppSidebar` collapse to icons at narrow widths as part of this, or is that a separate concern?
- **Verification** — layout regressions are exactly what unit tests miss. Does this need visual snapshots, and is that a dependency worth taking on?
