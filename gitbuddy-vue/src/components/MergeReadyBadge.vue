<template>
  <div
    :class="[
      'flex items-center gap-1 px-1.5 py-0.5 rounded text-xs font-medium',
      badgeClasses
    ]"
    :title="tooltip"
  >
    <svg v-if="isReady" class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
    </svg>
    <svg v-else-if="hasApprovals" class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
    </svg>
    <span>{{ displayText }}</span>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  isMergeReady?: boolean;
  requiredApprovingReviews?: number;
  currentApprovingReviews?: number;
  hasUnresolvedThreads?: boolean;
  mergeBlockReason?: string;
  compact?: boolean;
}>();

const isReady = computed(() => props.isMergeReady === true);
const hasApprovals = computed(() => props.requiredApprovingReviews !== undefined && props.requiredApprovingReviews > 0);

const badgeClasses = computed(() => {
  if (isReady.value) {
    return 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30';
  }
  if (hasApprovals.value) {
    return 'bg-amber-500/20 text-amber-400 border border-amber-500/30';
  }
  return 'bg-slate-700/30 text-slate-400';
});

const displayText = computed(() => {
  if (isReady.value) {
    return props.compact ? 'Ready' : 'Ready to merge';
  }
  if (hasApprovals.value) {
    const current = props.currentApprovingReviews ?? 0;
    const required = props.requiredApprovingReviews ?? 0;
    return `${current}/${required}`;
  }
  return 'Review';
});

const tooltip = computed(() => {
  if (isReady.value) {
    return 'All requirements met - ready to merge';
  }
  if (props.mergeBlockReason) {
    return props.mergeBlockReason;
  }
  if (hasApprovals.value) {
    const current = props.currentApprovingReviews ?? 0;
    const required = props.requiredApprovingReviews ?? 0;
    return `${current} of ${required} approvals received`;
  }
  return 'Needs review';
});
</script>
