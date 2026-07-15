# Documentation

Project documentation is split across two locations: **durable** knowledge under `docs/`, and **ephemeral** execution specs under `specs/` at the repo root. This matches how the project actually runs spec-driven development.

## Layout

```
docs/
├── ideas/<topic>/idea.md       # raw concept, brain dump, open questions
├── specs/<topic>/design.md     # durable refined design (post-implementation)
└── reference/<name>.md         # evergreen material (visual style, architecture overviews)

specs/                          # repo root, NOT under docs/
└── YYYY-MM-DD-<feature>/       # ephemeral execution spec from /feature-spec
    ├── requirements.md
    ├── plan.md
    └── validation.md
```

## Lifecycle

A feature flows through four steps:

1. **Capture an idea** in `docs/ideas/<topic>/idea.md`. Rough is fine — problem statement, rough approach, open questions.
2. **Spawn an execution spec** by running the `/feature-spec` skill. It writes `specs/YYYY-MM-DD-<topic>/` at the repo root with `requirements.md`, `plan.md`, and `validation.md`. This package is ephemeral — tied to a feature branch and consumed during implementation.
3. **Implement** from the execution spec.
4. **On merge**, mark the durable side of the trail:
   - Add the Implemented marker (below) to `docs/ideas/<topic>/idea.md`.
   - If the design has long-term value, promote a durable `docs/specs/<topic>/design.md` that summarises the shipped design and cross-links the execution spec that produced it. See `specs/branches-without-prs-refresh/design.md` for the template. Skipping the idea stage occasionally is fine — `tiptap-editor` did — but expect to start in `ideas/`.

## File naming

| Location                  | Filename    |
|---------------------------|-------------|
| `docs/ideas/<topic>/`     | `idea.md`   |
| `docs/specs/<topic>/`     | `design.md` |
| `docs/reference/`         | `<name>.md` (no `<topic>/` wrapper — these aren't staged) |

## Topic slug convention

Use a single canonical topic slug across `docs/ideas/<slug>/`, `docs/specs/<slug>/`, and `specs/YYYY-MM-DD-<slug>/`. Avoid drift like `branches-without-prs-refresh` (durable) vs `branches-without-prs-backend-refresh` (execution). When an execution spec legitimately extends an existing topic (e.g. `pr-analytics-author-filter`), keep the parent topic as a prefix so the relationship is obvious.

## Status markers

Shipped ideas are kept in place (not archived) and marked with a top-of-file line so they're easy to filter:

```
> **Status: Implemented** — see commit history for the shipped code.
```

Durable `docs/specs/<topic>/design.md` files may carry the same marker with a cross-link to the execution spec that produced them:

```
> **Status: Implemented** — see `specs/YYYY-MM-DD-<topic>/` for the execution spec.
```

## Evergreen reference

`docs/reference/` holds material that has no feature lifecycle — the visual style guide, future architecture overviews, anything that lives outside the idea → spec → shipped flow. Files are named directly (`visual-style.md`) since they aren't staged and don't belong to a topic folder.

## Execution specs (`/feature-spec` skill)

When kicking off a new feature with the `/feature-spec` skill, the dated execution spec package is written to `specs/` at the **repo root** (not under `docs/`). Each folder is named `YYYY-MM-DD-<feature>` and contains exactly `requirements.md`, `plan.md`, and `validation.md`. These specs are ephemeral — tied to a feature branch and consumed during implementation. `docs/` is for durable project knowledge.
