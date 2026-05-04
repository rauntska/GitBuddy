<template>
  <div class="tiptap-editor" @dragover.prevent @drop.prevent="onDrop">
    <div v-if="showToolbar && editor" class="flex items-center gap-1 p-2 bg-slate-800 border-b border-slate-700" :class="roundedTop">
      <button @click="editor.chain().focus().toggleBold().run()" :class="toolbarBtnClass(editor.isActive('bold'))" title="Bold (Ctrl+B)">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 4h8a4 4 0 014 4 4 4 0 01-4 4H6z"></path>
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 12h9a4 4 0 014 4 4 4 0 01-4 4H6z"></path>
        </svg>
      </button>
      <button @click="editor.chain().focus().toggleItalic().run()" :class="toolbarBtnClass(editor.isActive('italic'))" title="Italic (Ctrl+I)">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 4h4m-2 0v16m-4 0h8"></path>
        </svg>
      </button>
      <button @click="editor.chain().focus().toggleCode().run()" :class="toolbarBtnClass(editor.isActive('code'))" title="Inline Code">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4"></path>
        </svg>
      </button>
      <div class="w-px h-5 bg-slate-600 mx-1"></div>
      <button @click="insertLink" :class="toolbarBtnClass(editor.isActive('link'))" title="Link">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"></path>
        </svg>
      </button>
      <button @click="triggerImageUpload" :class="[toolbarBtnClass(false), { 'opacity-50 pointer-events-none': isUploading }]" title="Upload Image">
        <svg v-if="!isUploading" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
        <svg v-else class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
      </button>
      <input ref="fileInputRef" type="file" accept="image/png,image/jpeg,image/gif,image/webp,image/svg+xml" class="hidden" @change="onFileSelected" />
      <button @click="editor.chain().focus().toggleBlockquote().run()" :class="toolbarBtnClass(editor.isActive('blockquote'))" title="Quote">
        <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
          <path d="M14.017 21v-7.391c0-5.704 3.731-9.57 8.983-10.609l.995 2.151c-2.432.917-3.995 3.638-3.995 5.849h4v10h-9.983zm-14.017 0v-7.391c0-5.704 3.748-9.57 9-10.609l.996 2.151c-2.433.917-3.996 3.638-3.996 5.849h3.983v10h-9.983z"></path>
        </svg>
      </button>
      <button @click="editor.chain().focus().toggleCodeBlock().run()" :class="toolbarBtnClass(editor.isActive('codeBlock'))" title="Code Block">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16"></path>
        </svg>
      </button>
      <div class="w-px h-5 bg-slate-600 mx-1"></div>
      <button @click="editor.chain().focus().toggleBulletList().run()" :class="toolbarBtnClass(editor.isActive('bulletList'))" title="Bullet List">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
        </svg>
      </button>
      <button @click="editor.chain().focus().toggleOrderedList().run()" :class="toolbarBtnClass(editor.isActive('orderedList'))" title="Numbered List">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
        </svg>
      </button>
      <div class="flex-1"></div>
      <slot name="toolbar-actions" :editor="editor" />
    </div>

    <EditorContent v-if="editor" :editor="editor" class="tiptap-content" :class="[showToolbar ? roundedBottom : roundedAll]" :style="{ minHeight: `${minHeight}px` }" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onBeforeUnmount } from 'vue';
import { useEditor, EditorContent } from '@tiptap/vue-3';
import StarterKit from '@tiptap/starter-kit';
import Link from '@tiptap/extension-link';
import Placeholder from '@tiptap/extension-placeholder';
import { Markdown } from 'tiptap-markdown';
import { ResizableImage } from '../extensions/ResizableImage';
import { useImageUpload } from '../composables/useImageUpload';

const props = withDefaults(defineProps<{
  modelValue: string;
  placeholder?: string;
  disabled?: boolean;
  showToolbar?: boolean;
  minHeight?: number;
  prId?: number;
}>(), {
  placeholder: 'Write a comment...',
  disabled: false,
  showToolbar: true,
  minHeight: 120,
});

const emit = defineEmits<{
  'update:modelValue': [value: string];
  'save': [];
  'cancel': [];
}>();

const fileInputRef = ref<HTMLInputElement | null>(null);
const isUploading = ref(false);

const { uploadImage } = useImageUpload();

const roundedTop = computed(() => props.showToolbar ? 'rounded-t-lg' : 'rounded-lg');
const roundedBottom = computed(() => props.showToolbar ? 'rounded-b-lg border-t-0' : 'rounded-lg');
const roundedAll = computed(() => 'rounded-lg');

const toolbarBtnClass = (active: boolean) =>
  `p-1.5 rounded hover:bg-slate-700 transition-colors ${active ? 'bg-slate-700 text-white' : 'text-slate-300 hover:text-white'}`;

const editor = useEditor({
  content: props.modelValue || '',
  extensions: [
    StarterKit.configure({
      heading: { levels: [1, 2, 3] },
    }),
    Link.configure({
      openOnClick: false,
      HTMLAttributes: { class: 'text-blue-400 hover:text-blue-300 underline' },
    }),
    Placeholder.configure({
      placeholder: props.placeholder,
    }),
    ResizableImage,
    Markdown.configure({ html: true, breaks: true }),
  ],
  editorProps: {
    attributes: {
      class: 'tiptap-prose prose prose-invert prose-sm max-w-none text-sm text-slate-300',
    },
    handlePaste: (_view: any, event: ClipboardEvent) => {
      const files = event.clipboardData?.files;
      if (files && files.length > 0) {
        const file = files[0];
        if (file && file.type.startsWith('image/')) {
          event.preventDefault();
          handleImageFile(file);
          return true;
        }
      }
      return false;
    },
    handleDrop: (_view: any, event: DragEvent, _slice: any, moved: boolean) => {
      if (moved) return false;
      const files = event.dataTransfer?.files;
      if (files && files.length > 0) {
        const file = files[0];
        if (file && file.type.startsWith('image/')) {
          event.preventDefault();
          handleImageFile(file);
          return true;
        }
      }
      return false;
    },
  },
  onUpdate: ({ editor: e }) => {
    const md = (e.storage as any).markdown?.getMarkdown();
    if (md !== undefined) {
      emit('update:modelValue', md);
    }
  },
}) as any;

// Keyboard shortcuts via addKeyboardShortcuts after creation
if (editor.value) {
  const originalKeyDown = editor.value.view.props.handleKeyDown;
  editor.value.view.setProps({
    handleKeyDown: (view: any, event: KeyboardEvent) => {
      if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
        event.preventDefault();
        emit('save');
        return true;
      }
      if (event.key === 'Escape') {
        emit('cancel');
        return true;
      }
      if (originalKeyDown) {
        return originalKeyDown(view, event);
      }
      return false;
    },
  });
}

// Sync external modelValue changes into the editor
watch(() => props.modelValue, (newVal) => {
  if (!editor.value) return;
  const currentMd = (editor.value.storage as any).markdown?.getMarkdown();
  if (newVal !== currentMd) {
    editor.value.commands.setContent(newVal || '');
  }
});

const handleImageFile = async (file: File) => {
  if (!props.prId || !editor.value) return;

  const uploadingSrc = URL.createObjectURL(file);

  editor.value.chain().focus().setImage({
    src: uploadingSrc,
    alt: file.name,
    'data-uploading': 'true',
  } as any).run();

  isUploading.value = true;

  try {
    const url = await uploadImage(props.prId, file);

    const { tr } = editor.value.state;
    let replaced = false;
    editor.value.state.doc.descendants((node: any, pos: number) => {
      if (!replaced && node.type.name === 'resizableImage' && node.attrs.src === uploadingSrc) {
        const newAttrs = { ...node.attrs, src: url, 'data-uploading': null };
        const nodeType = editor.value!.schema.nodes.resizableImage;
        if (nodeType) {
          const newNode = nodeType.create(newAttrs);
          editor.value!.view.dispatch(tr.replaceWith(pos, pos + node.nodeSize, newNode));
        }
        replaced = true;
        return false;
      }
    });

    URL.revokeObjectURL(uploadingSrc);
  } catch {
    const { tr } = editor.value.state;
    let removed = false;
    editor.value.state.doc.descendants((node: any, pos: number) => {
      if (!removed && node.type.name === 'resizableImage' && node.attrs.src === uploadingSrc) {
        editor.value!.view.dispatch(tr.delete(pos, pos + node.nodeSize));
        removed = true;
        return false;
      }
    });
    URL.revokeObjectURL(uploadingSrc);
  } finally {
    isUploading.value = false;
  }
};

const triggerImageUpload = () => {
  fileInputRef.value?.click();
};

const onFileSelected = (event: Event) => {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file || !file.type.startsWith('image/')) {
    input.value = '';
    return;
  }
  input.value = '';
  handleImageFile(file);
};

const onDrop = (event: DragEvent) => {
  const files = event.dataTransfer?.files;
  if (files && files.length > 0) {
    const file = files[0];
    if (file && file.type.startsWith('image/')) {
      event.preventDefault();
      handleImageFile(file);
    }
  }
};

const insertLink = () => {
  if (!editor.value) return;
  const previous = editor.value.getAttributes('link').href;
  const url = window.prompt('URL', previous || 'https://');
  if (url === null) return;
  if (url === '') {
    editor.value.chain().focus().extendMarkRange('link').unsetLink().run();
    return;
  }
  editor.value.chain().focus().extendMarkRange('link').setLink({ href: url }).run();
};

const focus = () => {
  editor.value?.commands.focus();
};

onBeforeUnmount(() => {
  editor.value?.destroy();
});

defineExpose({ focus, editor });
</script>

<style>
.tiptap-content .tiptap {
  padding: 12px;
  background: #0f172a;
  border: 1px solid #334155;
  outline: none;
  min-height: inherit;
}

.tiptap-content .tiptap:focus {
  border-color: #3b82f6;
  box-shadow: 0 0 0 1px #3b82f6;
}

.tiptap-content .tiptap p.is-editor-empty:first-child::before {
  color: #475569;
  content: attr(data-placeholder);
  float: left;
  height: 0;
  pointer-events: none;
}

.tiptap .ProseMirror {
  min-height: inherit;
}

.tiptap p { margin-bottom: 0.5rem; }
.tiptap h1 { font-size: 1.25rem; font-weight: 600; color: #f1f5f9; margin-top: 1rem; margin-bottom: 0.5rem; }
.tiptap h2 { font-size: 1.125rem; font-weight: 600; color: #f1f5f9; margin-top: 1rem; margin-bottom: 0.5rem; }
.tiptap h3 { font-size: 1rem; font-weight: 600; color: #f1f5f9; margin-top: 1rem; margin-bottom: 0.5rem; }
.tiptap code { background: #1e293b; padding: 1px 4px; border-radius: 3px; color: #f472b6; font-size: 0.875rem; }
.tiptap pre { background: #1e293b; padding: 12px; border-radius: 8px; overflow-x: auto; margin: 8px 0; }
.tiptap pre code { background: transparent; padding: 0; }
.tiptap a { color: #60a5fa; text-decoration: underline; }
.tiptap a:hover { color: #93c5fd; }
.tiptap blockquote { border-left: 4px solid #3b82f6; padding-left: 16px; font-style: italic; color: #94a3b8; }
.tiptap ul { list-style: disc; padding-left: 16px; margin-bottom: 8px; }
.tiptap ol { list-style: decimal; padding-left: 16px; margin-bottom: 8px; }
.tiptap li { margin-bottom: 4px; }
.tiptap img { max-width: 100%; border-radius: 8px; }
.tiptap hr { border-color: #334155; margin: 12px 0; }
</style>
