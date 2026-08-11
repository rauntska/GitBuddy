<template>
  <div class="bg-slate-950">
    <div v-if="loading" class="p-8 text-center">
      <div class="flex flex-col items-center gap-3">
        <svg class="animate-spin h-7 w-7 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135-5.824 3-7.938l3 2.647z"></path>
        </svg>
        <span class="text-sm text-slate-400">Rendering document…</span>
      </div>
    </div>

    <div v-else-if="fetchError" class="p-8 text-center">
      <div class="flex flex-col items-center gap-3">
        <svg class="w-12 h-12 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
        <p class="text-slate-400 text-sm">Couldn't load the document. Try the Source view.</p>
        <p class="text-slate-600 text-xs">{{ fetchError }}</p>
      </div>
    </div>

    <div v-else-if="!renderedHtml" class="p-8 text-center">
      <div class="flex flex-col items-center gap-3">
        <svg class="w-12 h-12 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
        <p class="text-slate-400 text-sm">No content to render</p>
      </div>
    </div>

    <div
      v-else
      class="md-diff-row"
    >
      <div class="md-diff-column">
        <div
          ref="contentRef"
          class="prose prose-invert max-w-none text-slate-300 markdown-diff-content"
          v-html="renderedHtml"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue';
import type { FileDiff } from '../types';
import apiClient from '../utils/api';
import { getDiffLineNumbers } from '../utils/markdownDiffReconstruct';
import { renderMarkdownWithAddedHighlights } from '../utils/renderMarkdownDiff';
import { renderMermaidBlocks } from '../composables/useMermaid';

const props = defineProps<{
  file: FileDiff;
  prId?: number;
}>();

const FULL_FILE_END_LINE = 1_000_000;

const loading = ref(false);
const fetchError = ref<string | null>(null);
const fullFileText = ref<string>('');
const contentRef = ref<HTMLElement | null>(null);

const diffLines = computed(() => getDiffLineNumbers(props.file));

const renderedHtml = computed(() => {
  if (!fullFileText.value) return '';
  return renderMarkdownWithAddedHighlights(fullFileText.value, diffLines.value.added);
});

watch(renderedHtml, async () => {
  if (!renderedHtml.value) return;
  await nextTick();
  if (contentRef.value) {
    void renderMermaidBlocks(contentRef.value);
  }
});

async function loadFullFile() {
  if (!props.prId || !props.file.path) {
    fetchError.value = 'Missing PR or file path.';
    return;
  }

  if (props.file.status === 'added' && !hasPatchLines()) {
    fullFileText.value = '';
    fetchError.value = null;
    return;
  }

  loading.value = true;
  fetchError.value = null;
  try {
    const params = new URLSearchParams();
    params.append('path', props.file.path);
    params.append('newStartLine', '1');
    params.append('newEndLine', FULL_FILE_END_LINE.toString());

    const response = await apiClient.get<{ newLines: { lineNumber: number; content: string }[] }>(
      `/pullrequests/${props.prId}/files/content?${params.toString()}`
    );

    const lines = response.data.newLines ?? [];
    fullFileText.value = lines.map((l) => l.content).join('\n');
  } catch (err) {
    fetchError.value = err instanceof Error ? err.message : 'Failed to fetch file content';
  } finally {
    loading.value = false;
  }
}

function hasPatchLines(): boolean {
  return diffLines.value.added.size > 0;
}

watch(
  () => [props.file.path, props.prId, props.file.patch] as const,
  () => { void loadFullFile(); },
  { immediate: true }
);
</script>

<style scoped>
.md-diff-row {
  display: flex;
  justify-content: center;
  width: 100%;
  background-color: rgb(2 6 23);
}

.md-diff-column {
  width: 100%;
  max-width: 100%;
  padding: 1.5rem;
}

@media (min-width: 1024px) {
  .md-diff-column {
    max-width: 64rem;
  }
}

@media (min-width: 1280px) {
  .md-diff-column {
    max-width: 72rem;
  }
}

@media (min-width: 1536px) {
  .md-diff-column {
    max-width: 90rem;
  }
}

.markdown-diff-content {
  max-width: none !important;
}

.markdown-diff-content :deep(.md-added-block) {
  background-color: rgb(16 185 129 / 0.1);
  box-shadow: inset 3px 0 0 0 rgb(16 185 129 / 0.7);
  border-radius: 0 0.25rem 0.25rem 0;
}

.markdown-diff-content :deep(.md-added-block > pre) {
  background-color: rgb(16 185 129 / 0.08);
}

.markdown-diff-content :deep(td.md-added-block),
.markdown-diff-content :deep(th.md-added-block) {
  background-color: rgb(16 185 129 / 0.12);
  box-shadow: none;
  border-radius: 0;
}

.markdown-diff-content :deep(pre) {
  overflow-x: auto;
  white-space: pre;
}

.markdown-diff-content :deep(code) {
  white-space: pre-wrap;
}

.markdown-diff-content :deep(table) {
  display: block;
  overflow-x: auto;
  white-space: nowrap;
}

.markdown-diff-content :deep(.mermaid-diagram) {
  display: flex;
  justify-content: center;
  overflow-x: auto;
  padding: 1rem;
  margin: 1rem 0;
  background-color: rgb(15 23 42 / 0.5);
  border: 1px solid rgb(51 65 85 / 0.5);
  border-radius: 0.5rem;
}

.markdown-diff-content :deep(.mermaid-diagram svg) {
  max-width: 100%;
  height: auto;
}
</style>
