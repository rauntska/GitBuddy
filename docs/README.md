# Documentation

All project documentation lives in this folder, organized into three categories:

- **ideas/** — Early-stage concepts, brain dumps, and feature requests. Unstructured notes that capture raw thinking before refinement.
- **specs/** — Refined designs with requirements, architecture decisions, and technical details. These are the output of the design process.
- **plans/** — Implementation plans with step-by-step tasks. These translate specs into actionable work items.

## Convention

Each topic gets its own folder within the relevant category:

```
docs/
├── ideas/<topic>/       # e.g. ideas/notifications/notes.md
├── specs/<topic>/       # e.g. specs/tiptap-editor/design.md
└── plans/<topic>/       # e.g. plans/tiptap-editor/plan.md
```

A topic flows from `ideas/` → `specs/` → `plans/` as it matures. Not every idea needs to reach the spec or plan stage.
