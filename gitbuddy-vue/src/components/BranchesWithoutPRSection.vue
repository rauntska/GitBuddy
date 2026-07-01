<template>
  <div v-if="groupedBranches.length > 0 || loading" class="mb-6">
    <div class="flex items-center justify-between mb-3">
      <h2 class="text-lg font-semibold text-slate-200 flex items-center gap-2">
        <span class="w-3 h-3 rounded-full bg-amber-500"></span>
        Branches without PRs
        <span class="text-sm font-normal text-slate-500">({{ totalBranches }})</span>
      </h2>
      <div class="flex items-center gap-2">
        <button
          @click="$emit('refresh')"
          class="text-slate-500 hover:text-slate-300 transition-colors p-1"
          :disabled="refreshing"
          title="Refresh"
        >
          <svg class="w-4 h-4" :class="{ 'animate-spin': loading || refreshing }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
        </button>
        <button
          @click="isExpanded = !isExpanded"
          class="text-slate-500 hover:text-slate-300 transition-colors p-1"
          :title="isExpanded ? 'Collapse' : 'Expand'"
        >
          <svg
            class="w-5 h-5 transition-transform duration-200"
            :class="{ 'rotate-180': isExpanded }"
            fill="none" stroke="currentColor" viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
          </svg>
        </button>
      </div>
    </div>

    <Transition name="collapse">
      <div v-if="isExpanded" class="space-y-2">
        <template v-if="loading && groupedBranches.length === 0">
          <div class="flex items-center justify-center py-6">
            <svg class="w-5 h-5 animate-spin text-slate-400" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
            <span class="ml-2 text-sm text-slate-400">Scanning branches...</span>
          </div>
        </template>

        <template v-else-if="error">
          <div class="text-center py-4 text-red-400 text-sm">
            {{ error }}
          </div>
        </template>

        <template v-else>
          <template v-for="{ repo, branches } in groupedBranches" :key="repo">
            <div class="text-xs text-slate-500 mt-3 mb-1 px-1">{{ repo }}</div>
            <BranchWithoutPRRow
              v-for="branch in branches"
              :key="`${branch.repoFullName}-${branch.branchName}`"
              :branch="branch"
            />
          </template>
        </template>
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { BranchWithoutPR } from '../types';
import BranchWithoutPRRow from './BranchWithoutPRRow.vue';

defineProps<{
  groupedBranches: { repo: string; branches: BranchWithoutPR[] }[];
  totalBranches: number;
  loading: boolean;
  refreshing?: boolean;
  error: string | null;
}>();

defineEmits<{
  refresh: [];
}>();

const isExpanded = ref(true);
</script>

<style scoped>
.collapse-enter-active,
.collapse-leave-active {
  transition: all 0.2s ease;
  overflow: hidden;
}

.collapse-enter-from,
.collapse-leave-to {
  opacity: 0;
  max-height: 0;
}

.collapse-enter-to,
.collapse-leave-from {
  opacity: 1;
  max-height: 1000px;
}
</style>
