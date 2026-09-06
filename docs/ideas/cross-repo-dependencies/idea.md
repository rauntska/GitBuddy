# Cross-Repo PR Dependencies

## Problem

Features routinely span repos: the API PR needs to merge before the frontend PR that consumes the new endpoint. GitHub has no cross-repo dependency model — every team improvises with PR description links and tribal knowledge.

GitBuddy sees the whole org's PRs in one database — it can hold a dependency graph GitHub's per-repo UI structurally cannot. Today:

- Nothing prevents merging the frontend PR before its API dependency (it'll "work" and break at runtime).
- Reviewers of the dependent PR can't tell whether the API it assumes is merged, in review, or doesn't exist yet.
- The dependency link lives in prose in a description, so tooling (ordering, alerts, dashboards) can't use it.

**Boundary with `stacked-pr-support`**: stacks are *same-repo, branch-chained, auto-detected* chains. This idea is *explicit, user-declared, cross-repo (or same-repo non-adjacent)* dependency edges. Two mechanisms, one mental model — worth designing the data model so both can render through the same UI.

## Rough Approach

### Declaration

- "Blocked by…" action on `PRDetail` (and the quick-actions menu): search across all open PRs in the org (the dashboard's own data — GitHub's picker can't offer this) and link one or more dependencies.
- Parsing assist: scan PR description for `Depends on #123` / repo-qualified `org/repo#456` patterns and offer to convert links into structured dependencies.

### Enforcement-lite

- Dependency status surfaces everywhere the PR does:
  - `PRRow`: small chain indicator ("blocked: 1 of 2 deps merged").
  - `PRDetail`: dependency strip — each dep as a chip with live status (open / approved / merged / *closed unmerged*, the nasty case).
  - Merge button **warns** (not blocks — v1) when unmerged dependencies exist; escalation to hard block is a per-org setting.
- Cycles rejected at link time (the graph is per-repo-namespace simple DAG validation, cheap).

### Reactive Behavior

- On a dependency merging (or closing), SignalR notify dependents' authors: "your dependency `api#456` merged — `web#789` is unblocked" — the moment people actually want a nudge, unlike generic staleness pings.
- Optional: auto-retarget none, auto-rebase none — explicitly out of scope; this is visibility and ordering, not automation.

### Dashboard Ordering

- With a dependency graph available, the "ReadyToMerge" group can sort topologically *within* the graph (deps before dependents), and an "unblock first" mini-list ("merging these 3 PRs unblocks 7 others") is one query away — high-leverage merge-ordering advice.

## Open Questions

- **Soft warn vs hard block** — orgs differ; probably warn-by-default, block opt-in per repo?
- **Transitive display** — A→B→C across repos: show full chain on each PR, or direct deps only with drill-down?
- **Deps on merged/closed PRs** — auto-resolve (dep satisfied / dep dead — the dead case deserves a loud warning on the dependent).
- **External deps** — "blocked by JIRA ticket / deploy of service X"? Slippery scope; probably no for v1, keep it PRs-only.
- **Same-model rendering with stacks** — if both land, a shared `DependencyEdge`-style abstraction avoids two parallel graph codepaths; design this one second, informed by whatever `stacked-pr-support` ships.
