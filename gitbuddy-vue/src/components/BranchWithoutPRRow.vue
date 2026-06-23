<template>
  <button
    @click="handleClick"
    class="group w-full flex items-center border border-slate-700/50 bg-slate-800/50 hover:bg-slate-800 hover:shadow-lg hover:border-amber-500/30 transition-all duration-200 ease-out rounded-lg p-2 gap-4 cursor-pointer text-left"
  >
    <div class="flex-shrink-0 w-8 h-8 rounded-lg bg-amber-500/20 flex items-center justify-center">
      <svg class="w-4 h-4 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
      </svg>
    </div>

    <div class="flex-shrink-0 w-[140px] sm:w-[200px]">
      <div class="text-sm font-medium text-slate-200 truncate">{{ branch.repoFullName }}</div>
    </div>

    <div class="flex-1 min-w-0">
      <div class="text-sm text-amber-300 truncate font-mono">{{ branch.branchName }}</div>
      <div class="text-xs text-slate-500">
        into <span class="text-slate-400">{{ branch.defaultBranch }}</span>
        <span v-if="branch.lastActivityAt" class="text-slate-600 ml-2">{{ formatRelativeTime(branch.lastActivityAt) }}</span>
      </div>
    </div>

    <div class="flex-shrink-0 opacity-0 group-hover:opacity-100 transition-opacity">
      <span class="text-xs text-green-400 flex items-center gap-1">
        Create PR
        <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
      </span>
    </div>
  </button>
</template>

<script setup lang="ts">
import type { BranchWithoutPR } from '../types';
import { useCreatePRModal } from '../composables/useCreatePRModal';

const props = defineProps<{
  branch: BranchWithoutPR;
}>();

const { openWithPrefill } = useCreatePRModal();

const handleClick = () => {
  openWithPrefill(props.branch);
};

const formatRelativeTime = (dateStr: string): string => {
  const date = new Date(dateStr);
  const now = new Date();
  const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);
  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
};
</script>
