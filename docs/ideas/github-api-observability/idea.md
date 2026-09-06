# GitHub API Quota & Sync Health

## Problem

Every piece of data in GitBuddy arrives through the GitHub API, and the app is blind to the health of that pipe:

- **No rate-limit awareness.** Nothing in `GitHubService`, `GitHubGraphQLService`, or `GitHubTokenService` reads the `X-RateLimit-*` headers or GraphQL `rateLimit` cost. REST is 5,000/hour per user token; GraphQL is a separate 5,000-point budget. `PRRefreshService` polls every 5 minutes by default and `BranchWithoutPRRefreshService` every 30, per org, forever.
- **Silent degradation.** When the limit is hit, calls fail, the refresh loop swallows it, and the dashboard quietly serves stale rows. Users see "nothing happened today", not "sync is broken".
- **No sync freshness signal.** `GitHubConfig.LastRefresh` is stored but not surfaced. `ConnectionStatus` reports the SignalR socket — which is happily connected while the data behind it is hours old.
- **Token failure modes are invisible.** A user's OAuth token in `User.AccessToken` can be revoked or expired; a GitHub App installation token can lose repo access. Both look like "this PR stopped updating".
- Boundary with `webhook-management`: that idea covers *inbound* delivery observability. This covers *outbound* API consumption and overall sync freshness — the fallback path webhooks degrade into.

## Rough Approach

### Instrumentation

- Wrap all GitHub calls so every response records: endpoint class, remaining quota, reset time, cost (GraphQL), duration, outcome. A thin decorator over `IGitHubService`/`IGitHubGraphQLService` keeps this out of the call sites.
- Persist a rolling window (`ApiCallMetric`, aggregated hourly, pruned aggressively) plus current-state counters in `CacheService`-adjacent memory for cheap reads.
- Track per identity: the App installation token and each user token consume separate budgets, and mixing them in one number hides the problem.

### Guardrails

- **Adaptive backoff** — when remaining quota drops below a threshold, lengthen the refresh interval instead of failing. Honour `Retry-After` and secondary-rate-limit responses rather than retrying blindly.
- **Prioritised spending** — an explicit user-triggered refresh should outrank a background sweep when budget is short.
- **Circuit breaker** — after repeated auth failures for one token, stop hammering it, mark the user's connection as broken, and prompt re-auth rather than retrying forever.

### Surfacing

- **Admin panel section** — quota remaining and reset per identity, calls/hour trend, last successful sync per repo, recent failures with status codes.
- **Staleness indicator** — when the last successful refresh exceeds ~2× the configured interval, `ConnectionStatus` shows "data may be stale" and the reason. Broken things should look broken.
- **Alerting** — reuse the Teams webhook for "sync has been failing for 15 minutes".

## Open Questions

- **Storage cost** — is a metrics table worth it, or is an in-memory ring buffer plus structured logs enough? Restarts losing history may be perfectly acceptable.
- **Per-user token attribution** — background refresh presumably uses the App/PAT identity while user actions use their own token. Is that split clean enough today to attribute usage correctly?
- **Backoff policy** — fixed thresholds, or scale the interval continuously with remaining quota?
- **Failure visibility to non-admins** — how much should a regular user see? "Data may be stale" is useful; a quota graph is not.
- **Scope creep toward general observability** — does this warrant OpenTelemetry / a `/health` endpoint with proper readiness checks, or stay a focused GitHub-quota feature?
- **Interaction with `webhook-management`** — one combined "Sync Health" admin surface, or two separate panels?
