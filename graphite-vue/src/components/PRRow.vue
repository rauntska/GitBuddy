<template>
  <router-link
    :to="{ name: 'pr-detail', params: { id: pr.id } }"
    :class="[
      'group relative flex items-center gap-4 p-2 rounded-lg border cursor-pointer',
      'border-slate-700/50 bg-slate-800/50',
      'hover:bg-slate-800 hover:shadow-lg',
      'transition-all duration-200 ease-out',
      getStatusBorderClass(pr.status),
      getStatusShadowClass(pr.status),
      { 'opacity-75': isStale(pr.createdAt) }
    ]"
  >
    <!-- Status Badge -->
<!--    <div class="flex-shrink-0 w-[32px]">-->
<!--      <StatusBadge :status="pr.status" />-->
<!--    </div>-->

    <!-- Repository & PR Number -->
    <div class="flex-shrink-0 w-[140px]">
      <div class="text-sm font-medium text-slate-200 truncate">{{ pr.repository }}</div>
      <div class="text-xs text-slate-500">PR #{{ pr.gitHubId }}</div>
    </div>

    <!-- Author -->
    <div class="flex-shrink-0 w-[120px]">
      <div class="text-sm text-slate-300 truncate">{{ pr.author }}</div>
    </div>

    <!-- PR Title (Flexible) -->
    <div class="flex-1 min-w-0">
      <div class="text-sm text-slate-300 truncate group-hover:text-white transition-colors">
        {{ pr.title }}
      </div>
    </div>

     <!-- Metadata Section (Compact) -->
     <div class="flex items-center gap-3 flex-shrink-0 w-[550px] justify-end">
      <!-- PR Size Badge -->
      <div class="w-[60px] flex justify-center">
        <PRSizeBadge :additions="pr.additions" :deletions="pr.deletions" />
      </div>

      <!-- CI/CD Status Badge -->
      <div class="w-[40px] flex justify-center">
        <CIBadge 
          :status="pr.checksStatus" 
          :compact="true"
        />
      </div>

      <!-- Merge Ready Badge -->
      <div class="w-[70px] flex justify-center">
        <MergeReadyBadge
          v-if="!pr.draft && !pr.isMerged && pr.status !== 'Merged' && pr.status !== 'Closed'"
          :is-merge-ready="pr.isMergeReady"
          :required-approving-reviews="pr.requiredApprovingReviews"
          :current-approving-reviews="pr.currentApprovingReviews"
          :has-unresolved-threads="pr.hasUnresolvedThreads"
          :merge-block-reason="pr.mergeBlockReason"
          :compact="true"
        />
      </div>

      <!-- Stale Indicator / Spacer -->
      <div class="w-[50px] flex justify-center">
        <div
          v-if="isStale(pr.createdAt)"
          class="flex items-center gap-1 text-xs text-red-400"
          :title="`Created ${formatAge(pr.createdAt)} ago`"
        >
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                  d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          <span class="font-medium">{{ formatAge(pr.createdAt) }}</span>
        </div>
      </div>

      <!-- Reviewer Avatars -->
      <div class="w-[80px] flex justify-center">
        <ReviewerAvatars :reviews="pr.reviews" :max-display="3" />
      </div>

      <!-- Comments (Resolved/Total) -->
      <div class="w-[50px] flex justify-center">
        <div
          v-if="pendingThreadsCount > 0"
          class="flex items-center gap-1 px-1.5 py-0.5 rounded bg-slate-700/30 text-xs"
          :title="`${resolvedThreadsCount} resolved, ${pendingThreadsCount} pending`"
        >
          <svg class="w-3.5 h-3.5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                  d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
          </svg>
          <span class="text-slate-300 font-medium">
            {{ resolvedThreadsCount }}/{{ totalThreadsCount }}
          </span>
        </div>
      </div>

      <!-- Files Changed -->
      <div class="w-[45px] flex justify-center">
        <div
          class="flex items-center gap-1 px-1.5 py-0.5 rounded bg-slate-700/30 text-xs text-slate-300"
          :title="`${pr.changedFiles} ${pr.changedFiles === 1 ? 'file' : 'files'} changed`"
        >
          <svg class="w-3.5 h-3.5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                  d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
          </svg>
          <span class="font-medium">{{ pr.changedFiles }}</span>
        </div>
      </div>

      <!-- Line Changes (Compact) -->
      <div class="w-[90px] flex justify-center">
        <div
          class="flex items-center gap-1 text-xs font-mono"
          title="Lines changed"
        >
          <span class="text-green-400 font-medium">+{{ pr.additions }}</span>
          <span class="text-slate-600">/</span>
          <span class="text-red-400 font-medium">-{{ pr.deletions }}</span>
        </div>
      </div>

      <!-- Last Updated Time -->
      <div class="w-[50px] text-xs text-slate-500 text-right">
        {{ formatRelativeTime(pr.updatedAt) }}
      </div>
    </div>

    <!-- Hover Details (Expandable) -->
    <div
      class="absolute left-0 right-0 bottom-0 translate-y-full opacity-0 group-hover:opacity-100
             transition-opacity duration-200 pointer-events-none z-10"
    >
      <div class="mt-1 px-4 py-2 rounded-lg bg-slate-800 border border-slate-700 shadow-xl text-xs text-slate-400">
        <div class="flex items-center justify-between gap-4">
          <span>Created: {{ formatDate(pr.createdAt) }}</span>
          <span>Last synced: {{ formatRelativeTime(pr.lastSyncedAt) }}</span>
          <span class="text-slate-500">Click to view details →</span>
        </div>
      </div>
    </div>
  </router-link>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { PullRequest } from '../types';
import ReviewerAvatars from './ReviewerAvatars.vue';
import PRSizeBadge from './PRSizeBadge.vue';
import CIBadge from './CIBadge.vue';
import MergeReadyBadge from './MergeReadyBadge.vue';
import {
  isStale,
  formatAge,
  formatDate,
  getStatusBorderClass,
  getStatusShadowClass,
} from '../utils/prHelpers';

const props = defineProps<{
  pr: PullRequest;
}>();

const totalThreadsCount = computed(() => props.pr.reviewThreads?.length || 0);
const resolvedThreadsCount = computed(() => props.pr.reviewThreads?.filter(rt => rt.isResolved).length || 0);
const pendingThreadsCount = computed(() => props.pr.reviewThreads?.filter(rt => !rt.isResolved).length || 0);

const formatRelativeTime = (dateString: string): string => {
  const date = new Date(dateString);
  const now = new Date();
  const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (seconds < 60) return 'now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h`;
  const days = Math.floor(hours / 24);
  return `${days}d`;
};
</script>
