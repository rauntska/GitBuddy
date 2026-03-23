<template>
  <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-7 gap-2 sm:gap-3 mb-4 sm:mb-6">
    <div
      v-for="(stat, key) in statsConfig"
      :key="key"
      class="flex items-center gap-2 sm:gap-3 p-2 sm:p-4 rounded-lg border border-slate-700/50 bg-slate-800/30"
    >
      <div
        class="flex-shrink-0 w-8 h-8 sm:w-10 sm:h-10 rounded-lg flex items-center justify-center"
        :class="stat.colorClass"
      >
        <svg class="w-4 h-4 sm:w-5 sm:h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="stat.icon" />
        </svg>
      </div>
      <div>
        <div class="text-xl sm:text-2xl font-semibold text-slate-100">{{ stats[key] }}</div>
        <div class="text-xs text-slate-500 hidden sm:block">{{ stat.label }}</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { PRStats } from '../types';

const props = defineProps<{
  stats: PRStats;
}>();

const statsConfig = computed(() => ({
  totalOpen: {
    label: 'Total Open',
    icon: 'M13 10V3L4 14h7v7l9-11h-7z',
    colorClass: 'bg-blue-500/20 text-blue-400',
  },
  awaitingReview: {
    label: 'Awaiting Review',
    icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
    colorClass: 'bg-yellow-500/20 text-yellow-400',
  },
  approved: {
    label: 'Approved',
    icon: 'M5 13l4 4L19 7',
    colorClass: 'bg-green-500/20 text-green-400',
  },
  draft: {
    label: 'Drafts',
    icon: 'M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z',
    colorClass: 'bg-gray-500/20 text-gray-400',
  },
  totalComments: {
    label: 'Total Comments',
    icon: 'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z',
    colorClass: 'bg-purple-500/20 text-purple-400',
  },
  resolvedComments: {
    label: 'Resolved',
    icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z',
    colorClass: 'bg-cyan-500/20 text-cyan-400',
  },
  pendingComments: {
    label: 'Pending',
    icon: 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z',
    colorClass: 'bg-orange-500/20 text-orange-400',
  },
}));
</script>
