<template>
  <div class="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
    <div
      v-for="(stat, key) in statsConfig"
      :key="key"
      class="flex items-center gap-3 p-4 rounded-lg border border-slate-700/50 bg-slate-800/30"
    >
      <div
        class="flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center"
        :class="stat.colorClass"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="stat.icon" />
        </svg>
      </div>
      <div>
        <div class="text-2xl font-semibold text-slate-100">{{ stats[key] }}</div>
        <div class="text-xs text-slate-500">{{ stat.label }}</div>
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
}));
</script>
