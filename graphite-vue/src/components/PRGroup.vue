<template>
  <div class="mb-6">
    <div class="flex items-center justify-between mb-3">
      <h2 class="text-lg font-semibold text-slate-200 flex items-center gap-2">
        <span
          class="w-3 h-3 rounded-full"
          :class="[statusColor, { 'stale-pulse': isGroupStale }]"
        ></span>
        {{ title }}
        <span class="text-sm font-normal text-slate-500">({{ pullRequests.length }})</span>
      </h2>
       <button
         @click="$emit('toggle')"
         class="text-slate-500 hover:text-slate-300 transition-colors p-1"
         :title="isExpanded ? 'Collapse' : 'Expand'"
       >
         <svg
           class="w-5 h-5 transition-transform duration-200"
           :class="{ 'rotate-180': isExpanded }"
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
      class="collapse-wrapper"
      :class="{ collapsed: !isExpanded }"
    >
      <div class="collapse-inner">
        <div class="space-y-2">
          <TransitionGroup name="pr-list">
            <PRRow
              v-for="(pr, index) in pullRequests"
              :key="pr.gitHubId"
              :pr="pr"
              :compact="compact"
              :density="density"
              :style="index < 20 ? { animationDelay: `${index * 50}ms` } : undefined"
              @contextmenu="$emit('contextmenu', $event)"
            />
          </TransitionGroup>

          <!-- Contextual Empty State -->
          <div
            v-if="pullRequests.length === 0"
            class="text-center py-6"
          >
            <div class="flex flex-col items-center gap-2">
              <div class="text-slate-600" v-html="emptyStateIcon"></div>
              <p class="text-slate-500 text-sm">{{ emptyStateMessage }}</p>
            </div>
          </div>
        </div>
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
  compact?: boolean;
  density?: 'compact' | 'comfortable' | 'expanded';
}>();

 defineEmits<{
  toggle: [];
  contextmenu: [payload: { pr: any; x: number; y: number }];
 }>();

 const isExpanded = computed(() => {
   if (props.pullRequests.length === 0) {
     return false;
   }
   return props.expanded;
 });

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

const isGroupStale = computed(() => {
  if (props.status !== 'AwaitingReview') return false;
  const oneDayAgo = new Date(Date.now() - 24 * 60 * 60 * 1000);
  return props.pullRequests.some(pr => new Date(pr.createdAt) < oneDayAgo);
});

const emptyStateMessage = computed(() => {
  const messages: Record<string, string> = {
    AwaitingReview: 'All caught up — no PRs waiting for review',
    ChangesRequested: 'No PRs with changes requested',
    Approved: 'No approved PRs ready to merge',
    Reviewed: 'No reviewed PRs at the moment',
    Draft: 'No draft PRs',
    Merged: 'No merged or closed PRs',
  };
  return messages[props.status] || 'No pull requests in this category';
});

const emptyStateIcon = computed(() => {
  const icons: Record<string, string> = {
    AwaitingReview: '<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>',
    ChangesRequested: '<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>',
    Approved: '<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M5 13l4 4L19 7" /></svg>',
    Reviewed: '<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" /></svg>',
    Draft: '<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>',
    Merged: '<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M13 10V3L4 14h7v7l9-11h-7z" /></svg>',
  };
  return icons[props.status] || '';
});
</script>

<style scoped>
/* Grid-template-rows collapse — no max-height measurement needed */
.collapse-wrapper {
  display: grid;
  grid-template-rows: 1fr;
  transition: grid-template-rows 0.3s ease-out;
}

.collapse-wrapper.collapsed {
  grid-template-rows: 0fr;
}

.collapse-inner {
  overflow: hidden;
}

/* Staggered list entrance */
.pr-list-enter-active {
  animation: fadeSlideIn 0.3s ease-out both;
}

.pr-list-leave-active {
  transition: opacity 0.2s ease-in, transform 0.2s ease-in;
}

.pr-list-leave-to {
  opacity: 0;
  transform: translateX(-8px);
}

.pr-list-move {
  transition: transform 0.3s ease;
}

@keyframes fadeSlideIn {
  from {
    opacity: 0;
    transform: translateY(8px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
