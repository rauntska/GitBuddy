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

     <Transition name="collapse">
     <div
       v-if="isExpanded"
       class="space-y-2"
     >
      <PRRow
        v-for="pr in pullRequests"
        :key="pr.gitHubId"
        :pr="pr"
        :compact="compact"
      />

      <div
        v-if="pullRequests.length === 0"
        class="text-center py-2 text-slate-600 text-sm"
      >
        No pull requests in this category
      </div>
     </div>
    </Transition>
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
}>();

 defineEmits<{
  toggle: [];
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
</script>

<style scoped>
.collapse-enter-active,
.collapse-leave-active {
  transition: max-height 0.3s ease, opacity 0.3s ease;
  overflow: hidden;
}
.collapse-enter-from,
.collapse-leave-to {
  max-height: 0;
  opacity: 0;
}
.collapse-enter-to,
.collapse-leave-from {
  max-height: 2000px;
  opacity: 1;
}
</style>
