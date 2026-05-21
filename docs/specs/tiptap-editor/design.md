# Tiptap WYSIWYG Markdown Editor

## Context

Both `EditableDescription.vue` and `RichTextEditor.vue` currently use plain textareas with a split Write/Preview pane. Image uploads go through a separate `ImageResizeCard` modal. This creates a disjointed experience — users write markdown, switch to preview to check it, and resize images in a popup.

**Goal:** Replace both editors with a single-pane Tiptap-based WYSIWYG editor that renders markdown as-you-type. Images appear inline with resize handles directly in the editor. No separate resize modal. No split pane.

## Library Choice

**Tiptap** (ProseMirror-based) with `tiptap-markdown` for markdown serialization/deserialization.

- Headless — full control over toolbar styling to match existing dark theme
- Official Vue 3 support
- Custom `Image` node extension with resize handles is a well-documented pattern
- Rich extension ecosystem (bold, italic, code, links, lists, blockquotes, code blocks, tables)

## Architecture

### New/Modified Files

| File | Action | Purpose |
|------|--------|---------|
| `src/components/TiptapEditor.vue` | **New** | Shared WYSIWYG editor component used by both editors |
| `src/extensions/ResizableImage.ts` | **New** | Custom Tiptap extension: `<img>` node with drag-to-resize handles |
| `src/extensions/MarkdownExtension.ts` | **New** | Configures tiptap-markdown for bidirectional markdown conversion |
| `src/composables/useImageUpload.ts` | **Modify** | Remove `pendingImage` / `showResizeModal` flow. Upload immediately, insert as Tiptap node |
| `src/components/EditableDescription.vue` | **Modify** | Replace textarea + preview split with `TiptapEditor` |
| `src/components/RichTextEditor.vue` | **Modify** | Replace textarea + preview modes with `TiptapEditor` |
| `src/components/ImageResizeCard.vue` | **Delete** | No longer needed |

### Component Structure

```
TiptapEditor.vue
├── Toolbar (bold, italic, code, link, image upload, quote, code block, lists)
├── Editor content area (Tiptap EditorContent)
│   └── ResizableImage nodes (inline images with drag handles)
└── Props: modelValue (markdown string), placeholder, prId
    Emits: update:modelValue, save (Ctrl+Enter)
```

### Data Flow

**Loading (markdown → editor):**
1. Parent passes `content` prop as markdown string
2. `TiptapEditor` uses `tiptap-markdown` to parse markdown into ProseMirror document
3. Images render as `<img>` elements via `ResizableImage` extension

**Saving (editor → markdown):**
1. User clicks Save or presses Ctrl+Enter
2. `tiptap-markdown` serializes ProseMirror document back to markdown string
3. Editor emits `update:modelValue` with the markdown string
4. Parent handles API save as before

**Image upload flow:**
1. User pastes/drops/clicks image upload button
2. File uploads to server immediately (`POST /images/upload`)
3. On success, Tiptap inserts a `ResizableImage` node with the returned URL
4. Placeholder text (`![Uploading...]`) shows in the node's place during upload
5. User can drag resize handles anytime in the editor
6. Resize dimensions are stored as attributes on the node (`width`, `height`)
7. On save, these dimensions serialize to markdown as `![name](url){width=320}`

### ResizableImage Extension

Custom Tiptap `Node` extension that extends the built-in `Image` node:

- Adds `width` and `height` attributes (optional, in pixels)
- Renders a wrapper `<div>` around the `<img>` with four corner drag handles
- Handles mouse drag events to resize (maintaining aspect ratio by default)
- Shows a small dimension label (`320 × 180`) while resizing
- Attributes flow into markdown serialization via tiptap-markdown config

### Toolbar

Reuse existing toolbar button styles. The toolbar maps directly to Tiptap commands:
- Bold → `toggleBold`
- Italic → `toggleItalic`
- Code → `toggleCode`
- Link → `setLink` (prompt for URL)
- Image upload → custom handler that calls `useImageUpload`
- Quote → `toggleBlockquote`
- Code block → `toggleCodeBlock`
- Bullet list → `toggleBulletList`
- Numbered list → `toggleOrderedList`

### EditableDescription.vue Changes

- Remove the grid Write/Preview split layout
- Replace with single `TiptapEditor` component
- Keep the hover edit button, expanded/collapse behavior for the read-only rendered view
- Read-only view continues using `marked` to render markdown (unchanged)
- `processImages` proxy logic for GitHub attachments stays as-is

### RichTextEditor.vue Changes

- Remove the write/preview mode toggle
- Replace with single `TiptapEditor` component
- Keep existing emit interface (`update:modelValue`, `submit`)

### useImageUpload Composable Changes

- Remove `pendingImage` ref and `showResizeModal()` / `cancelPendingImage()` methods
- `handleImageInsertion()` no longer takes textarea refs — instead receives a Tiptap editor instance
- Inserts a `ResizableImage` node directly via `editor.chain().focus().setImage({ src: url })`
- Placeholder during upload: insert node with a loading state attribute, update on completion

## Scope Exclusions

- **No resize in rendered view** — images in the read-only markdown rendering are not resizable
- **No table editing** — not in current toolbar, not adding now
- **No collaborative editing** — single-user editing only
- **No markdown source toggle** — WYSIWYG only, no raw markdown view (can add later if needed)

## Verification

1. **EditableDescription**: Open a PR detail page → click edit → type markdown → image renders inline → drag to resize → save → markdown saved correctly → re-open → dimensions preserved
2. **RichTextEditor**: Open a comment editor → same flow → submit comment → image displays correctly
3. **Image upload via paste**: Paste an image → uploads immediately → appears inline → resizable
4. **Image upload via drag & drop**: Drop an image → same behavior
5. **Image upload via toolbar button**: Click image icon → file picker → same behavior
6. **Existing markdown content**: PR descriptions with existing images, code blocks, lists render correctly on load
7. **GitHub proxied images**: Existing `processImages` flow still works for GitHub attachment URLs
8. **Keyboard shortcuts**: Ctrl+B (bold), Ctrl+I (italic), Ctrl+Enter (save), Escape (cancel) all work
