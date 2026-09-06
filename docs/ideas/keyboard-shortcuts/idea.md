# Keyboard Shortcuts & Focus Model

## Problem

GitBuddy is entirely mouse-driven. Searching the frontend for keyboard handling turns up exactly two bindings: `@keydown.escape` to clear the file-tree search, and `@keydown.enter` to submit a reply in `CommentsPanel`. There is no global key handling of any kind.

For a tool people live in for hours a day, that means:

- **Every action is a click.** Open a PR, scroll to a file, click the comment affordance, click submit, navigate back, find the next PR. Nothing can be driven from the home row.
- **No way to move between items.** Lists (`PRGroup` rows, `FileTree` nodes, diff hunks, comment threads) have no arrow-key navigation and no roving focus — they are `div`s and buttons in document order.
- **Focus is unmanaged in overlays.** The review modal, `ContextMenu`, `SearchableDropdown`, and `MentionAutocomplete` do not trap focus or restore it on close, so `Tab` walks off into the page behind them.
- **Nothing is discoverable.** Even the two bindings that exist are undocumented.

This is the missing foundation under several existing ideas rather than a competitor to them: `command-palette` needs a global hotkey and a focus-restoring overlay, `review-queue-sessions` needs `j`/`k`/`a`/`s`, `theming-and-accessibility` needs the focus model for screen-reader and keyboard-only users, and `mobile-review` is the surface where none of this applies.

## Rough Approach

### Shortcut Registry

- A `useKeyboardShortcuts` composable holding a registry: key combo, scope, description, handler. Components register on mount and unregister on unmount, so bindings follow the UI that owns them.
- Scopes prevent collisions: `global` (always), `view` (dashboard vs. PR detail), `overlay` (modal open — suppresses everything below it).
- Hard rule: no shortcut fires while focus is in a text input, `TiptapEditor`, or `contenteditable`, except explicit modifier combos. Typing `a` in a comment must never approve a PR.

### Bindings

- **Global** — `?` cheat sheet, `g d` dashboard, `g s` settings, `/` focus search, `Esc` close topmost overlay.
- **Dashboard** — `j`/`k` move between rows, `Enter` open, `p` pin, `.` open the existing `ContextMenu` at the focused row.
- **PR detail** — `j`/`k` next/previous file, `n`/`p` next/previous comment thread, `c` comment on the focused line, `v` toggle viewed, `r` open review.
- Follow GitHub's conventions where they exist. Muscle memory is the whole point; inventing a different scheme spends it for nothing.

### Focus Model

- Roving `tabindex` in `PRGroup` and `FileTree`: one stop for the list, arrows move within it.
- A shared focus-trap utility for overlays that restores focus to the trigger on close — used by the review modal, `ContextMenu`, `SearchableDropdown`, and `create-pr-modal`.
- Visible focus rings that survive the dense visual style. A focus ring that cannot be seen is not a focus ring.

### Discoverability

- A `?` overlay listing active shortcuts by scope, generated from the registry so it cannot drift from reality.
- Show the shortcut hint in tooltips on the buttons that have one.

## Open Questions

- **Build or adopt?** The frontend keeps dependencies tight; `@vueuse/core` has `useMagicKeys` and would cover most of this. Is the dependency worth avoiding the sequence-handling (`g d`) and scope edge cases?
- **Sequences vs. modifiers** — `g d` style prefixes avoid collisions but need a timeout and a visible pending state. Worth it, or stick to single keys and modifiers?
- **Customisation** — user-remappable bindings stored in `UserPreferences`, or a fixed set? Fixed is far simpler and probably sufficient until someone asks.
- **Ordering with `command-palette`** — the palette is the most valuable single binding. Does the registry land first as groundwork, or does the palette ship with its own hotkey and get retrofitted?
- **Conflicts with browser and screen-reader keys** — single-letter shortcuts can collide with assistive-technology navigation. Does the `?` sheet need a global disable toggle?
