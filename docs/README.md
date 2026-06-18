# Documentation

Project documentation lives here, organized into three maturity stages. Each topic gets its own folder; a topic flows from `ideas/` → `specs/` → `plans/` as it matures. Not every idea reaches the spec or plan stage.

## Layout

```
docs/
├── ideas/<topic>/idea.md       # raw concept, brain dump, open questions
├── specs/<topic>/design.md     # refined design: requirements, architecture, decisions
└── plans/<topic>/plan.md       # actionable implementation plan with task groups
```

## File naming

| Folder   | Filename    |
|----------|-------------|
| `ideas/` | `idea.md`   |
| `specs/` | `design.md` |
| `plans/` | `plan.md`   |

## Status markers

Ideas that have shipped are kept in place (not archived) and marked with a top-of-file line so they're easy to filter:

```
> **Status: Implemented** — see commit history for the shipped code.
```

## Execution specs (feature-spec skill)

When kicking off a new feature with the `/feature-spec` skill, the dated execution spec package is written to `specs/` at the **repo root** (not under `docs/`). Those specs are ephemeral — tied to a feature branch and consumed during implementation. `docs/` is for durable project knowledge.
