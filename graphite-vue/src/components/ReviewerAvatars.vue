<template>
  <div class="flex items-center -space-x-2">
    <div
      v-for="review in displayedReviews"
      :key="review.reviewer"
      class="relative group"
    >
      <img
        :src="review.reviewerAvatar"
        :alt="review.reviewer"
        :title="`${review.reviewer} - ${getReviewStateLabel(review.state)}`"
        class="w-7 h-7 rounded-full border-2 border-slate-800 ring-2"
        :class="getReviewStateColor(review.state)"
      />
      <div
        class="absolute bottom-0 right-0 w-3 h-3 rounded-full border-2 border-slate-800"
        :class="getReviewStateDotColor(review.state)"
      />
    </div>
    <div
      v-if="remainingCount > 0"
      class="flex items-center justify-center w-7 h-7 rounded-full border-2 border-slate-800 bg-slate-700 text-xs text-slate-300 font-medium"
      :title="`${remainingCount} more reviewers`"
    >
      +{{ remainingCount }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { Review } from '../types';

const props = defineProps<{
  reviews: Review[];
  maxDisplay?: number;
}>();

const maxDisplay = computed(() => props.maxDisplay || 4);

const displayedReviews = computed(() => props.reviews.slice(0, maxDisplay.value));
const remainingCount = computed(() => Math.max(0, props.reviews.length - maxDisplay.value));

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
