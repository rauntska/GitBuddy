<template>
  <div class="rich-text-editor">
    <!-- Toolbar -->
    <div v-if="showToolbar" class="toolbar flex items-center gap-1 p-2 bg-slate-800 border-b border-slate-700 rounded-t-lg">
      <button
        @click="insertFormat('bold')"
        class="toolbar-btn p-1.5 rounded hover:bg-slate-700 text-slate-300 hover:text-white transition-colors"
        title="Bold (Ctrl+B)"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 4h8a4 4 0 014 4 4 4 0 01-4 4H6z"></path>
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 12h9a4 4 0 014 4 4 4 0 01-4 4H6z"></path>
        </svg>
      </button>
      <button
        @click="insertFormat('italic')"
        class="toolbar-btn p-1.5 rounded hover:bg-slate-700 text-slate-300 hover:text-white transition-colors"
        title="Italic (Ctrl+I)"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 4h4m-2 0v16m-4 0h8"></path>
        </svg>
      </button>
      <button
        @click="insertFormat('code')"
        class="toolbar-btn p-1.5 rounded hover:bg-slate-700 text-slate-300 hover:text-white transition-colors"
        title="Inline Code"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4"></path>
        </svg>
      </button>
      <div class="w-px h-5 bg-slate-600 mx-1"></div>
      <button
        @click="insertFormat('link')"
        class="toolbar-btn p-1.5 rounded hover:bg-slate-700 text-slate-300 hover:text-white transition-colors"
        title="Link"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"></path>
        </svg>
      </button>
      <button
        @click="insertFormat('quote')"
        class="toolbar-btn p-1.5 rounded hover:bg-slate-700 text-slate-300 hover:text-white transition-colors"
        title="Quote"
      >
        <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
          <path d="M14.017 21v-7.391c0-5.704 3.731-9.57 8.983-10.609l.995 2.151c-2.432.917-3.995 3.638-3.995 5.849h4v10h-9.983zm-14.017 0v-7.391c0-5.704 3.748-9.57 9-10.609l.996 2.151c-2.433.917-3.996 3.638-3.996 5.849h3.983v10h-9.983z"></path>
        </svg>
      </button>
      <button
        @click="insertFormat('codeBlock')"
        class="toolbar-btn p-1.5 rounded hover:bg-slate-700 text-slate-300 hover:text-white transition-colors"
        title="Code Block"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16"></path>
        </svg>
      </button>
      <div class="w-px h-5 bg-slate-600 mx-1"></div>
      <button
        @click="insertFormat('ul')"
        class="toolbar-btn p-1.5 rounded hover:bg-slate-700 text-slate-300 hover:text-white transition-colors"
        title="Bullet List"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
        </svg>
      </button>
      <button
        @click="insertFormat('ol')"
        class="toolbar-btn p-1.5 rounded hover:bg-slate-700 text-slate-300 hover:text-white transition-colors"
        title="Numbered List"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
        </svg>
      </button>
      <div class="flex-1"></div>
      <!-- View Toggle -->
      <div class="flex items-center gap-1 bg-slate-700 rounded p-0.5">
        <button
          @click="viewMode = 'write'"
          :class="['px-2 py-1 text-xs rounded transition-colors', viewMode === 'write' ? 'bg-slate-600 text-white' : 'text-slate-400 hover:text-white']"
        >
          Write
        </button>
        <button
          @click="viewMode = 'preview'"
          :class="['px-2 py-1 text-xs rounded transition-colors', viewMode === 'preview' ? 'bg-slate-600 text-white' : 'text-slate-400 hover:text-white']"
        >
          Preview
        </button>
      </div>
    </div>

    <!-- Editor Area -->
    <div v-show="viewMode === 'write'" class="editor-wrapper">
      <textarea
        ref="textareaRef"
        :value="modelValue"
        @input="onInput"
        @keydown="onKeydown"
        :placeholder="placeholder"
        :disabled="disabled"
        class="w-full min-h-[120px] p-3 bg-slate-900 text-slate-100 border border-slate-700 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none resize-y font-mono text-sm"
        :class="[showToolbar ? 'rounded-b-lg border-t-0' : 'rounded-lg']"
        :style="{ minHeight: `${minHeight}px` }"
      ></textarea>
      <slot name="autocomplete" :textarea="textareaRef" :insert-text="insertText" :get-cursor-position="getCursorPosition"></slot>
    </div>

    <!-- Preview Area -->
    <div
      v-show="viewMode === 'preview'"
      class="preview-area p-3 bg-slate-900 border border-slate-700 min-h-[120px] overflow-auto"
      :class="[showToolbar ? 'rounded-b-lg border-t-0' : 'rounded-lg']"
      :style="{ minHeight: `${minHeight}px` }"
    >
      <div v-if="modelValue" class="prose prose-invert prose-sm max-w-none" v-html="renderedMarkdown"></div>
      <p v-else class="text-slate-500 italic">Nothing to preview</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, nextTick } from 'vue';
import { marked } from 'marked';
import Prism from 'prismjs';

const props = withDefaults(defineProps<{
  modelValue: string;
  placeholder?: string;
  disabled?: boolean;
  showToolbar?: boolean;
  minHeight?: number;
}>(), {
  placeholder: 'Write a comment...',
  disabled: false,
  showToolbar: true,
  minHeight: 120
});

const emit = defineEmits<{
  'update:modelValue': [value: string];
  'save': [];
  'cancel': [];
}>();

const textareaRef = ref<HTMLTextAreaElement | null>(null);
const viewMode = ref<'write' | 'preview'>('write');

const renderedMarkdown = computed(() => {
  if (!props.modelValue) return '';

  // Configure marked
  marked.setOptions({
    breaks: true,
    gfm: true
  });

  const html = marked.parse(props.modelValue) as string;

  // Apply syntax highlighting to code blocks
  nextTick(() => {
    document.querySelectorAll('.preview-area pre code').forEach((block) => {
      Prism.highlightElement(block as Element);
    });
  });

  return html;
});

const onInput = (event: Event) => {
  const target = event.target as HTMLTextAreaElement;
  emit('update:modelValue', target.value);
};

const onKeydown = (event: KeyboardEvent) => {
  // Handle Tab key for indentation
  if (event.key === 'Tab') {
    event.preventDefault();
    const textarea = textareaRef.value;
    if (!textarea) return;

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const value = textarea.value;

    if (event.shiftKey) {
      // Remove indent
      const lineStart = value.lastIndexOf('\n', start - 1) + 1;
      if (value.substring(lineStart, lineStart + 2) === '  ') {
        const newValue = value.substring(0, lineStart) + value.substring(lineStart + 2);
        emit('update:modelValue', newValue);
        nextTick(() => {
          textarea.selectionStart = textarea.selectionEnd = start - 2;
        });
      }
    } else {
      // Add indent
      const newValue = value.substring(0, start) + '  ' + value.substring(end);
      emit('update:modelValue', newValue);
      nextTick(() => {
        textarea.selectionStart = textarea.selectionEnd = start + 2;
      });
    }
  }

  // Handle Ctrl+Enter to save
  if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
    event.preventDefault();
    emit('save');
  }

  // Handle Escape to cancel
  if (event.key === 'Escape') {
    emit('cancel');
  }
};

const insertText = (text: string, replaceStart?: number, replaceEnd?: number) => {
  const textarea = textareaRef.value;
  if (!textarea) return;

  const start = replaceStart ?? textarea.selectionStart;
  const end = replaceEnd ?? textarea.selectionEnd;
  const value = textarea.value;

  const newValue = value.substring(0, start) + text + value.substring(end);
  emit('update:modelValue', newValue);

  nextTick(() => {
    textarea.focus();
    const cursorPos = start + text.length;
    textarea.selectionStart = textarea.selectionEnd = cursorPos;
  });
};

const getCursorPosition = () => {
  const textarea = textareaRef.value;
  if (!textarea) return { start: 0, end: 0, text: '' };

  return {
    start: textarea.selectionStart,
    end: textarea.selectionEnd,
    text: textarea.value
  };
};

const insertFormat = (format: string) => {
  const textarea = textareaRef.value;
  if (!textarea) return;

  const start = textarea.selectionStart;
  const end = textarea.selectionEnd;
  const selectedText = textarea.value.substring(start, end);
  let newText = '';

  switch (format) {
    case 'bold':
      newText = `**${selectedText || 'bold text'}**`;
      break;
    case 'italic':
      newText = `_${selectedText || 'italic text'}_`;
      break;
    case 'code':
      newText = `\`${selectedText || 'code'}\``;
      break;
    case 'link':
      newText = `[${selectedText || 'link text'}](url)`;
      break;
    case 'quote':
      newText = `> ${selectedText || 'quote'}`;
      break;
    case 'codeBlock':
      newText = `\`\`\`\n${selectedText || 'code'}\n\`\`\``;
      break;
    case 'ul':
      newText = `- ${selectedText || 'list item'}`;
      break;
    case 'ol':
      newText = `1. ${selectedText || 'list item'}`;
      break;
  }

  insertText(newText, start, end);
};

const focus = () => {
  textareaRef.value?.focus();
};

defineExpose({
  focus,
  insertText,
  getCursorPosition
});
</script>

<style scoped>
.rich-text-editor :deep(.prose) {
  @apply text-slate-300;
}

.rich-text-editor :deep(.prose h1),
.rich-text-editor :deep(.prose h2),
.rich-text-editor :deep(.prose h3) {
  @apply text-slate-100 font-semibold mt-4 mb-2;
}

.rich-text-editor :deep(.prose h1) {
  @apply text-xl;
}

.rich-text-editor :deep(.prose h2) {
  @apply text-lg;
}

.rich-text-editor :deep(.prose h3) {
  @apply text-base;
}

.rich-text-editor :deep(.prose p) {
  @apply mb-2;
}

.rich-text-editor :deep(.prose code) {
  @apply bg-slate-800 px-1 py-0.5 rounded text-pink-400 text-sm;
}

.rich-text-editor :deep(.prose pre) {
  @apply bg-slate-800 p-3 rounded-lg overflow-x-auto my-2;
}

.rich-text-editor :deep(.prose pre code) {
  @apply bg-transparent p-0;
}

.rich-text-editor :deep(.prose a) {
  @apply text-blue-400 hover:text-blue-300 underline;
}

.rich-text-editor :deep(.prose blockquote) {
  @apply border-l-4 border-blue-500 pl-4 italic text-slate-400;
}

.rich-text-editor :deep(.prose ul),
.rich-text-editor :deep(.prose ol) {
  @apply pl-4 mb-2;
}

.rich-text-editor :deep(.prose ul) {
  @apply list-disc;
}

.rich-text-editor :deep(.prose ol) {
  @apply list-decimal;
}

.rich-text-editor :deep(.prose li) {
  @apply mb-1;
}
</style>
