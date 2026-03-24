<template>
  <div
    v-if="threadId"
    class="px-4 py-3 bg-slate-800/60 border-b border-slate-700/30 flex items-center justify-between"
  >
    <div class="flex items-center gap-2">
      <button
        @click="$emit('toggle')"
        class="text-slate-400 hover:text-slate-200 transition-colors p-1 -ml-2"
        title="Toggle thread"
      >
        <svg
          :class="['w-4 h-4 transition-transform', { 'rotate-90': !isExpanded }]"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
      </button>
      <span class="text-xs font-medium text-slate-300">{{ commentCount }} comment{{ commentCount > 1 ? 's' : '' }}</span>
      <span v-if="lineNumber" class="text-xs text-slate-500">Line {{ lineNumber }}</span>
    </div>
    <div class="flex items-center gap-2">
      <span
        v-if="isOutdated"
        class="px-2 py-0.5 text-xs bg-amber-900/30 text-amber-400 rounded-full border border-amber-700/50"
      >
        Outdated
      </span>
      <button
        v-if="threadGitId"
        @click="$emit('resolve', !isResolved)"
        :disabled="isResolving"
        class="flex items-center gap-1 px-2 py-0.5 text-xs rounded-full border transition-all disabled:opacity-50"
        :class="[
          isResolved
            ? 'bg-emerald-900/30 text-emerald-400 border-emerald-700/50'
            : 'bg-slate-700/30 text-slate-400 border-slate-600/50 hover:bg-slate-600/30'
        ]"
        :title="isResolved ? 'Mark as unresolved' : 'Mark as resolved'"
      >
        <svg v-if="isResolved" class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L14.586 9H4a1 1 0 110-2h10.586l-4.293 4.293z" clip-rule="evenodd" />
        </svg>
        <svg v-else class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
        </svg>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  threadId: number | null;
  threadGitId?: string;
  commentCount: number;
  isExpanded: boolean;
  isResolved: boolean;
  isOutdated: boolean;
  isResolving: boolean;
  lineNumber?: number;
}>();

defineEmits<{
  toggle: [];
  resolve: [resolved: boolean];
}>();
</script>
