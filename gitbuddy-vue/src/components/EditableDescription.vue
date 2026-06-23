<template>
  <div class="editable-description group relative">
    <div
      v-if="!isEditing"
      class="relative"
    >
      <div
        class="prose prose-invert prose-sm max-w-none text-sm text-slate-200 leading-relaxed markdown-content min-h-[60px]"
        :class="{ 'max-h-96 overflow-y-auto': !expanded }"
      >
        <div v-if="loading" class="flex items-center justify-center py-8">
          <svg class="animate-spin h-6 w-6 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0-3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
        </div>
        <div v-else-if="processedHtml" v-html="processedHtml" />
        <p v-else class="text-slate-500 italic">No description added</p>
      </div>

      <div class="absolute top-0 right-0 opacity-0 group-hover:opacity-100 transition-opacity duration-200">
        <div class="flex items-center gap-1 bg-slate-900/95 border border-slate-800 rounded p-1 shadow-lg">
          <button
            @click.stop="startEditing"
            class="p-1.5 hover:bg-slate-800 rounded text-slate-300 hover:text-slate-100 transition-colors"
            title="Edit description"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button
            v-if="!expanded && (content?.length || 0) > 200"
            @click.stop="expanded = true"
            class="p-1.5 hover:bg-slate-800 rounded text-slate-300 hover:text-slate-100 transition-colors"
            title="Expand"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4" />
            </svg>
          </button>
          <button
            v-if="expanded"
            @click.stop="expanded = false"
            class="p-1.5 hover:bg-slate-800 rounded text-slate-300 hover:text-slate-100 transition-colors"
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
        <div class="bg-slate-900 border border-slate-800 rounded shadow-2xl overflow-hidden">
          <TiptapEditor
            :model-value="editContent"
            @update:model-value="editContent = $event"
            placeholder="Write your description here..."
            :min-height="256"
            :pr-id="prId"
            @save="saveChanges"
            @cancel="cancelEditing"
          >
            <template #toolbar-actions>
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
                class="px-3 py-1.5 text-xs text-slate-300 hover:text-slate-100 hover:bg-slate-800 rounded transition-colors disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                @click="saveChanges"
                :disabled="saving"
                class="px-3 py-1.5 text-xs bg-slate-200 hover:bg-white text-slate-900 rounded transition-colors disabled:opacity-50"
              >
                Save
              </button>
            </template>
          </TiptapEditor>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { ref, toRef } from 'vue';
import { useProxiedHtml } from '../composables/useProxiedHtml';
import TiptapEditor from './TiptapEditor.vue';

const props = defineProps<{
  content: string;
  prId?: number;
}>();

const emit = defineEmits<{
  save: [content: string];
}>();

const isEditing = ref(false);
const editContent = ref('');
const saving = ref(false);
const expanded = ref(false);

const { html: processedHtml, loading } = useProxiedHtml(toRef(props, 'content'), { isMarkdown: true });

const startEditing = () => {
  editContent.value = props.content || '';
  isEditing.value = true;
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
</script>

<style scoped>
.editable-description :deep(.prose) {
  @apply text-slate-200;
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
  @apply text-slate-100 hover:text-white underline underline-offset-2;
}

.editable-description :deep(.prose blockquote) {
  @apply border-l-4 border-slate-700 pl-4 italic text-slate-400;
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

.editable-description :deep(.prose img) {
  @apply max-w-full rounded-lg my-2;
}
</style>
