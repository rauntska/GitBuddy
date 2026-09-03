# Linked Issues & Work Items

## Problem

GitBuddy models pull requests in isolation. The *why* behind a PR — the issue, ticket, or work item it closes — exists only as prose in the description:

- `PullRequest` has `Title`, `Body`, branches, and reviews; nothing links to an issue. `Fixes #123` in a body is rendered as text by `DescriptionRenderer` and understood by nobody.
- Reviewers open a PR with no cheap way to see the requirement it satisfies. "Is this the right change?" needs the ticket, which lives in another tab.
- Multi-PR work (frontend + API for one ticket) is invisible — the dashboard shows two unrelated rows. `cross-repo-dependencies` models the *ordering* constraint between such PRs; this models the *shared purpose*.
- No "what is still open for this issue?" view, and no way to filter or group the dashboard by work item.
- GitHub Issues are not touched anywhere: `GitHubService`/`GitHubGraphQLService` fetch PRs, reviews, threads, checks, and files only.

## Rough Approach

### Extraction

- Parse issue references out of `PullRequest.Body`, title, and branch name at sync time in `CacheService`: GitHub forms (`#123`, `owner/repo#123`, closing keywords) and a configurable external pattern (e.g. `[A-Z]+-\d+` for Jira/Linear), with the URL template stored on `GitHubConfig` alongside the existing org settings.
- Prefer GitHub's own `closingIssuesReferences` from GraphQL where available — it is authoritative for the GitHub case and free with the PR detail query — and fall back to regex for external trackers.
- Store as a `PullRequestIssueLink` join (`PullRequestId`, `Provider`, `Key`, `Url`, `Title`, `State`, `LinkSource` = detected/manual). Re-derive on every sync; manual links survive.

### Surfacing

- **`PRRow`**: a compact issue chip next to the title, muted per the dense/pro style — key only, tooltip for the title.
- **`PRDetail`**: an issue strip above the diff, with title, state, and a link out. For GitHub issues the body can be fetched and rendered inline (collapsed by default) so reviewers can check the requirement without leaving.
- **Sibling PRs**: on a PR linked to issue X, list the other open/merged PRs sharing X. This is the cheapest possible answer to "is this the whole change?".
- **Dashboard grouping**: an optional "group by issue" mode, and issue key as a filter dimension for `saved-filters` and a search token for `command-palette`.

### Optional Enforcement

- A per-repo rule ("PRs must link an issue") on `RepositoryRule`, surfaced as a soft warning badge, not a merge block. Bot PRs are exempt (see `bot-pr-automation`).

## Open Questions

- **Which trackers for v1** — GitHub Issues only (no new auth, data is one query away), or a generic "external key + URL template" that covers Jira/Linear/Azure DevOps without integrating with them?
- **Fetching external issue state** — showing a Jira ticket's title/status needs Jira credentials and a whole auth story. Is a link-only chip enough for v1?
- **Where does parsing live** — in `CacheService` during sync, or a dedicated `IssueLinkService` invoked from both sync and webhook paths?
- **Backfill** — parse historical PRs on migration, or only link going forward?
- **Noise** — bodies frequently contain `#123` references that are not links (issue mentions, code, comment numbers). How aggressive should closing-keyword-only matching be?
- **Overlap with `cross-repo-dependencies`** — should a shared issue imply a soft dependency edge, or stay purely informational?
