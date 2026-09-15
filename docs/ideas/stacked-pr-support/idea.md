# Stacked PR Support — GitHub Native Stacks

## Problem

GitHub shipped **native stacked pull requests**: private preview 13 April 2026, public preview 30 July 2026, now available on every repository. A stack is a first-class GitHub object — an ordered chain of PRs in one repository, each targeting the branch of the PR below it, landing on a trunk. GitHub owns the stack map, the cascading rebase, the retargeting, and the merge semantics.

This changes the problem for GitBuddy in a way that goes beyond a missing feature. **GitBuddy is not merely stack-unaware; it is actively wrong about stacks.**

- **The merge button will fail.** `GitHubService.MergePullRequestAsync` calls `restClient.PullRequest.Merge(...)` — Octokit's legacy synchronous merge endpoint. GitHub documents that a stack *cannot* be merged with the legacy synchronous merge endpoints or mutations; stacked merges require the new asynchronous merge API. Every merge attempt on a stacked PR from GitBuddy's dashboard or PR detail hits this path.
- **Merge readiness understates the blast radius.** `RepositoryRuleService` computes `IsMergeReady` / `MergeBlockReason` from draft state, conflicts, unresolved threads, and approval counts alone. For a mid-stack PR, GitHub's semantics are that merging it *also merges every PR below it*. GitBuddy would render a calm "Ready to merge" badge on a button that lands four PRs.
- **The dashboard flattens the chain.** `PullRequest` has no stack fields. Stack members appear as unrelated rows, with independent priority scores (`PriorityService`) and independent analytics — a four-PR stack is counted as four unrelated units of review work.
- **Reviewers lose the ordering.** Nothing indicates that #412 sits on top of #409, so a reviewer can't tell whether the change they're reading has foundations that are still under review.

The data needed is cheap and already on GitBuddy's existing paths: GraphQL exposes read-only `stack` and `stackEntry` fields on `PullRequest`, REST attaches a `stack` object (stack number and size, the PR's position and base) to pull request resources, and — most usefully — webhooks add a `stack` property to the `pull_request` payloads that `GitHubWebhookProcessor` already handles.

### What this supersedes

An earlier version of this idea assumed GitBuddy would have to *infer* stacks by matching `SourceBranch` against `TargetBranch` and build its own chain model. Native stacks make most of that obsolete, and some of it wrong:

- **Detection is no longer a heuristic.** Stack membership is authoritative data on the PR. The ambiguity guard for two PRs sharing a head branch is unnecessary for native stacks.
- **The "exclude parent changes" diff toggle is unnecessary.** GitHub scopes each layer's diff to that layer already — "each one shows only the diff for its layer". The old concern about inflated `PRSizeBadge` numbers does not apply to native stacks.
- **Manual retargeting drudgery is gone.** Merging the bottom PR auto-rebases the rest; merging mid-stack retargets everything above to the stack's base and leaves it open.

What survives is the *surfacing* half — the dashboard and PR detail work below — plus one genuinely new and urgent piece: the merge path.

## Rough Approach

### Ingest Stack Membership

- Persist the stack fields on `PullRequest`: `StackNumber`, `StackSize`, `StackPosition`, `StackBaseBranch`, all nullable so non-stacked PRs are unaffected and no backfill is needed.
- Read them in three places that already exist: the GraphQL PR query in `GitHubGraphQLService`, the REST sync in `CacheService`, and the `pull_request` webhook path in `WebhookService` — the last one means stack changes arrive in real time with no polling cost.
- **Library constraint:** `Octokit 13.0.1`, `Octokit.GraphQL 0.4.0-beta`, and `Octokit.Webhooks 3.2.1` all predate the feature, so none of them expose `stack`. `GitHubService` already drops to a raw `HttpClient` with `Accept: application/vnd.github+json` for endpoints Octokit doesn't cover (repository rules, around lines 539 and 642) — that established pattern is the way in, without waiting on upstream bindings.

### Fix the Merge Path

This is the part that is a bug fix, not an enhancement.

- Route stacked PRs to the asynchronous merge API: submit the merge, receive a handle, poll for the result. Non-stacked PRs can keep the current synchronous call, so the change is additive.
- Model the semantics honestly in the UI. The merge confirmation must name exactly which PRs will land — "Merging #412 will also merge #409, #410, #411" — with their titles and statuses. A one-click action with a four-PR blast radius needs to say so before it runs, not after.
- The async flow has failure modes the current UI has no vocabulary for: only basic PR state is checked at submit, while branch protection and repository rules are evaluated later when the merge actually runs, surfacing as a failure during polling. Merges are atomic — the whole group merges (or enters the merge queue), or none of it does — so partial success is not a state to design for, but "submitted, running, failed on a rule three PRs down" is.
- Where the polling lives is a real design question: a background service that broadcasts the outcome over the existing SignalR hub fits GitBuddy's architecture better than a client-side poll that dies when the tab closes.

### Stack-Aware Merge Readiness

- Extend `RepositoryRuleService` so `MergeBlockReason` accounts for position: a PR whose lower layers aren't ready inherits their blockers ("blocked by #409 — 1 more approval needed").
- Merge requirements for every PR in a stack are determined by the bottom PR's base branch, and branch protection is enforced on every layer including mid-stack ones. GitBuddy's per-PR rule lookup currently resolves rules from each PR's own target branch, which is the wrong branch for every layer above the bottom.

### Dashboard & Detail Surfacing

- **`PRRow` stack badge** — position indicator (`2/4`) with a chain glyph, and stack members visually grouped under their bottom PR rather than scattered across status groups.
- **Stack map in PR detail** — mirror GitHub's merge-box map: every layer, its status, one-click navigation between them. This is a natural fit for the context strip in `pr-detail-navigation`, which is in flight now.
- **Priority and analytics** — a stack should be scored and counted as a unit, or at least not four times over. Worth deciding before `pr-analytics` numbers quietly drift.

### Manual Stacks as a Fallback

Teams that stacked branches by hand before this feature existed still have those PRs open, and Graphite-style workflows outside GitHub's model won't carry a `stack` object. Keeping a lightweight branch-chain heuristic as a *secondary* signal — clearly labelled as inferred, never used to drive merge behaviour — covers them without contaminating the authoritative path.

## Open Questions

- **Preview volatility.** GitHub labels this "public preview and subject to change". Persisting stack fields and rewriting the merge path against a moving API has real cost. Does this wait for GA, or ship read-only surfacing now and defer the merge work?
- **How urgent is the merge fix, really?** It only breaks for users who adopt native stacks. Worth checking whether anyone in the org has, before treating it as a bug rather than a feature. If nobody has, the honest sequencing is surfacing first, merge path when demand appears.
- **Does GitBuddy ever *create* stacks?** The REST Stacks API supports create, extend, and dissolve, and `create-pr-modal` already creates PRs. But GitBuddy is a review dashboard, and stack authoring is a local-git workflow that `gh stack` owns end to end. Read-only seems clearly right for v1 — worth stating explicitly rather than leaving open.
- **Cascading rebase.** GitHub supports triggering a server-side cascading rebase from the PR. Is that exposed as an API endpoint, or UI-only? If it's available, it is a high-value button; if not, GitBuddy should link out rather than reimplement it.
- **Merge queue interaction.** Stacks are merge-queue aware and queue support rolled out progressively after public preview. GitBuddy has no merge-queue concept at all — does a stacked PR entering a queue need its own display state?
- **Which identity merges?** The async merge runs in the background over several minutes. GitBuddy merges with the user's OAuth token; the flow needs to survive that token's session ending mid-merge.
- **Interaction with `cross-repo-dependencies`** — native stacks are same-repo only, and cross-fork stacks are unsupported. That idea remains the answer for the cross-repo case, and the two should stay distinct rather than one absorbing the other.

## References

- [About stacked pull requests](https://docs.github.com/en/pull-requests/get-started/about-stacked-prs) — model, merge semantics, rules and CI enforcement
- [Stacked pull requests APIs and webhooks](https://docs.github.com/en/pull-requests/reference/stacked-pull-requests-rest-and-graphql-apis) — REST `stack` object, Stacks API, GraphQL read-only fields, async merge requirement, webhook payload
- [Stacked pull requests are now in public preview](https://github.blog/changelog/2026-07-30-stacked-pull-requests-are-now-in-public-preview/) — rollout timeline
