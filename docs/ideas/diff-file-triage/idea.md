# Diff File Triage — Separate Code From Prose and Noise

## Problem

A reviewer's scarce resource is attention on *code*. The diff spends it on everything equally.

Take this repository's own PR #14 (`Add rendered markdown diff view`). Its 3,313 added lines break down as:

| Kind | Lines |
|---|---|
| Hand-written code | 461 |
| Spec markdown (`specs/2026-08-11-.../{plan,requirements,validation}.md`) | 403 |
| Generated (`package-lock.json`, EF migration + `.Designer.cs`) | 2,449 |

The reviewer wades through 2,852 lines of non-code to reach 461 lines that need judgement. Markdown alone is **47% of the human-authored diff**. PR #15 was 1,148 lines, 100% markdown.

This is not incidental — it is what spec-driven development produces. Every `/feature-spec` run adds `requirements.md`, `plan.md`, and `validation.md` to the branch, and every AI-assisted change tends to carry design notes with it. The prose is genuinely useful; reviewing it interleaved with code, line by line, is not.

What GitBuddy does with this today:

- **`FileTree` has no concept of file kind.** It offers name search and a viewed/unviewed filter with counts — a good pattern, but nothing distinguishes `PriorityService.cs` from `plan.md` from `package-lock.json`. They sort alphabetically into the same tree, so spec files land *between* code files rather than after them.
- **`PRSizeBadge` counts every line the same.** `getPRSize` thresholds are XS <50, S <200, M <500, L <1000, XL ≥1000 over `additions + deletions`. 403 lines of spec prose alone pushes a PR from S to M; a lockfile bump makes anything XL. The badge that exists to tell reviewers "this is a big one" is reporting mostly noise, and `pr-analytics` measures PR size from the same undifferentiated number.
- **Viewed-state is per file, one click each.** `UserFileViewedState` tracks individual files, so dismissing 12 generated files means 12 clicks.
- **Nothing warns before you open it.** The dashboard gives no hint that a PR is 90% docs, so triage happens only after committing to opening it.

Note the boundary with what already shipped: PR #14 delivered `MarkdownDiffViewer` with a rendered/source toggle, which fixed how markdown *reads*. This idea is about **volume and ordering**, not legibility — rendering 400 lines of spec beautifully still puts 400 lines between the reviewer and the code. The two compose well; this one leans on the rendered view rather than duplicating it.

## Rough Approach

### Classify Files by Kind

- Add `Kind` to `FileDiff` — `code`, `docs`, `generated`, `config`, `test`, `asset` — populated during sync. Half the work is already done: `LanguageDetectionService` runs per file and `FileDiff.Language` is stored, so `.md` files are already labelled `markdown`.
- Classification is path rules plus language: `specs/**` and `docs/**` and `*.md` → docs; `package-lock.json`, `Migrations/**`, `*.Designer.cs`, `*.min.*`, `__snapshots__/**` → generated; test-path conventions → test. Deliberately boring and inspectable — reviewers must be able to see *why* a file was classified, and override it.
- Server-side, not client-side, so the classification is shared, cacheable, and available to analytics.

### Honest Size Signals

- `PRSizeBadge` reports **code** lines, with the rest as secondary detail: tooltip `461 code · 403 docs · 2,449 generated`, and the size letter derived from code alone.
- A docs-only PR gets its own marker on `PRRow` rather than a size letter. "This is 400 lines of prose and no code" is exactly the triage signal that lets a reviewer pick it up in a five-minute gap instead of deferring it.
- `pr-analytics` size and latency metrics get materially more honest once "size" means code.

### Order and Group the Tree

- Group `FileTree` by kind with per-group counts, code first, generated last and collapsed by default.
- Kind filter chips next to the existing viewed/unviewed filter — same interaction, same place, no new vocabulary for users to learn.
- **Bulk viewed-marking per kind**: "mark all 12 generated files viewed" as one action, reusing `UserFileViewedState`.
- Persist the preferred grouping per user alongside the other `UserPreferences` diff settings.

### Treat Specs as Context, Not Diff

The deeper fix for spec markdown specifically: a spec is usually the *input* the code was written from. Reviewers want to read it once, then judge the code against it — not review it as a line-by-line changeset.

- Surface spec/docs files as a collapsible **context panel** rendered with the existing `MarkdownDiffViewer`, sitting alongside the code rather than inside the file sequence.
- This is the same slot `linked-issues` wants for "the requirement this PR satisfies", and fits the Conversation/Files split in `pr-detail-navigation`. Worth designing the three together rather than bolting on a third panel.

### Shared Classifier

`large-diff-performance` proposes collapsing large and generated files by default for *performance* reasons — same mechanism, different motive. These should share one classifier rather than growing two heuristics that disagree about what counts as generated.

## Open Questions

- **Is making specs skippable actually the goal?** The annoyance may be partly signal: if spec files become one collapsed panel nobody expands, the team loses the review of *intent* that spec-driven development is supposed to buy. Possibly the right answer is to make specs prominent but reviewed *first and separately*, rather than easy to skip. This is the question to settle before building anything.
- **Hiding generated files has a security cost.** An unexpected `package-lock.json` change is a real supply-chain vector, and collapse-by-default is how it goes unnoticed. Collapsed-with-visible-count seems safer than hidden — but does a dependency-diff summary belong here, or in its own idea?
- **Where do classification rules live** — hard-coded heuristics for v1, `RepositoryRule` rows, or a committed `.gitbuddy` file per repo? Heuristics ship fastest and are probably right for v1, but "why is my file marked generated" needs an answer.
- **Does `Kind` need a migration and backfill**, or can it be computed on read from `Path` and the existing `Language`? Computing on read avoids a migration entirely and may be enough until analytics needs to query by it.
- **Per-user or per-repo defaults** for grouping and collapse behaviour?
- **Should docs-only PRs get lighter gates** — fewer required approvals, skipped CI? That is `pr-quality-gates` territory, but this idea produces the classification that would make it possible.
- **Renames and moves.** A spec file moved from `docs/ideas/` to `docs/specs/` is pure noise in the diff. Does rename detection deserve its own treatment here?
