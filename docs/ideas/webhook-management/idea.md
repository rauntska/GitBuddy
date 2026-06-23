# Webhook Management & Observability

## Problem

The webhook processing pipeline (`GitHubWebhookProcessor` → `WebhookService`) is fully implemented and handles PR events, reviews, comments, and threads. But there's no visibility into whether webhooks are actually working. Teams have no way to:

1. **Know if webhooks are registered** — Are webhooks configured on all repos? Did someone accidentally delete one? The only way to check is to go to each repo's settings on GitHub.
2. **Detect delivery failures** — GitHub retries failed deliveries but eventually gives up. There's no monitoring for missed or failing webhooks.
3. **Manage webhooks from the dashboard** — Registering webhooks on new repos requires manual GitHub admin access. There's no self-service workflow.
4. **Debug webhook issues** — When a PR update doesn't appear in real-time, it's unclear whether the webhook fired, was received, or failed during processing.

The system silently falls back to polling via `PRRefreshService` when webhooks stop working, but nobody knows it's degraded.

## Rough Approach

### Webhook Registration Manager

A service that manages GitHub webhook lifecycle:

- **Auto-registration**: When a repository is added to GitBuddy, automatically register a webhook pointing to `/api/webhooks/github` with the correct events (`pull_request`, `pull_request_review`, `pull_request_review_comment`, `pull_request_review_thread`, `check_run`, `push`, `issue_comment`)
- **Health checks**: Periodic background job (e.g. hourly) that calls GitHub's `GET /repos/{owner}/{repo}/hooks` API to verify webhooks exist and are active on all tracked repos
- **Auto-repair**: If a webhook is missing or misconfigured, re-register it automatically (with logging)
- **Settings page**: Show webhook status per repository (registered/not-registered, last delivery timestamp, active/inactive)

### Webhook Delivery Log

Track every webhook delivery for debugging:

- **Log table**: Store incoming webhook events with `EventId`, `EventType`, `Repository`, `Action`, `DeliveryId` (GitHub's `X-GitHub-Delivery` header), `ReceivedAt`, `ProcessingStatus` (success/failed/processing), `Error` (if failed), `ProcessingDurationMs`
- **Dashboard UI**: Admin view showing recent webhook deliveries, filterable by repo/event type/status
- **Failure details**: Click into a failed delivery to see the error and full payload
- **Retention policy**: Keep logs for 7 days by default (configurable), auto-prune older entries

### Webhook Health Dashboard

A monitoring panel for admins:

- **Per-repo status**: Green/yellow/red indicator for each repo's webhook health
  - Green: webhook registered, last delivery within expected window
  - Yellow: webhook registered but no recent deliveries (may indicate events aren't firing)
  - Red: webhook missing or last delivery failed
- **Aggregate stats**: Total deliveries, success rate, average processing time, p99 latency
- **Alerting**: SignalR notification to admins when webhook health degrades (e.g. "Webhook for repo X hasn't delivered in 1 hour")
- **Fallback indicator**: Show when the system is relying on polling fallback vs. real-time webhooks

### Data Model Sketch

```
WebhookDeliveryLog
├── Id                    (int, PK)
├── GitHubDeliveryId      (string, X-GitHub-Delivery header)
├── EventType             (string, e.g. "pull_request")
├── Action                (string, e.g. "opened")
├── RepositoryFullName    (string)
├── Payload               (string, JSON - optional, truncate large ones)
├── ProcessingStatus      (enum: Success, Failed, Processing)
├── Error                 (string?, nullable)
├── ProcessingDurationMs  (int?)
├── ReceivedAt            (DateTime)

WebhookRegistration
├── Id                    (int, PK)
├── RepositoryFullName    (string)
├── GitHubHookId          (long?, the hook ID returned by GitHub)
├── IsActive              (bool)
├── LastVerifiedAt        (DateTime)
├── LastDeliveryAt        (DateTime?)
├── Status                (enum: Active, Missing, Misconfigured, Unknown)
├── RegisteredAt          (DateTime)
```

### Integration Points

- Extend `GitHubWebhookProcessor` to log deliveries before and after processing
- Add a new `WebhookManagementService` for registration and health checks
- Add a `WebhookHealthWorker` background service for periodic checks
- New admin API endpoints: `GET /api/webhooks/status`, `POST /api/webhooks/register/{repo}`, `GET /api/webhooks/deliveries`
- Admin-only frontend panel in Settings

## Open Questions

- **Payload storage**: Should full webhook payloads be stored? They can be large. Options: (a) store everything for 24h, (b) store only metadata + truncated payload, (c) store only failed payloads in full. Option (b) seems like the right default.
- **Permissions**: Should webhook management be admin-only, or can any user see the status? Registration likely needs admin (requires repo admin on GitHub), but viewing status could be broader.
- **Multi-repo registration**: When an org has 50+ repos, should there be a "register all" bulk operation? GitHub App webhooks might be a better approach for large-scale (single webhook for the entire org/installation).
- **GitHub App vs. individual webhooks**: The current architecture uses per-repo webhooks. Should this idea also cover migrating to GitHub App-level webhooks (which cover all repos in an installation)?
- **Rate limiting**: GitHub's webhook registration API is rate-limited. How to handle registering webhooks for many repos without hitting limits?
- **Secret rotation**: Should the webhook secret be rotatable without breaking existing registrations?
