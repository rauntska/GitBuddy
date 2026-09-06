# Mobile Review Mode & PWA

## Problem

PRs don't wait for your desk. The current frontend is a desktop-first layout with no responsive story:

- `MainLayout`/`AppSidebar` and the multi-column dashboard assume a wide screen; on a phone the dashboard is horizontal-scroll misery.
- `PRDetail`'s file tree + side-by-side-ish diff + comment panels don't reflow; reviewing from a phone means pinch-zooming a desktop page.
- Yet the highest-value mobile moments are exactly the small ones: approve a small PR while waiting for coffee, leave a one-line comment, check what blew up after a Teams nudge. The app already has all the APIs for these actions — it's purely a layout/UX gap.
- Browser notifications exist (`useBrowserNotifications`), but there's no installable app surface; tapping a notification opens a browser tab into the desktop layout.

## Rough Approach

### Responsive Foundation

- Breakpoint pass over `MainLayout` (sidebar → bottom nav or hamburger drawer), `Dashboard` (groups stack vertically; `PRColumnHeaders` and expanded density mode hidden on narrow screens — compact rows already exist via view density modes), `PRDetail` (tabs: Overview / Files / Activity instead of side panels; file tree becomes a sheet/drawer over the diff).

### Diff Viewer on Small Screens

- Unified diff mode (hunks stacked, no horizontal scroll for reasonable line lengths), long-line handling (wrap toggle vs. horizontal swipe), tap-to-expand context already exists conceptually.
- Comment flow must work: tap a line → existing inline comment form, sized for touch; `MentionAutocomplete` and `CommentTemplates` dropdowns repositioned for mobile keyboards.

### Review Quick Actions

- A mobile-tuned action bar on `PRDetail`: Approve / Request changes / Comment (pending-review flow reused as-is), Copy link, Open in GitHub — the 90% actions, thumb-reachable.
- Explicitly *not* trying to make merge-from-phone first-class: merging deserves deliberate context; show merge status and defer the button to desktop (or behind a confirmation, at most).

### PWA Layer

- Web manifest + minimal service worker (app shell caching only — offline data is a lie for a live dashboard, don't pretend).
- Installable → standalone window, proper icon/splash, OS-level app feel. Ties into notification taps deep-linking to the PR (`JoinPRRoom` + route already exist).

## Open Questions

- **Scope discipline** — is "triage and small reviews from phone" the explicit non-goal boundary? Writing a serious 500-line diff review on a phone is misery regardless of layout; optimize for triage, approve-small, comment-short.
- **Testing surface** — no E2E/testing framework exists in the repo at all; responsive regressions will be manual unless Playwright lands separately. Does that gate this?
- **Service worker risk** — even shell caching brings stale-asset footguns; is `display: standalone` + no SW (pure installability) the safer v1?
- **Push notifications** — real closed-app push needs VAPID web push + a server sender (SignalR dies with the tab). Worth it, or is the existing open-app notification flow enough for v1?
- **Tablet** — the awkward middle; land phone breakpoints first and let tablets inherit?
