# Audit Log — Action History

## Problem

GitBuddy performs *write* actions on GitHub (merge, publish, review submission, comment edits/deletes, priority overrides, nudges that post comments and Teams messages) and manages access (role changes, invitations, allowlist edits, settings changes). None of it is recorded:

- "Who merged this?" — GitBuddy-initiated merges are indistinguishable after the fact unless the merge commit message says so; nudges, priority changes, reviewer add/removes leave no trail at all.
- Admin actions (someone's role changed, an allowlist entry appeared, settings rotated) have no history — a real problem for a multi-user admin surface.
- Debugging "why did the dashboard change" (priority jumped, PR vanished via delete-old-PRs cleanup) is archaeology through logs on the server, if they were kept.

Note the boundary: the `webhook-management` idea logs *inbound* webhook deliveries; this logs *outbound and local* actions taken through GitBuddy.

## Rough Approach

### Data Model

```
AuditEntry
├── Id            (int, PK)
├── UserId        (FK → User, nullable for system actions)
├── ActorType     (User | System | BackgroundService)
├── Action        (string, e.g. "pr.merged", "pr.priority-override", "admin.role-changed")
├── TargetType    (PullRequest | Repository | User | Settings | …)
├── TargetId      (string — PR id, username, "settings/github")
├── Details       (JSON: merge method, old→new values, nudge recipient)
├── CreatedAt     (DateTime UTC)
```

### What Gets Logged

- **GitHub-write actions**: merge, publish draft, submitted reviews (approve/changes/comment), comment/reply create-edit-delete, thread resolve/unresolve, reviewer add/remove, nudge (with rate-limit context), PR create, priority set/clear, suggestion apply if `code-suggestions` lands.
- **Local/admin actions**: role changes, invitations created/revoked, allowlist edits, settings changes (redact secrets — key names and which changed, never values), user deletion.
- **System actions**: auto-cleanup (delete-old-PRs), stale automation if `stale-pr-management` lands, auto-merges if `bot-pr-automation` lands — these need the trail most, being no-human actions.

### Implementation Shape

- A thin `IAuditService` invoked at the end of the relevant MediatR handlers — the flat-controllers/MediatR architecture means every write action funnels through one place per action, so coverage is systematic, not scattered.
- Write-and-forget (its failure should never fail the user action); batch inserts are fine.

### UI

- Admin settings tab: filterable table (by user, action type, target, date range) with Details expansion.
- PR-scoped slice: a compact "Activity" section on `PRDetail` showing GitBuddy-side actions for that PR (merges, nudges, priority changes) alongside the existing `ReviewTimeline`.

### Retention

- Default 90 days, configurable, pruned by a lightweight pass in an existing background worker. No PII beyond what's already in the DB.

## Open Questions

- **Granularity** — log read actions (who *viewed* what)? Default no (noise + privacy); only state-changing actions.
- **Immutability** — DB-only is fine for v1; does anyone need export (CSV/JSON) for compliance before that's asked for?
- **Correlation** — should entries carry the SignalR event / webhook delivery id that triggered them, linking outbound actions to inbound causes (would pair naturally with `webhook-management`'s delivery log)?
- **Old→new details in JSON** — how much is too much? Diff bodies no; metadata (method, counts, usernames) yes.
