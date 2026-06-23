<template>
  <div
    v-if="threadId"
    class="px-2.5 py-1.5 bg-slate-800/70 border-b border-slate-700/30 flex items-center justify-between"
  >
    <div class="flex items-center gap-1.5 flex-1 min-w-0">
      <button
        @click="$emit('toggle')"
        class="w-5 h-5 flex items-center justify-center text-slate-400 hover:text-slate-200 hover:bg-slate-700/50 rounded transition-all flex-shrink-0"
        title="Toggle"
        :aria-label="isExpanded ? 'Collapse' : 'Expand'"
        :aria-expanded="isExpanded"
      >
        <svg
          :class="['w-3 h-3 transition-transform duration-200', { 'rotate-90': !isExpanded }]"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
      </button>
      
      <div class="flex items-center gap-1 text-[11px]">
        <span class="font-semibold text-slate-200">{{ commentCount }}</span>
        <span class="text-slate-500">{{ commentCount === 1 ? 'comment' : 'comments' }}</span>
      </div>
      
      <span 
        v-if="lineNumber" 
        class="px-1 py-0.5 text-[9px] font-mono bg-slate-700/50 text-slate-400 rounded flex-shrink-0"
      >
        L{{ lineNumber }}
      </span>
      
      <template v-if="!isExpanded && lastCommentExcerpt">
        <span class="text-slate-600">·</span>
        <span class="text-[10px] text-slate-400 truncate max-w-[150px]">{{ lastCommentExcerpt }}</span>
      </template>
    </div>
    
    <div class="flex items-center gap-1.5 flex-shrink-0">
      <span
        v-if="isResolved"
        class="inline-flex items-center gap-0.5 px-1.5 py-0.5 text-[9px] font-semibold uppercase tracking-wide bg-emerald-500/10 text-emerald-400 rounded"
      >
        <svg class="w-2.5 h-2.5" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
        </svg>
        Resolved
      </span>
      
      <span
        v-if="isOutdated"
        class="inline-flex items-center gap-0.5 px-1.5 py-0.5 text-[9px] font-semibold uppercase tracking-wide bg-amber-500/10 text-amber-400 rounded"
      >
        <svg class="w-2.5 h-2.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        Outdated
      </span>
      
      <button
        v-if="threadGitId && !isResolved"
        @click="$emit('resolve', true)"
        :disabled="isResolving"
        class="inline-flex items-center gap-0.5 px-1.5 py-0.5 text-[9px] font-semibold text-slate-400 hover:text-emerald-400 hover:bg-emerald-500/10 rounded transition-all disabled:opacity-50"
        title="Resolve"
        aria-label="Mark as resolved"
      >
        <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
        </svg>
        Resolve
      </button>
      
      <button
        v-if="threadGitId && isResolved"
        @click="$emit('resolve', false)"
        :disabled="isResolving"
        class="inline-flex items-center gap-0.5 px-1.5 py-0.5 text-[9px] font-semibold text-emerald-400/70 hover:text-emerald-400 hover:bg-emerald-500/10 rounded transition-all disabled:opacity-50"
        title="Reopen"
        aria-label="Reopen thread"
      >
        <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
        </svg>
        Reopen
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
  lastCommentExcerpt?: string;
}>();

defineEmits<{
  toggle: [];
  resolve: [resolved: boolean];
}>();
</script>
