# Merge Collision Detection Across Open PRs

## Problem

Two people open PRs touching the same files in the same repo. GitHub shows each PR in isolation — nothing warns either author that a sibling PR modifies the same code until one merges and the other turns into a conflict rebase marathon.

GitBuddy is uniquely positioned here: it already syncs `FileDiff` rows (path, patch, additions/deletions) for **every open PR in the org**. GitHub's per-repo UI can't easily do this; GitBuddy has the whole picture in one database.

Concretely, nothing today tells you:

- "3 other open PRs also modify `Program.cs`"
- "This PR and #412 rewrite the same function" (hunk-level overlap — near-certain conflict)
- "Merge this one first to minimize pain" (order matters once files overlap)

## Rough Approach

### Overlap Computation

- **File level** — for each repo, index open PRs by `FileDiff.Path`; any path appearing in ≥2 open PRs is a collision set. Trivial query, computable in the PR refresh cycle or on demand.
- **Hunk level (precision pass)** — for colliding files, parse the stored `Patch` into hunk ranges per PR; overlapping line ranges = true textual conflict risk; same file but disjoint ranges = likely clean auto-merge (still worth a soft note). Hunk parsing already exists in the diff viewer (`diffHelpers`).

### Surfacing

- **On `PRRow` / detail header** — a collision badge: "⚠ overlaps 2 open PRs" with a popover listing them (title, author, status, how far along each is).
- **On `FileDiffViewer` header** — per-file indicator when that specific file collides, since that's where the anxiety actually lives.
- **Proactive angle** — if a colliding PR is "ReadyToMerge" or already approved while yours is early, the popover can say "finish and merge #412 first, then rebase" — ordering advice is the actionable part.
- **Optional SignalR nudge** — when a PR with collisions gets merged, notify authors of the other colliding PRs ("#412 just merged — expect conflicts in `Program.cs`") through the existing broadcast + notification pipeline.

### Cost Model

- Compute server-side in the refresh cycle, store as a lightweight per-PR collision summary (or compute on read — with org-scale open-PR counts both are cheap; start on read, cache if needed).

## Open Questions

- **Renames & moves** — `OldPath` vs `Path` matching: a rename vs. an edit of the old path is a conflict GitBuddy's index should catch but naive path matching misses. Match on both paths?
- **Noise floor** — lockfiles (`pnpm-lock.yaml`, generated files) collide constantly and nobody cares; needs an ignore list (per-repo configurable?) or the badge becomes wallpaper.
- **Risk grading** — hunk overlap vs. file overlap vs. delete/modify: how granular before it's over-engineering? Ship file-level first?
- **Same-author collisions** — stacking workflows intentionally touch the same files (see `stacked-pr-support`); exclude stacks once detected.
- **Closed/merged horizon** — only *open* PRs matter; merged ones are already reflected in mergeable state from GitHub.
