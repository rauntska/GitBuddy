# Apply-able Code Suggestions

## Problem

Half of review comments boil down to "change this line to X". Today a GitBuddy reviewer writes that in prose (Tiptap editor → markdown), the author reads it, switches to their editor, and re-types the change. GitHub solves this with **suggestion blocks** — fenced code blocks marked `suggestion` that render with an "Apply" button producing a commit — but GitBuddy can neither *author* nor *apply* them:

- The `TiptapEditor` has a generic code block, no suggestion variant, so reviewers hand-type the ` ```suggestion ` fence.
- `CommentBody` renders suggestions as plain code blocks with no affordance for the author.
- Applying a suggestion requires the GraphQL `createCommitOnBranch` mutation (there is no REST endpoint), which GitBuddy never calls.

## Rough Approach

### Authoring (reviewer side)

- **Tiptap node extension** — a `suggestionBlock` code node (serializes to a ` ```suggestion ` fence via tiptap-markdown). Toolbar button in the comment form.
- **Line-selection flow** — in `FileDiffViewer`, select lines → "Suggest change…" opens the comment form pre-filled with a suggestion block containing the selected lines. The pending-review inline-comment path (`POST {id}/pending-review/comments`) is the natural vehicle.
- Validation before submit: suggestion content must apply cleanly to the current file (server can verify with the file blob at the comment's path/sha).

### Rendering (both sides)

- `CommentBody` parses ` ```suggestion ` fences into a distinct component: diff-style rendering (old lines dimmed, suggestion lines highlighted) instead of a flat code block — this alone makes review threads far more readable even for people applying on GitHub.

### Applying (author side)

- **Per-suggestion "Apply" button** on rendered suggestion blocks, visible only to users with push access (determine via existing collaborators data).
- **Batch apply** — checkbox per suggestion across multiple comments in the same PR, "Apply N suggestions" produces a single commit. This is the part GitHub's UI still does poorly; GitBuddy can do better.
- Implementation: GraphQL `createCommitOnBranch` with the file blobs updated per suggestion, via the acting user's token (`GitHubTokenService` already resolves user-context tokens). Handle 409 conflicts when the suggestion no longer applies.

### Safety Rails

- Suggestion anchored to an outdated diff → disabled with explanation ("file changed since comment").
- Applied suggestions get a marker ("applied in `abc1234`") by resolving the commit afterwards.

## Open Questions

- **Octokit GraphQL auth** — the codebase uses GraphQL for reads; does the user-token flow work for mutations as-is, or does `GitHubGraphQLService` need a write-capable path?
- **Multi-file commits** — batch apply across different files in one commit should be fine via `createCommitOnBranch`; confirm head-SHA handling (needs current branch head, i.e. the same `HeadSha` tracking discussed in `review-delta-view`).
- **Partial overlap** — two suggestions touching the same lines: apply sequentially with conflict detection, or reject the second?
- **Should GitBuddy mark applied state** — query commit history vs. heuristic (thread resolution after apply)?
- **Split diffs / multi-line limits** — GitHub caps suggestions at 10 lines (recently lifted?); confirm current limits rather than discovering them via API errors.
