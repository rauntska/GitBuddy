<template>
  <div class="prose prose-invert prose-sm max-w-none text-sm text-slate-300 leading-relaxed max-h-96 overflow-y-auto markdown-content">
    <div v-if="loading" class="flex items-center justify-center py-8">
      <svg class="animate-spin h-8 w-8 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0-3.042 1.135 5.824 3 7.938l3-2.647z"></path>
      </svg>
      <span class="ml-2 text-sm text-slate-400">Loading images...</span>
    </div>
    <div v-else v-html="html" />
  </div>
</template>

<script setup lang="ts">
import { toRef } from 'vue';
import { useProxiedHtml } from '../composables/useProxiedHtml';

const props = defineProps<{
  content: string;
}>();

const { html, loading } = useProxiedHtml(toRef(props, 'content'));
</script>
