# Multi-Org Aggregation

## Problem

`GitHubConfig` is a literal singleton row: one `Organization`, one credential set (`PersonalAccessToken` or a single `AppId`/`PrivateKey`/`InstallationId` when `UseGitHubApp` is true), one `TeamsWebhookUrl`. Every service assumes it: `RepositoriesController.GetOrganizationRepositoriesAsync` calls `GetOrganizationRepositoriesAsync(config.Organization, ...)`, `RepositoryRuleService.SyncRepositoryRulesAsync` reads `config.Organization` directly, and `PRRefreshService` polls against that one org. `PullRequest.Repository` is stored as a bare `"owner/repo"` string with no link to an org entity at all.

Anyone who works across more than one GitHub org — a consultancy reviewing client repos, a company mid-spinoff with two orgs, or a user who also maintains personal-org projects — gets no aggregated view. Today the only option is standing up a second GitBuddy deployment per org, which duplicates infrastructure and loses the whole point of a unified dashboard.

## Rough Approach

### Data model

Replace the singleton `GitHubConfig` with a `GitHubOrgConnection` table:

```
GitHubOrgConnection
├── Id                       (int, PK)
├── Organization             (string)
├── DisplayName / Color      (string — for the UI badge, see below)
├── AuthMode                 (enum: PAT, GitHubApp)
├── PersonalAccessToken      (string?)
├── AppId / PrivateKey / InstallationId (string?, when AuthMode = GitHubApp)
├── WebhookSecret            (string)
├── RefreshIntervalMinutes   (int)
├── Enabled                  (bool)
```

The existing single-row config becomes the first migrated `GitHubOrgConnection` — no forced re-auth for current deployments.

### Sync and routing

- `PRRefreshService` loops over all enabled connections instead of one config; each `PullRequest` gains an `OrgConnectionId` FK (rather than re-deriving org from the repository string) so isolation and permissions are explicit, not parsed.
- `GitHubWebhookProcessor` currently assumes one webhook secret; it needs to route an incoming payload's repository to the owning connection and validate against *that* connection's `WebhookSecret`.
- `RepositoryRuleService` and any per-repo sync worker iterate connections the same way `PRRefreshService` does.

### Access control

An org connection's PRs should only be visible to users entitled to that org — today `AllowlistService` appears to gate the whole app as one boundary. This needs to become per-connection membership, otherwise a contractor allow-listed for one client's repos would see every other connected org's PRs too, which is a real data-exposure risk, not just a UX nit.

### UI

- Admin/Settings screen changes from "one GitHub config form" to a list of connections — add, remove, enable/disable, and a "test connection" action reusing the existing validation calls.
- Dashboard groups gain an org badge/color chip once more than one connection is enabled, so PRs from different orgs are visually distinguishable in a merged view (extends the same facet model `dashboard-layout-customization` already uses for repo grouping).

## Open Questions

- **Real need: multi-org, or multi-installation?** GitHub Apps are installed per-org already; if `UseGitHubApp` is the common path, this might really be "support multiple installations of one App" rather than fully independent orgs with separate PATs — a narrower, more common ask worth scoping first.
- **Merged vs. switched view** — should PRs from all connections blend into one dashboard by default (needs the org-badge treatment above to stay legible), or should users explicitly switch org context like GitHub's own org switcher? Blending is more useful but riskier to get wrong visually.
- **Rate limits are per-token** — running N orgs concurrently multiplies GitHub API call volume against N separate limits; the `github-api-observability` idea's rate-limit tracking would need to become per-connection rather than global.
- **Schema churn vs. actual demand** — this touches `PullRequest`, webhook routing, and the allowlist model. Worth confirming someone actually needs two live orgs at once before taking on the migration, versus the simpler (if less elegant) two-deployment workaround.
