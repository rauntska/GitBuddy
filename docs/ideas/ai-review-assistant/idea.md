# AI Review Assistant

## Problem

Review bandwidth is the constraint this whole product orbits, and large PRs are where it's lost: opening a 40-file diff is intimidating, so it sits in "AwaitingReview" (the stale-pr idea addresses the *nudging* side; this addresses the *bandwidth* side). Two specific gaps:

- **No orientation layer.** A reviewer opening a big PR in `PRDetail` sees files and diffs, but has to reconstruct the what/why/risk themselves. The description is whatever the author bothered to write.
- **Repetitive first-pass comments.** Typos, naming, obvious null-handling, missing test coverage — human reviewers spend attention on these before the interesting design questions.

The analytics data GitBuddy already keeps (PR size, review latency, reviewer history) is exactly the context an assistant could leverage, and nothing AI-shaped exists in the codebase yet.

## Rough Approach

### Configuration

- Admin-level settings following the `GitHubConfig` pattern: provider (OpenAI / Anthropic / Azure OpenAI / self-hosted endpoint), API key, model, per-user daily cap, hard cost ceiling. Store in a new `AIConfig` or extend `SettingsController`'s payload.
- Fully opt-in per user (a toggle in user settings); nothing AI-related renders for users who haven't opted in.

### PR Summary Card (the anchor feature)

- "Summarize" button on `PRDetail` — generates what/why/areas-of-risk/test-coverage-notes from the PR description + file diffs (patch text only; cap by size and refuse oversize PRs).
- Cached server-side keyed by PR + head SHA so repeat views and multiple users don't re-pay; invalidated on push.
- Rendered as a visually distinct card ("AI-generated") — never impersonating author or reviewer content.

### Reviewer Assist (phase 2, more sensitive)

- Per-file "review this file" in the diff viewer: assistant comments on suspicious hunks, shown in a clearly-separate gutter layer (never mixed into the comment threads, never posted to GitHub without explicit human action).
- Human submits the actual review as today; assistant output is copy-into-comment at most.

### Dashboard Triage Signals (phase 3)

- Risk flag on `PRRow` for outliers (huge diff + low test-file ratio + risky file types like migrations/auth), computed in the background refresh and fed into `PriorityService` as one input alongside the existing signals.

### Guardrails

- Never auto-post to GitHub. No code leaves the configured provider. Per-user rate limits and a global monthly budget with a kill switch. Logs record prompt size and cost, never content.

## Open Questions

- **Provider abstraction** — one provider or an interface from day one? A minimal `IAssistantService` interface seems cheap insurance.
- **Privacy posture** — org code going to an external API is a policy decision, not a technical one. Self-hosted/Ollama option may be mandatory for some orgs; does that gate the whole feature?
- **What context is worth sending** — description + patches only, or also review history ("the author previously rejected renaming this method")? More context = better answers, more exposure.
- **Cost model** — per-user caps vs. global budget vs. both; what happens at the limit (degrade to summary-only? hard off?).
- **Trust calibration** — if summaries are frequently wrong, the feature actively costs trust. Start with summaries (verifiable, low-stakes) before hunk-level comments?
