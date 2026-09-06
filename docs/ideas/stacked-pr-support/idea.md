# Stacked PR Support

## Problem

Teams practicing small, incremental changes (Graphite-style stacking) split one feature into a chain of PRs where each PR's base branch is the previous PR's head branch. GitBuddy treats every PR as an independent flat row:

- **No stack awareness on the dashboard** — a stacked PR's diff includes everything from its parent PRs, inflating additions/deletions and `PRSizeBadge`. Reviewers can't tell that PR #412 depends on #409.
- **Merge order is invisible** — merging a child before its parent either fails or silently retargets it, depending on GitHub settings. Nothing warns that a "ReadyToMerge" PR isn't actually at the bottom of its stack.
- **Manual retargeting drudgery** — after merging the bottom PR, authors must manually rebase/re-target each child. GitHub nudges about this, GitBuddy doesn't.
- The branches-without-PRs feature detects *un-PR'd* branches, but a stack where only the tip has a PR looks identical to a normal PR from the dashboard's perspective.

We already track `PullRequest.SourceBranch` and `TargetBranch`, which is exactly the data needed to detect stacks.

## Rough Approach

### Stack Detection

- Two open PRs in the same repo form a parent/child edge when `child.SourceBranch == parent.TargetBranch` — wait, invert: when `child.TargetBranch == parent.SourceBranch` (child is based on parent's head branch).
- Build chains in memory from already-synced data (a `StackService` computing chains per repo on dashboard load is cheap — no GitHub API calls needed). Optionally persist `StackRootId` / `StackDepth` on `PullRequest` later if computed-on-the-fly proves too slow.
- Ambiguity guard: if two open PRs share the same head branch, don't guess — mark the stack ambiguous and fall back to flat display.

### Dashboard Surface

- **Stack badge on `PRRow`** — position indicator like "3 of 5" plus a small chain icon.
- **Visual grouping** — stacked PRs rendered indented/bracketed under their stack root, or a hover popover listing the full chain with links.
- **Merge-readiness correction** — `PullRequestStatusService` should not report "ReadyToMerge" for a PR whose parent is still open; surface `MergeBlockReason = "Parent PR not merged"`.

### PR Detail

- **Stack strip** — horizontal chain widget under the header: each node = PR number + status, clickable, current PR highlighted.
- **"Exclude parent changes" diff toggle** — diff the PR against its parent PR's head instead of the target branch, so reviewers only see this PR's own changes. File-level additions/deletions in the header recalculate accordingly.

### Merge Flow

- **Bottom-up enforcement** — merge button disabled with explanation when the PR has an unmerged parent.
- **"Merge stack"** — one action that cascades: merge bottom PR, wait for GitHub to retarget children (or retarget explicitly via the edit-PR-base API), then offer the next one. Start conservative: merge one, then prompt to continue.

### Create PR

- `create-pr-modal` pre-fills the base branch when creating a follow-up PR from a stacked branch, and offers "base: parent PR #409's branch" as a suggestion alongside default branch.

## Open Questions

- **Enforce or suggest?** Should GitBuddy block out-of-order merges it computes itself, or only warn? GitHub is the source of truth for actual mergeability — warning feels safer.
- **Retarget mechanics** — when the parent merges, GitHub auto-retargets children by default. Do we need to handle the non-retarget case at all, or just refresh and display?
- **Cross-repo stacks** — out of scope? Almost certainly yes for v1.
- **Draft stacks** — Graphite users often keep the whole stack in draft until the tip is ready. Does the Draft group need special stack handling?
- **Display density** — indented stacks vs. badges-only: badges are cheaper to build, indentation is clearer. Probably badges first.
