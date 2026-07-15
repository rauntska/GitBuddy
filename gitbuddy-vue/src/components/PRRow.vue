<template>
  <div
    :class="[
      'group relative flex border border-slate-800 rounded',
      getStatusTintClass(pr.status),
      'hover:bg-slate-800/40 hover:border-slate-700 hover:-translate-y-px',
      'transition-all duration-150 ease-out',
      getStatusBorderClass(pr.status),
      { 'opacity-70': isStale(pr.createdAt) },
      { 'activity-flash': flashActive },
      isExpanded ? 'py-3 px-4 gap-3 flex-col' : (compact ? 'py-1 px-2 gap-2 items-center' : 'py-2 px-3 gap-4 items-center')
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
        <div class="flex-shrink-0 w-[32px] h-[32px] rounded-full bg-slate-800 border border-slate-700 flex items-center justify-center text-xs text-slate-200">
          {{ pr.author?.substring(0, 2).toUpperCase() }}
        </div>
        <div class="flex-1 min-w-0">
          <div class="text-base text-slate-200 truncate">
            {{ pr.title }} <span class="font-mono text-sm">#{{ pr.gitHubId }}</span>
          </div>
          <div class="text-xs text-slate-200 mt-0.5 font-mono">{{ pr.repository }} <span>·</span> {{ pr.author }}</div>
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
      <div class="flex items-center gap-4 text-xs text-slate-200 pl-11 font-mono">
        <span class="flex items-center gap-1">
          <span class="text-emerald-400">+{{ pr.additions }}</span>
          <span>/</span>
          <span class="text-red-400">-{{ pr.deletions }}</span>
        </span>
        <span class="font-sans">{{ pr.changedFiles }} {{ pr.changedFiles === 1 ? 'file' : 'files' }}</span>
        <span v-if="pendingThreadsCount > 0" class="font-sans">{{ pendingThreadsCount }} threads</span>
        <ReviewerAvatars :reviews="pr.reviews.filter(r => r.reviewer !== pr.author)" :max-display="3" size="sm" />
        <span class="ml-auto">{{ formatRelativeTime(pr.updatedAt) }}</span>
        <span
          v-if="isStale(pr.createdAt)"
          class="flex items-center gap-1 text-amber-400"
          :title="`Stale: created ${formatAge(pr.createdAt)} ago`"
        >
          <span>⚑</span>
          <span class="font-sans">{{ formatAge(pr.createdAt) }}</span>
        </span>
      </div>
    </template>

    <!-- Compact / Comfortable mode: single-line layout -->
    <template v-else>
     <!-- Repository & PR Number -->
     <div class="flex-shrink-0" :class="compact ? 'w-[80px] sm:w-[100px]' : 'w-[100px] sm:w-[140px]'">
       <div :class="compact ? 'text-xs' : 'text-sm'" class="text-slate-200 truncate">{{ pr.repository }}</div>
       <div class="text-xs text-slate-200 font-mono">#{{ pr.gitHubId }}</div>
     </div>

     <!-- Author (hidden on small screens) -->
     <div class="hidden md:flex flex-shrink-0" :class="compact ? 'w-[80px]' : 'w-[120px]'">
       <div :class="compact ? 'text-xs' : 'text-sm'" class="text-slate-200 truncate">{{ pr.author }}</div>
     </div>

     <!-- PR Title (Flexible) -->
     <div class="flex-1 min-w-0">
       <div :class="compact ? 'text-xs' : 'text-sm'" class="text-slate-200 truncate">
         {{ pr.title }}
       </div>
     </div>

      <!-- Metadata Section -->
      <div class="flex items-center flex-shrink-0 flex-wrap justify-end gap-1 sm:gap-2 md:gap-3 font-mono">
      <!-- Priority Badge -->
      <div v-if="showPriority" :class="compact ? 'w-[24px]' : 'w-[32px]'" class="flex justify-center items-center gap-0.5">
        <span
          class="text-xs font-semibold"
          :class="getPriorityColor(pr.priority)"
          :title="`${getPriorityLabel(pr.priority)}${pr.priorityOverridden ? ' (manual override)' : ''}`"
        >{{ getPriorityGlyph(pr.priority) }}</span>
        <span v-if="pr.priorityOverridden" class="text-[8px] leading-none" :class="getPriorityColor(pr.priority)">•</span>
      </div>

      <!-- PR Size Badge -->
      <div :class="compact ? 'w-[36px]' : 'w-[44px]'" class="flex justify-center">
        <PRSizeBadge :additions="pr.additions" :deletions="pr.deletions" :compact="compact" />
      </div>

      <!-- CI/CD Status Glyph -->
      <div :class="compact ? 'w-[24px]' : 'w-[32px]'" class="flex justify-center">
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

      <!-- Reviewer Avatars (hidden on small screens) -->
      <div :class="compact ? 'w-[60px]' : 'w-[80px]'" class="hidden lg:flex justify-center">
        <ReviewerAvatars :reviews="pr.reviews.filter(r => r.reviewer !== pr.author)" :max-display="compact ? 2 : 3" :size="compact ? 'sm' : 'md'" />
      </div>

      <!-- Comments (Resolved/Total) -->
      <div :class="compact ? 'w-[36px]' : 'w-[44px]'" class="hidden md:flex justify-center">
        <span
          v-if="totalThreadsCount > 0"
          class="text-xs tabular-nums"
          :class="pendingThreadsCount > 0 ? 'text-orange-400' : 'text-slate-200'"
          :title="`${resolvedThreadsCount} resolved, ${pendingThreadsCount} pending`"
        >{{ resolvedThreadsCount }}/{{ totalThreadsCount }}</span>
      </div>

      <!-- Files Changed -->
      <div :class="compact ? 'w-[30px]' : 'w-[40px]'" class="hidden md:flex justify-center">
        <span
          class="text-xs tabular-nums text-slate-200"
          :title="`${pr.changedFiles} ${pr.changedFiles === 1 ? 'file' : 'files'} changed`"
        >{{ pr.changedFiles }}</span>
      </div>

      <!-- Line Changes -->
      <div :class="compact ? 'w-[70px]' : 'w-[90px]'" class="hidden sm:flex justify-end gap-1 text-xs">
          <span class="text-emerald-400">+{{ pr.additions }}</span>
          <span class="text-red-400">-{{ pr.deletions }}</span>
      </div>

      <!-- Stale glyph (compact) -->
      <div v-if="compact" class="hidden md:flex w-[16px] justify-center">
        <span
          v-if="isStale(pr.createdAt)"
          class="text-amber-400"
          :title="`Stale: created ${formatAge(pr.createdAt)} ago`"
        >⚑</span>
      </div>

      <!-- Last Updated Time -->
      <div :class="compact ? 'w-[36px]' : 'w-[44px]'" class="hidden md:block text-xs text-slate-200 text-right">
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
  getStatusTintClass,
  getPriorityColor,
  getPriorityGlyph,
  getPriorityLabel,
  PRIORITY_HIGH,
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

const showPriority = computed(() => (props.pr.priority ?? 1) >= PRIORITY_HIGH);

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
