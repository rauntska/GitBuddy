<template>
  <div class="flex items-center -space-x-2">
    <div
      v-for="review in displayedReviews"
      :key="review.reviewer"
      class="relative group/avatar"
      @mouseenter="showTooltip($event, `${review.reviewer} — ${getReviewStateLabel(review.state)}`)"
      @mouseleave="hideTooltip"
      @mousemove="moveTooltip"
    >
      <img
        :src="review.reviewerAvatar"
        :alt="review.reviewer"
        :class="[avatarSize, 'rounded-full border-2 border-slate-800 ring-2', getReviewStateColor(review.state)]"
      />
      <div
        :class="[dotSize, 'rounded-full border-2 border-slate-800 absolute bottom-0 right-0', getReviewStateDotColor(review.state)]"
      />
    </div>
    <div
      v-if="remainingCount > 0"
      :class="[avatarSize, 'flex items-center justify-center rounded-full border-2 border-slate-800 bg-slate-700 text-slate-300 font-medium', compact ? 'text-[10px]' : 'text-xs']"
      @mouseenter="showTooltip($event, `${remainingCount} more reviewers`)"
      @mouseleave="hideTooltip"
      @mousemove="moveTooltip"
    >
      +{{ remainingCount }}
    </div>
    <Teleport to="body">
      <div
        v-if="tooltipVisible"
        class="fixed z-[9999] bg-slate-700 text-sm text-slate-200 rounded px-2 py-1 shadow-lg pointer-events-none whitespace-nowrap"
        :style="{ top: `${tooltipY}px`, left: `${tooltipX}px` }"
      >
        {{ tooltipText }}
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import type { Review } from '../types';

const props = defineProps<{
  reviews: Review[];
  maxDisplay?: number;
  size?: 'sm' | 'md';
}>();

const compact = computed(() => props.size === 'sm');

const maxDisplay = computed(() => props.maxDisplay || 4);
const avatarSize = computed(() => compact.value ? 'w-5 h-5' : 'w-7 h-7');
const dotSize = computed(() => compact.value ? 'w-2 h-2' : 'w-3 h-3');

const displayedReviews = computed(() => props.reviews.slice(0, maxDisplay.value));
const remainingCount = computed(() => Math.max(0, props.reviews.length - maxDisplay.value));

const tooltipVisible = ref(false);
const tooltipText = ref('');
const tooltipX = ref(0);
const tooltipY = ref(0);

const showTooltip = (event: MouseEvent, text: string) => {
  tooltipText.value = text;
  tooltipX.value = event.clientX;
  tooltipY.value = event.clientY - 36;
  tooltipVisible.value = true;
};

const moveTooltip = (event: MouseEvent) => {
  tooltipX.value = event.clientX;
  tooltipY.value = event.clientY - 36;
};

const hideTooltip = () => {
  tooltipVisible.value = false;
};

const getReviewStateColor = (state: string): string => {
  const colors: Record<string, string> = {
    Approved: 'ring-green-500',
    ChangesRequested: 'ring-orange-500',
    Commented: 'ring-blue-500',
    Dismissed: 'ring-gray-500',
  };
  return colors[state] || 'ring-gray-500';
};

const getReviewStateDotColor = (state: string): string => {
  const colors: Record<string, string> = {
    Approved: 'bg-green-500',
    ChangesRequested: 'bg-orange-500',
    Commented: 'bg-blue-500',
    Dismissed: 'bg-gray-500',
  };
  return colors[state] || 'bg-gray-500';
};

const getReviewStateLabel = (state: string): string => {
  const labels: Record<string, string> = {
    Approved: 'Approved',
    ChangesRequested: 'Changes Requested',
    Commented: 'Commented',
    Dismissed: 'Dismissed',
  };
  return labels[state] || state;
};
</script>
