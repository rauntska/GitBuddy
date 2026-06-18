---
name: pr
description: Review the current git branch's diff vs its base (main/master, local or origin) and write ./pr.md with a PR title + description that follows this repo's commit/PR conventions. Doesn't commit, push, or create the remote PR — just produces the draft file for the user to edit.
---

# /pr — draft a PR title + description for the current branch

## Goal

Produce a clean `./pr.md` at the repo root, ready for the user to edit and feed into `gh pr create`. The user is the reviewer of *your* draft — they'll tweak wording, add context, run the create. You never push, you never open a PR.

## Steps

### 1. Find the base branch

Pick the most recent commit reachable from any of these (in this priority order). Run them in parallel; the first one that resolves to a real SHA wins:

```
git merge-base origin/main HEAD
git merge-base origin/master HEAD
git merge-base main HEAD
git merge-base master HEAD
```

Prefer `origin/*` over local — it's what the PR will actually be opened against. If none resolve, abort with: `"no main/master branch found locally or on origin; can't determine base"`.

### 2. Gather the branch state

Run in parallel:

- `git log <base>..HEAD --pretty=format:'%h %s%n%b' --no-merges` — full commit messages in the branch (NOT just one-line; you need the bodies for context)
- `git diff --stat <base>..HEAD` — file-level summary
- `git diff <base>..HEAD` — full diff. If it's large (>1500 lines), don't dump it all — read the stat first, then targeted hunks of the most-changed files.
- `git status` — for uncommitted changes detection

### 3. Synthesize

Read the project's `CLAUDE.md` (and any nested ones) to pick up the local PR/commit conventions. In particular:

- **Title format**: prefer `<JIRA-NNN> <imperative verb> <what changed>` whenever a JIRA ticket is in scope. To find the ticket, check in this order and stop at the first hit:
  1. The current branch name (`git rev-parse --abbrev-ref HEAD`). Match `[A-Z]+-\d+` anywhere in it. Common shapes that all yield the same key: `CLVL-193`, `CLVL-193-be-docs`, `feature/CLVL-193-be-docs`, `features/CLVL-193-be-docs`, `bugfix/ITDEV-3297-foo`, `hotfix/ITDEV-3297`. Strip any path prefix (`feature/`, `features/`, `bugfix/`, `hotfix/`, `chore/`, …) before matching.
  2. Commit subjects/bodies in the branch (`git log <base>..HEAD`). Same `[A-Z]+-\d+` regex. If multiple keys appear, the one that's also in the branch name wins; otherwise the most frequent.
  Only fall back to `<scope>: <verb> <what>` (e.g. `langfuse: switch to Cloud, drop self-hosted stack`) when neither source has a key. Match what recent commits on `main` look like (`git log main --oneline -20`) when in doubt.
- Title ≤ 70 chars. Imperative, lowercase verb. No trailing period.
- **Jira backreference**: when a key was identified above, emit `contributes to https://fusebox.atlassian.net/browse/<KEY>` as the very first line of the PR description (between the title and `## Summary`), with one blank line on each side. Example: `contributes to https://fusebox.atlassian.net/browse/CLVL-193`. When no key is in scope (rare — the `<scope>: <verb>` fallback case), omit the line entirely.
- **Body structure**:
  - `## Summary` — 2–4 bullets describing observable behaviour change. Consolidate across commits; don't list every commit verbatim.
  - `## Implementation Details` (optional) — only include if there's a non-obvious approach, trade-off, or edge case worth flagging to a reviewer. Skip it for trivial PRs.
  - `## Test Plan` — concrete checkboxes derived from what changed. Manual steps, automated coverage, regression scenarios. Be specific (`open /knowledge-bot, ask "what is mFRR?", verify response streams`) not vague (`test the bot`).
- Extract the **why** from commit message bodies and code. If you can't tell why something was done, say so in the draft as an open question rather than guessing.
- A reviewer should be able to skim the body in <30s and know what they're looking at.

### 4. Write `./pr.md`

Overwrite if it exists. Don't `git add` it. The format inside the file:

```
<title line — no leading "Title:" or markdown heading>

contributes to https://fusebox.atlassian.net/browse/<KEY>

## Summary
- ...

## Implementation Details
...

## Test Plan
- [ ] ...
```

Drop the `contributes to …` line entirely if no Jira key was identified.

### 5. Tell the user

One-line summary of what you wrote (e.g. "Wrote pr.md — title: 'langfuse: switch to Cloud, drop self-hosted stack', 4 summary bullets, 5 test-plan items.") and a reminder to review/edit before running `gh pr create`.

## Edge cases

- **No commits ahead of base** → abort: "branch has no commits ahead of `<base>`; nothing to PR." Don't write the file.
- **Uncommitted changes present** → proceed, but flag in your response: "Note: you have uncommitted changes; consider committing them before opening the PR."
- **Already on the default branch** → abort: "you're on `<default>`; switch to a feature branch first."
- **Single commit, single file, trivial change** → keep the PR draft tiny too. Don't pad it. Three-bullet Summary + a one-step Test Plan is fine.
- **Mixed scopes (e.g. langfuse swap + posthog setup + docs)** → title should cover the dominant theme; surface the others as Summary bullets so the reviewer isn't surprised.

## Don't

- Don't commit `pr.md` and don't `git add` it. It's a working draft.
- Don't push or run `gh pr create`. The user does that.
- Don't add the `🤖 Generated with Claude Code` / `Co-Authored-By` trailer in the PR body. Trailers live in commit messages per this repo's convention; PR descriptions stay clean.
- Don't speculate about motivations you can't see in commit messages or code. If you're guessing, mark it `(open question — please confirm)`.
- Don't use checklists in Summary; use bullets. Test Plan is the only checklist section.
