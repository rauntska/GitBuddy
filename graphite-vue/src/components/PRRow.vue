<template>
  <a
    :href="pr.url"
    target="_blank"
    rel="noopener noreferrer"
    class="group flex items-center gap-3 p-3 rounded-lg border border-slate-700/50 bg-slate-800/50 hover:bg-slate-800 transition-all duration-150 cursor-pointer"
  >
    <StatusBadge :status="pr.status" />

    <div class="flex-shrink-0 min-w-[120px]">
      <div class="text-sm font-medium text-slate-200 truncate">{{ pr.repository }}</div>
      <div class="text-xs text-slate-500">PR #{{ pr.gitHubId }}</div>
    </div>

    <div class="flex-shrink-0 min-w-[100px]">
      <div class="text-sm text-slate-300 truncate">{{ pr.author }}</div>
    </div>

    <div class="flex-1 min-w-0">
      <div class="text-sm text-slate-300 truncate group-hover:text-white transition-colors">
        {{ pr.title }}
      </div>
    </div>

    <div class="flex items-center gap-3 flex-shrink-0">
      <ReviewerAvatars :reviews="pr.reviews" :max-display="4" />
    </div>

    <div class="relative flex-shrink-0" v-if="pr.comment?.pendingCount && pr.comment.pendingCount > 0">
      <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
      </svg>
      <span class="absolute -top-1.5 -right-1.5 min-w-[16px] h-4 px-0.5 flex items-center justify-center text-[10px] font-medium rounded-full bg-orange-500 text-white">
        {{ pr.comment.pendingCount }}
      </span>
    </div>

    <div
      class="flex items-center gap-1.5 px-2 py-1 rounded text-xs font-mono"
      title="Lines changed"
    >
      <span class="text-green-400">+{{ pr.additions }}</span>
      <span class="text-red-400">-{{ pr.deletions }}</span>
    </div>

    <div class="text-xs text-slate-500 flex-shrink-0 min-w-[50px] text-right">
      {{ formatRelativeTime(pr.updatedAt) }}
    </div>
  </a>
</template>

<script setup lang="ts">

import type { PullRequest } from '../types';
import StatusBadge from './StatusBadge.vue';
import ReviewerAvatars from './ReviewerAvatars.vue';

const props = defineProps<{
  pr: PullRequest;
}>();

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
