<template>
  <div class="editable-description group relative">
    <div 
      v-if="!isEditing" 
      class="relative"
    >
      <div 
        class="prose prose-invert prose-sm max-w-none text-sm text-slate-300 leading-relaxed markdown-content min-h-[60px]"
        :class="{ 'max-h-96 overflow-y-auto': !expanded }"
      >
        <div v-if="loading" class="flex items-center justify-center py-8">
          <svg class="animate-spin h-6 w-6 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0-3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
        </div>
        <div v-else-if="processedHtml" v-html="processedHtml" />
        <p v-else class="text-slate-500 italic">No description added</p>
      </div>
      
      <div class="absolute top-0 right-0 opacity-0 group-hover:opacity-100 transition-opacity duration-200">
        <div class="flex items-center gap-1 bg-slate-800/90 backdrop-blur-sm border border-slate-600/50 rounded-lg p-1 shadow-lg">
          <button 
            @click.stop="startEditing"
            class="p-1.5 hover:bg-slate-700 rounded text-slate-300 hover:text-white transition-colors"
            title="Edit description"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button 
            v-if="!expanded && (content?.length || 0) > 200"
            @click.stop="expanded = true"
            class="p-1.5 hover:bg-slate-700 rounded text-slate-300 hover:text-white transition-colors"
            title="Expand"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4" />
            </svg>
          </button>
          <button 
            v-if="expanded"
            @click.stop="expanded = false"
            class="p-1.5 hover:bg-slate-700 rounded text-slate-300 hover:text-white transition-colors"
            title="Collapse"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 9V4.5M9 9H4.5M9 9L3.75 3.75M9 15v4.5M9 15H4.5M9 15l-5.25 5.25M15 9h4.5M15 9V4.5M15 9l5.25-5.25M15 15h4.5M15 15v4.5m0-4.5l5.25 5.25" />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <Transition
      enter-active-class="transition-opacity duration-200"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-opacity duration-150"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div v-if="isEditing" class="editor-overlay">
        <div class="bg-slate-900 border border-slate-600 rounded-xl shadow-2xl overflow-hidden">
          <div class="flex items-center gap-1 p-2 bg-slate-800 border-b border-slate-700">
            <button @click="insertFormat('bold')" class="toolbar-btn" title="Bold (Ctrl+B)">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 4h8a4 4 0 014 4 4 4 0 01-4 4H6z"></path>
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 12h9a4 4 0 014 4 4 4 0 01-4 4H6z"></path>
              </svg>
            </button>
            <button @click="insertFormat('italic')" class="toolbar-btn" title="Italic (Ctrl+I)">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 4h4m-2 0v16m-4 0h8"></path>
              </svg>
            </button>
            <button @click="insertFormat('code')" class="toolbar-btn" title="Inline Code">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4"></path>
              </svg>
            </button>
            <div class="w-px h-5 bg-slate-600 mx-1"></div>
            <button @click="insertFormat('link')" class="toolbar-btn" title="Link">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"></path>
              </svg>
            </button>
            <button @click="insertFormat('quote')" class="toolbar-btn" title="Quote">
              <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                <path d="M14.017 21v-7.391c0-5.704 3.731-9.57 8.983-10.609l.995 2.151c-2.432.917-3.995 3.638-3.995 5.849h4v10h-9.983zm-14.017 0v-7.391c0-5.704 3.748-9.57 9-10.609l.996 2.151c-2.433.917-3.996 3.638-3.996 5.849h3.983v10h-9.983z"></path>
              </svg>
            </button>
            <button @click="insertFormat('codeBlock')" class="toolbar-btn" title="Code Block">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16"></path>
              </svg>
            </button>
            <div class="w-px h-5 bg-slate-600 mx-1"></div>
            <button @click="insertFormat('ul')" class="toolbar-btn" title="Bullet List">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
              </svg>
            </button>
            <button @click="insertFormat('ol')" class="toolbar-btn" title="Numbered List">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
              </svg>
            </button>
            <div class="flex-1"></div>
            <span v-if="saving" class="flex items-center gap-2 text-xs text-slate-400 mr-2">
              <svg class="animate-spin h-3.5 w-3.5" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Saving...
            </span>
            <button 
              @click="cancelEditing" 
              :disabled="saving"
              class="px-3 py-1.5 text-xs font-medium text-slate-300 hover:text-white hover:bg-slate-700 rounded transition-colors disabled:opacity-50"
            >
              Cancel
            </button>
            <button 
              @click="saveChanges" 
              :disabled="saving"
              class="px-3 py-1.5 text-xs font-medium bg-blue-600 hover:bg-blue-500 text-white rounded transition-colors disabled:opacity-50"
            >
              Save
            </button>
          </div>
          
          <div class="grid grid-cols-2 divide-x divide-slate-700">
            <div class="relative">
              <div class="absolute top-2 left-3 text-xs text-slate-500 font-medium uppercase tracking-wide">Write</div>
              <textarea
                ref="textareaRef"
                v-model="editContent"
                @keydown="handleKeydown"
                placeholder="Write your description here..."
                class="w-full h-64 p-3 pt-8 bg-slate-900 text-slate-100 font-mono text-sm border-0 focus:ring-0 focus:outline-none resize-none"
              ></textarea>
            </div>
            <div class="relative bg-slate-900/50">
              <div class="absolute top-2 left-3 text-xs text-slate-500 font-medium uppercase tracking-wide">Preview</div>
              <div class="h-64 p-3 pt-8 overflow-y-auto prose prose-invert prose-sm max-w-none text-slate-300">
                <div v-if="editContent" v-html="previewHtml" class="markdown-content" />
                <p v-else class="text-slate-500 italic">Nothing to preview</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed, nextTick } from 'vue';
import { marked } from 'marked';
import apiClient from '../utils/api';

const props = defineProps<{
  content: string;
}>();

const emit = defineEmits<{
  save: [content: string];
}>();

const textareaRef = ref<HTMLTextAreaElement | null>(null);
const isEditing = ref(false);
const editContent = ref('');
const saving = ref(false);
const loading = ref(false);
const expanded = ref(false);
const processedHtml = ref('');
const processedImages = ref<Map<string, string>>(new Map());

const previewHtml = computed(() => {
  if (!editContent.value) return '';
  marked.setOptions({ breaks: true, gfm: true });
  return marked.parse(editContent.value) as string;
});

const processImages = async () => {
  const content = props.content || '';
  if (!content) {
    processedHtml.value = '';
    return;
  }

  loading.value = true;

  try {
    console.log("here")
    const envUrl = import.meta.env.VITE_API_BASE_URL;
    const apiBaseUrl = typeof envUrl === 'string' ? envUrl : 'http://localhost:5248/api';
    const proxyUrl = `${apiBaseUrl}/images/proxy`;

    const html = content.replace(/\r\n/g, '\n');

    const imgRegex = /<img[^>]*src="(https:\/\/github\.com\/user-attachments\/[^"]*)"[^>]*>/g;
    const imageUrls: string[] = [];
    let match: RegExpExecArray | null;

    while ((match = imgRegex.exec(html)) !== null) {
      if (match[1]) {
        imageUrls.push(match[1]);
      }
    }

    for (const imageUrl of imageUrls) {
      try {
        const response = await apiClient.get(`${proxyUrl}?url=${encodeURIComponent(imageUrl)}`, {
          responseType: 'blob',
        });

        const blob = response.data;
        const objectUrl = URL.createObjectURL(blob);
        processedImages.value.set(imageUrl, objectUrl);
      } catch (error: any) {
        console.error('Failed to load image:', imageUrl, error);
      }
    }

    let newHtml = html;
    processedImages.value.forEach((blobUrl, originalUrl) => {
      newHtml = newHtml.replace(originalUrl, blobUrl);
    });

    marked.setOptions({ breaks: true, gfm: true });
    processedHtml.value = marked.parse(newHtml) as string;
  } catch (error) {
    console.error('Error processing images:', error);
    marked.setOptions({ breaks: true, gfm: true });
    processedHtml.value = marked.parse(content) as string;
  } finally {
    loading.value = false;
  }
};

const startEditing = () => {
  editContent.value = props.content || '';
  isEditing.value = true;
  nextTick(() => {
    textareaRef.value?.focus();
  });
};

const cancelEditing = () => {
  isEditing.value = false;
  editContent.value = '';
};

const saveChanges = async () => {
  saving.value = true;
  try {
    emit('save', editContent.value);
    isEditing.value = false;
  } finally {
    saving.value = false;
  }
};

const handleKeydown = (event: KeyboardEvent) => {
  if (event.key === 'Tab') {
    event.preventDefault();
    const textarea = textareaRef.value;
    if (!textarea) return;

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const value = textarea.value;

    if (event.shiftKey) {
      const lineStart = value.lastIndexOf('\n', start - 1) + 1;
      if (value.substring(lineStart, lineStart + 2) === '  ') {
        const newValue = value.substring(0, lineStart) + value.substring(lineStart + 2);
        editContent.value = newValue;
        nextTick(() => {
          textarea.selectionStart = textarea.selectionEnd = start - 2;
        });
      }
    } else {
      const newValue = value.substring(0, start) + '  ' + value.substring(end);
      editContent.value = newValue;
      nextTick(() => {
        textarea.selectionStart = textarea.selectionEnd = start + 2;
      });
    }
  }

  if (event.key === 'Escape') {
    cancelEditing();
  }

  if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
    event.preventDefault();
    saveChanges();
  }
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

  const value = textarea.value;
  editContent.value = value.substring(0, start) + newText + value.substring(end);
  
  nextTick(() => {
    textarea.focus();
    const cursorPos = start + newText.length;
    textarea.selectionStart = textarea.selectionEnd = cursorPos;
  });
};

watch(() => props.content, () => {
  processImages();
}, { immediate: true });
</script>

<style scoped>
.editable-description :deep(.prose) {
  @apply text-slate-300;
}

.editable-description :deep(.prose h1),
.editable-description :deep(.prose h2),
.editable-description :deep(.prose h3) {
  @apply text-slate-100 font-semibold mt-4 mb-2;
}

.editable-description :deep(.prose h1) {
  @apply text-xl;
}

.editable-description :deep(.prose h2) {
  @apply text-lg;
}

.editable-description :deep(.prose h3) {
  @apply text-base;
}

.editable-description :deep(.prose p) {
  @apply mb-2;
}

.editable-description :deep(.prose code) {
  @apply bg-slate-800 px-1 py-0.5 rounded text-pink-400 text-sm;
}

.editable-description :deep(.prose pre) {
  @apply bg-slate-800 p-3 rounded-lg overflow-x-auto my-2;
}

.editable-description :deep(.prose pre code) {
  @apply bg-transparent p-0;
}

.editable-description :deep(.prose a) {
  @apply text-blue-400 hover:text-blue-300 underline;
}

.editable-description :deep(.prose blockquote) {
  @apply border-l-4 border-blue-500 pl-4 italic text-slate-400;
}

.editable-description :deep(.prose ul),
.editable-description :deep(.prose ol) {
  @apply pl-4 mb-2;
}

.editable-description :deep(.prose ul) {
  @apply list-disc;
}

.editable-description :deep(.prose ol) {
  @apply list-decimal;
}

.editable-description :deep(.prose li) {
  @apply mb-1;
}

.toolbar-btn {
  @apply p-1.5 rounded hover:bg-slate-700 text-slate-300 hover:text-white transition-colors;
}
</style>
