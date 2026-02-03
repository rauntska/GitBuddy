<template>
  <div class="mb-6">
    <div class="flex items-center justify-between mb-3">
      <h2 class="text-lg font-semibold text-slate-200 flex items-center gap-2">
        <span
          class="w-3 h-3 rounded-full"
          :class="statusColor"
        ></span>
        {{ title }}
        <span class="text-sm font-normal text-slate-500">({{ pullRequests.length }})</span>
      </h2>
      <button
        @click="$emit('toggle')"
        class="text-slate-500 hover:text-slate-300 transition-colors p-1"
        :title="expanded ? 'Collapse' : 'Expand'"
      >
        <svg
          class="w-5 h-5 transition-transform duration-200"
          :class="{ 'rotate-180': expanded }"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M19 9l-7 7-7-7"
          />
        </svg>
      </button>
    </div>

    <div
      v-if="expanded"
      class="space-y-2 transition-all duration-200"
    >
      <PRRow
        v-for="pr in pullRequests"
        :key="pr.gitHubId"
        :pr="pr"
      />
      
      <div
        v-if="pullRequests.length === 0"
        class="text-center py-2 text-slate-600 text-sm"
      >
        No pull requests in this category
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { PullRequest } from '../types';
import PRRow from './PRRow.vue';

const props = defineProps<{
  title: string;
  pullRequests: PullRequest[];
  status: string;
  expanded?: boolean;
}>();

defineEmits<{
  toggle: [];
}>();

const statusColor = computed(() => {
  const colors: Record<string, string> = {
    AwaitingReview: 'bg-blue-500',
    Approved: 'bg-green-500',
    Reviewed: 'bg-purple-500',
    ChangesRequested: 'bg-orange-500',
    Draft: 'bg-gray-500',
    Merged: 'bg-slate-600',
  };
  return colors[props.status] || 'bg-gray-500';
});
</script>
