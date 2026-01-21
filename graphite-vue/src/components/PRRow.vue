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

    <div class="flex-1 min-w-0">
      <div class="text-sm text-slate-300 truncate group-hover:text-white transition-colors">
        {{ pr.title }}
      </div>
    </div>

    <div class="flex items-center gap-2 flex-shrink-0">
      <img
        :src="pr.authorAvatar"
        :alt="pr.author"
        :title="pr.author"
        class="w-6 h-6 rounded-full"
      />
    </div>

    <div class="flex items-center gap-3 flex-shrink-0">
      <ReviewerAvatars :reviews="pr.reviews" :max-display="4" />
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
