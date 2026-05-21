<template>
  <div
    :class="[
      'group relative flex border',
      'border-slate-700/50 bg-slate-800/50',
      'hover:bg-slate-800 hover:shadow-xl hover:-translate-y-px',
      'transition-all duration-200 ease-out',
      getStatusBorderClass(pr.status),
      getStatusShadowClass(pr.status),
      { 'opacity-75': isStale(pr.createdAt) },
      { 'activity-flash': flashActive },
      isExpanded ? 'rounded-lg p-3 gap-3 flex-col' : (compact ? 'rounded p-1.5 gap-2 items-center' : 'rounded-lg p-2 gap-4 items-center')
    ]"
    @contextmenu.prevent="onContextMenu"
  >
    <router-link
      :to="{ name: 'pr-detail', params: { id: pr.id } }"
      :aria-label="`PR #${pr.gitHubId} ${pr.title} by ${pr.author} in ${pr.repository} — ${pr.status}`"
      class="flex-1 min-w-0 flex cursor-pointer"
      :class="isExpanded ? 'flex-col gap-3' : 'items-center gap-4'"
    >
    <!-- Expanded mode: 2-line layout -->
    <template v-if="isExpanded">
      <!-- Row 1: Author + Title + Status -->
      <div class="flex items-center gap-3 min-w-0">
        <div class="flex-shrink-0 w-[32px] h-[32px] rounded-full bg-slate-700 flex items-center justify-center text-xs font-semibold text-slate-300">
          {{ pr.author?.substring(0, 2).toUpperCase() }}
        </div>
        <div class="flex-1 min-w-0">
          <div class="text-sm font-medium text-slate-200 truncate group-hover:text-white transition-colors">
            {{ pr.title }} <span class="text-slate-500 font-normal">#{{ pr.gitHubId }}</span>
          </div>
          <div class="text-xs text-slate-500 mt-0.5">{{ pr.repository }} &bull; by {{ pr.author }}</div>
        </div>
        <div class="flex items-center gap-2 flex-shrink-0">
          <PRSizeBadge :additions="pr.additions" :deletions="pr.deletions" :compact="false" />
          <CIBadge :status="pr.checksStatus" :compact="true" />
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
      </div>
      <!-- Row 2: Metadata bar -->
      <div class="flex items-center gap-4 text-xs text-slate-500 pl-11">
        <span class="flex items-center gap-1">
          <span class="text-green-400 font-mono">+{{ pr.additions }}</span>
          <span class="text-slate-600">/</span>
          <span class="text-red-400 font-mono">-{{ pr.deletions }}</span>
        </span>
        <span>{{ pr.changedFiles }} {{ pr.changedFiles === 1 ? 'file' : 'files' }}</span>
        <span v-if="pendingThreadsCount > 0">{{ pendingThreadsCount }} threads</span>
        <ReviewerAvatars :reviews="pr.reviews.filter(r => r.reviewer !== pr.author)" :max-display="3" size="sm" />
        <span class="ml-auto">{{ formatRelativeTime(pr.updatedAt) }}</span>
        <div
          v-if="isStale(pr.createdAt)"
          class="flex items-center gap-1 text-amber-400"
          :title="`Stale: created ${formatAge(pr.createdAt)} ago`"
        >
          <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          {{ formatAge(pr.createdAt) }}
        </div>
      </div>
    </template>

    <!-- Compact / Comfortable mode: existing single-line layout -->
    <template v-else>
     <!-- Repository & PR Number -->
     <div class="flex-shrink-0" :class="compact ? 'w-[80px] sm:w-[100px]' : 'w-[100px] sm:w-[140px]'">
       <div :class="compact ? 'text-xs' : 'text-sm'" class="font-medium text-slate-200 truncate">{{ pr.repository }}</div>
       <div class="text-xs text-slate-500">PR #{{ pr.gitHubId }}</div>
     </div>

     <!-- Author (hidden on small screens) -->
     <div class="hidden md:flex flex-shrink-0" :class="compact ? 'w-[80px]' : 'w-[120px]'">
       <div :class="compact ? 'text-xs' : 'text-sm'" class="text-slate-300 truncate">{{ pr.author }}</div>
     </div>

     <!-- PR Title (Flexible) -->
     <div class="flex-1 min-w-0">
       <div :class="compact ? 'text-xs' : 'text-sm'" class="text-slate-300 truncate group-hover:text-white transition-colors">
         {{ pr.title }}
       </div>
     </div>

      <!-- Metadata Section -->
      <div class="flex items-center flex-shrink-0 flex-wrap justify-end gap-1 sm:gap-2 md:gap-3">
      <!-- PR Size Badge -->
      <div :class="compact ? 'w-[50px]' : 'w-[60px]'" class="flex justify-center">
        <PRSizeBadge :additions="pr.additions" :deletions="pr.deletions" :compact="compact" />
      </div>

      <!-- CI/CD Status Badge -->
      <div :class="compact ? 'w-[30px]' : 'w-[40px]'" class="flex justify-center">
        <CIBadge
          :status="pr.checksStatus"
          :compact="true"
        />
      </div>

      <!-- Merge Ready Badge -->
      <div :class="compact ? 'w-[50px]' : 'w-[70px]'" class="flex justify-center">
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

      <!-- Stale Indicator / Spacer (hidden on small screens) -->
      <div :class="compact ? 'w-[30px]' : 'w-[45px]'" class="hidden md:flex justify-center">
        <div
          v-if="isStale(pr.createdAt)"
          :class="compact ? 'px-1 py-0.5' : 'px-1.5 py-0.5'"
          class="flex items-center gap-1 rounded bg-amber-500/10 border border-amber-500/30"
          :title="`Stale: created ${formatAge(pr.createdAt)} ago`"
        >
          <svg class="w-3 h-3 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <span v-if="!compact" class="text-xs text-amber-300 font-medium">{{ formatAge(pr.createdAt) }}</span>
        </div>
      </div>

      <!-- Reviewer Avatars (hidden on small screens) -->
      <div :class="compact ? 'w-[60px]' : 'w-[80px]'" class="hidden lg:flex justify-center">
        <ReviewerAvatars :reviews="pr.reviews.filter(r => r.reviewer !== pr.author)" :max-display="compact ? 2 : 3" :size="compact ? 'sm' : 'md'" />
      </div>

      <!-- Comments (Resolved/Total) (hidden on small screens) -->
      <div :class="compact ? 'w-[40px]' : 'w-[50px]'" class="hidden md:flex justify-center">
        <div
          v-if="pendingThreadsCount > 0"
          :class="compact ? 'px-1 py-0.5' : 'px-1.5 py-0.5'"
          class="flex items-center gap-1 rounded bg-slate-700/30 text-xs"
          :title="`${resolvedThreadsCount} resolved, ${pendingThreadsCount} pending`"
        >
          <svg class="w-3 h-3 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
          </svg>
          <span class="text-slate-300 font-medium">
            {{ resolvedThreadsCount }}/{{ totalThreadsCount }}
          </span>
        </div>
      </div>

      <!-- Files Changed (hidden on small screens) -->
      <div :class="compact ? 'w-[35px]' : 'w-[45px]'" class="hidden md:flex justify-center">
        <div
          :class="compact ? 'px-1 py-0.5' : 'px-1.5 py-0.5'"
          class="flex items-center gap-1 rounded bg-slate-700/30 text-xs text-slate-300"
          :title="`${pr.changedFiles} ${pr.changedFiles === 1 ? 'file' : 'files'} changed`"
        >
          <svg class="w-3 h-3 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
          </svg>
          <span class="font-medium">{{ pr.changedFiles }}</span>
        </div>
      </div>

      <!-- Line Changes (hidden on small screens) -->
      <div :class="compact ? 'w-[70px]' : 'w-[90px]'" class="hidden sm:flex justify-center">
        <div
          class="flex items-center gap-1 text-xs font-mono"
          title="Lines changed"
        >
          <span class="text-green-400 font-medium">+{{ pr.additions }}</span>
          <span class="text-slate-600">/</span>
          <span class="text-red-400 font-medium">-{{ pr.deletions }}</span>
        </div>
      </div>

      <!-- Last Updated Time (hidden on small screens) -->
      <div :class="compact ? 'w-[40px]' : 'w-[50px]'" class="hidden md:block text-xs text-slate-500 text-right">
        {{ formatRelativeTime(pr.updatedAt) }}
      </div>
    </div>
    </template>
    </router-link>

    <!-- Quick Actions "..." button -->
    <button
      class="absolute top-1 right-1 p-1 rounded opacity-0 group-hover:opacity-100 transition-opacity text-slate-500 hover:text-slate-300 hover:bg-slate-700/50"
      title="Actions"
      @click.stop="onActionsClick($event)"
    >
      <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
        <path d="M6 10a2 2 0 11-4 0 2 2 0 014 0zM12 10a2 2 0 11-4 0 2 2 0 014 0zM16 12a2 2 0 100-4 2 2 0 000 4z" />
      </svg>
    </button>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { PullRequest } from '../types';
import ReviewerAvatars from './ReviewerAvatars.vue';
import PRSizeBadge from './PRSizeBadge.vue';
import CIBadge from './CIBadge.vue';
import MergeReadyBadge from './MergeReadyBadge.vue';
import {
  isStale,
  formatAge,
  getStatusBorderClass,
  getStatusShadowClass,
} from '../utils/prHelpers';

const props = defineProps<{
  pr: PullRequest;
  compact?: boolean;
  density?: 'compact' | 'comfortable' | 'expanded';
}>();

const emit = defineEmits<{
  contextmenu: [event: { pr: PullRequest; x: number; y: number }];
}>();

const compact = computed(() => props.compact ?? false);
const isExpanded = computed(() => props.density === 'expanded');

const flashActive = ref(false);

watch(() => props.pr.updatedAt, () => {
  flashActive.value = true;
  setTimeout(() => { flashActive.value = false; }, 800);
});

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

const onContextMenu = (e: MouseEvent) => {
  emit('contextmenu', { pr: props.pr, x: e.clientX, y: e.clientY });
};

const onActionsClick = (e: MouseEvent) => {
  const rect = (e.currentTarget as HTMLElement).getBoundingClientRect();
  emit('contextmenu', { pr: props.pr, x: rect.left, y: rect.bottom + 4 });
};
</script>
