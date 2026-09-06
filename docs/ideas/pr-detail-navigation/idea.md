# PR Detail Navigation — Sections, Not One Long Scroll

## Problem

`PRDetail.vue` is a single 1,535-line component that renders everything a PR has, stacked vertically, all at once. Reading the template top to bottom: header → branch info → description → general comments → a right column of priority, `ReviewerManager`, `ReviewTimeline`, and CI checks → then, below all of it, the file diffs.

The consequences are structural, not cosmetic:

- **The diff is below the fold, always.** The thing a reviewer came to do is under a description, a comment thread, and a sidebar. On a PR with a long description it is a lot of scrolling before any code appears.
- **No way to jump.** There are no tabs, no section nav, no anchors — `grep` for `tab` in the file returns nothing. Getting from a diff hunk back to the review timeline means scrolling past everything in between.
- **No deep links.** You cannot send a colleague a link to the checks or to a specific file; the URL identifies only the PR.
- **Context is lost while reading code.** Merge readiness, approval count, and unresolved-thread count live in the sticky header, but the reviewer/CI detail scrolls away exactly when a reviewer wants it.
- **The monolith resists change.** Nearly every idea in `docs/ideas/` adds something to this view (`ci-log-triage` to checks, `linked-issues` to the header, `review-delta-view` to the diff toolbar). Each one edits the same enormous file.

## Rough Approach

### Segmented Sections

- Introduce top-level sections — **Conversation / Files / Checks** — as a segmented control in the existing sticky header, following the same pattern `review-lifecycle-timeline` already established for its Events/Lifecycle toggle.
- Default to **Files** when the current user is a requested reviewer and there is a diff to read; default to Conversation otherwise. The common case should need zero clicks.
- Keep section state in the route (`/pr/:id/files`) so links are shareable, back/forward work, and a file path can extend it (`/pr/:id/files?path=src/Program.cs`).

### Persistent Context Strip

- Whatever section is active, the sticky header keeps the decision-relevant facts: status, approvals `2/3`, unresolved threads, CI summary. These already exist in the header; the change is making them the *stable* layer rather than one of several scrolling copies.
- Clicking any of them jumps to the owning section.

### Decomposition

- Split `PRDetail.vue` into a shell plus one component per section (`pr-conversation.vue`, `pr-files.vue`, `pr-checks.vue`), matching the existing kebab-case component convention (`comment-thread.vue`, `file-diff-header.vue`).
- Sections mount lazily — a reviewer who never opens Checks never pays for rendering them, which also helps the payload concerns in `large-diff-performance`.

## Open Questions

- **Tabs vs. a single scroll with a jump nav.** Tabs hide content, and reviewers legitimately want the description visible *while* reading the diff. Is a two-pane layout (context rail + diff) better than tabs? Worth mocking both before committing.
- **What happens to the right-hand info column** — does it become the Conversation section's content, or a persistent rail across all sections?
- **Comments panel interaction** — the panel is independently toggleable and resizable today. Does it stay global, or become part of the Files section?
- **Unread/attention markers per section** — should the Checks tab show a red dot when CI fails, and Files a count of files changed since your last review (`review-delta-view`)?
- **Migration cost** — decomposing a 1,535-line component with no tests is the riskiest kind of refactor. Does this wait on `automated-test-foundation`, or ship behind a preference toggle so the old layout remains available?
- **Mobile** — `mobile-review` needs a section model anyway, since nothing else fits on a phone. Should these two be designed together rather than sequentially?
