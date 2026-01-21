<template>
  <span
    :class="[
      'inline-flex items-center gap-1.5 px-2 py-1 rounded-full text-xs font-medium',
      statusClass
    ]"
  >
    <svg class="w-3.5 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="statusIcon" />
    </svg>
    {{ label }}
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  status: string;
}>();

const statusConfig: Record<string, { label: string; class: string; icon: string }> = {
  AwaitingReview: { label: 'Awaiting Review', class: 'bg-blue-500/20 text-blue-400', icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z' },
  Approved: { label: 'Approved', class: 'bg-green-500/20 text-green-400', icon: 'M5 13l4 4L19 7' },
  Reviewed: { label: 'Reviewed', class: 'bg-purple-500/20 text-purple-400', icon: 'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z' },
  ChangesRequested: { label: 'Changes Requested', class: 'bg-orange-500/20 text-orange-400', icon: 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z' },
  Draft: { label: 'Draft', class: 'bg-gray-500/20 text-gray-400', icon: 'M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z' },
};

const label = computed(() => statusConfig[props.status]?.label || props.status);
const statusClass = computed(() => statusConfig[props.status]?.class || 'bg-gray-500/20 text-gray-400');
const statusIcon = computed(() => statusConfig[props.status]?.icon || 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z');
</script>
