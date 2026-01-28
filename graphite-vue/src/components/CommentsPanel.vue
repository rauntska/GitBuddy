<template>
  <div class="h-full flex flex-col bg-slate-900 border-l border-slate-700">
    <!-- Header -->
    <div class="flex items-center justify-between px-4 py-3 border-b border-slate-700">
      <h3 class="text-sm font-medium text-slate-200">
        Comments ({{ comments.length }})
      </h3>
      <button
        @click="$emit('close')"
        class="p-1 hover:bg-slate-800 rounded transition-colors"
      >
        <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>
    </div>

    <!-- Filters -->
    <div class="flex gap-2 px-4 py-2 border-b border-slate-700">
      <button
        v-for="filter in filters"
        :key="filter.value"
        @click="activeFilter = filter.value"
        :class="[
          'px-3 py-1.5 text-xs rounded transition-colors',
          activeFilter === filter.value
            ? 'bg-blue-600 text-white'
            : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
        ]"
      >
        {{ filter.label }} ({{ getFilterCount(filter.value) }})
      </button>
    </div>

    <!-- Comments List - Grouped by Thread -->
    <div class="flex-1 overflow-auto p-4 space-y-4">
      <!-- Threads -->
      <div
        v-for="thread in filteredThreadsWithComments"
        :key="thread.threadId"
        class="border border-slate-700 rounded-lg overflow-hidden"
      >
        <!-- Thread Header -->
        <div
          class="p-3 bg-slate-800 cursor-pointer hover:bg-slate-750 transition-colors"
          @click="$emit('scrollToThread', thread.threadInfo.gitHubId)"
        >
          <div class="flex items-start gap-3 mb-2">
            <svg class="w-4 h-4 text-slate-500 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                    d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
            </svg>
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 mb-1">
                <span class="text-xs font-medium text-slate-200">{{ thread.threadInfo.firstCommentAuthor }}</span>
                <span class="text-xs text-slate-500">· {{ thread.comments.length }} comments</span>
                <span class="text-xs text-slate-500">{{ formatRelativeTime(thread.threadInfo.createdAt) }}</span>
              </div>
              
              <!-- File Path and Line -->
              <div v-if="thread.threadInfo.path" class="flex items-center gap-2 text-xs text-slate-400">
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                        d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                </svg>
                <span class="truncate font-mono">{{ thread.threadInfo.path }}</span>
                <span v-if="thread.threadInfo.line">:{{ thread.threadInfo.line }}</span>
              </div>
            </div>

            <!-- Badges -->
            <div class="flex items-center gap-2">
              <span
                v-if="thread.threadInfo.isOutdated"
                class="px-2 py-0.5 text-xs bg-orange-900/30 text-orange-400 rounded border border-orange-700/50"
              >
                Outdated
              </span>
              <span
                v-if="thread.threadInfo.isResolved"
                class="px-2 py-0.5 text-xs bg-green-900/30 text-green-400 rounded border border-green-700/50"
              >
                Resolved
              </span>
            </div>
          </div>

          <!-- First Comment Body Preview -->
          <p class="text-xs text-slate-300 truncate ml-7">{{ thread.threadInfo.firstCommentBody }}</p>
        </div>

        <!-- Comments in Thread (Sorted by date, oldest first) -->
        <div class="divide-y divide-slate-700/50">
          <div
            v-for="comment in thread.comments"
            :key="comment.id"
            class="p-3 bg-slate-800/50 hover:bg-slate-800 transition-colors"
          >
            <div class="flex items-start gap-2 mb-1">
              <img
                v-if="comment.authorAvatar"
                :src="comment.authorAvatar"
                :alt="comment.author"
                class="w-6 h-6 rounded-full flex-shrink-0"
              />
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2">
                  <span class="text-xs font-medium text-slate-200">{{ comment.author }}</span>
                  <span class="text-[10px] text-slate-500">{{ formatRelativeTime(comment.createdAt) }}</span>
                </div>
                <p class="text-xs text-slate-300 whitespace-pre-wrap mt-1">{{ comment.body }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Standalone Comments (not part of a thread) -->
      <div
        v-for="comment in filteredStandaloneComments"
        :key="comment.id"
        class="p-3 bg-slate-800 rounded-lg border border-slate-700 hover:border-slate-600 transition-colors cursor-pointer"
        @click="$emit('scrollToComment', comment)"
      >
        <!-- Comment Header -->
        <div class="flex items-start gap-3 mb-2">
          <img
            v-if="comment.authorAvatar"
            :src="comment.authorAvatar"
            :alt="comment.author"
            class="w-8 h-8 rounded-full flex-shrink-0"
          />
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 mb-1">
              <span class="text-sm font-medium text-slate-200">{{ comment.author }}</span>
              <span class="text-xs text-slate-500">{{ formatRelativeTime(comment.createdAt) }}</span>
            </div>
            
            <!-- File Path and Line -->
            <div v-if="comment.path" class="flex items-center gap-2 text-xs text-slate-400">
              <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                      d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
              </svg>
              <span class="truncate font-mono">{{ comment.path }}</span>
              <span v-if="comment.line">:{{ comment.line }}</span>
            </div>
          </div>

          <!-- Outdated Badge -->
          <span
            v-if="comment.isOutdated"
            class="px-2 py-0.5 text-xs bg-orange-900/30 text-orange-400 rounded border border-orange-700/50"
          >
            Outdated
          </span>
        </div>

        <!-- Comment Body -->
        <p class="text-sm text-slate-300 whitespace-pre-wrap">{{ comment.body }}</p>
      </div>

      <!-- Empty State -->
      <div v-if="filteredThreadsWithComments.length === 0 && filteredStandaloneComments.length === 0" class="text-center py-12">
        <svg class="w-12 h-12 mx-auto text-slate-600 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
        </svg>
        <p class="text-sm text-slate-400">No {{ activeFilter }} comments</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import type { Comment, ReviewThread } from '../types';

const props = defineProps<{
  comments: Comment[];
  reviewThreads: ReviewThread[];
}>();

defineEmits<{
  close: [];
  scrollToComment: [comment: Comment];
  scrollToThread: [threadId: string];
}>();

const activeFilter = ref<'all' | 'file' | 'outdated' | 'thread'>('all');

const filters = [
  { label: 'All', value: 'all' as const },
  { label: 'Threads', value: 'thread' as const },
  { label: 'In Files', value: 'file' as const },
  { label: 'Outdated', value: 'outdated' as const },
];

// Group comments by review thread ID and sort within each thread by date (oldest first)
const groupedThreads = computed(() => {
  const threadMap = new Map<number, { threadInfo: ReviewThread; comments: Comment[] }>();
  
  // Group comments by thread
  props.comments.forEach(comment => {
    if (comment.reviewThreadId) {
      const threadInfo = props.reviewThreads.find(rt => rt.id === comment.reviewThreadId);
      if (threadInfo) {
        if (!threadMap.has(comment.reviewThreadId)) {
          threadMap.set(comment.reviewThreadId, { threadInfo, comments: [] });
        }
        threadMap.get(comment.reviewThreadId)!.comments.push(comment);
      }
    }
  });

  // Sort comments within each thread by date (oldest first)
  threadMap.forEach((value) => {
    value.comments.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
  });

  // Convert to array and sort threads by their first comment date
  return Array.from(threadMap.entries())
    .map(([threadId, data]) => ({ threadId, ...data }))
    .sort((a, b) => new Date(a.threadInfo.createdAt).getTime() - new Date(b.threadInfo.createdAt).getTime());
});

const standaloneComments = computed(() => {
  return props.comments
    .filter(c => !c.reviewThreadId)
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
});

const filteredThreadsWithComments = computed(() => {
  switch (activeFilter.value) {
    case 'file':
      return groupedThreads.value.filter(t => t.threadInfo.path);
    case 'outdated':
      return groupedThreads.value.filter(t => t.threadInfo.isOutdated || t.comments.some(c => c.isOutdated));
    case 'thread':
      return groupedThreads.value;
    default:
      return groupedThreads.value;
  }
});

const filteredStandaloneComments = computed(() => {
  switch (activeFilter.value) {
    case 'file':
      return standaloneComments.value.filter(c => c.path);
    case 'outdated':
      return standaloneComments.value.filter(c => c.isOutdated);
    case 'thread':
      return [];
    default:
      return standaloneComments.value;
  }
});

const getFilterCount = (filter: typeof activeFilter.value) => {
  switch (filter) {
    case 'file':
      return props.comments.filter(c => c.path).length;
    case 'outdated':
      return props.comments.filter(c => c.isOutdated).length;
    case 'thread':
      return groupedThreads.value.length;
    default:
      return props.comments.length;
  }
};

const formatRelativeTime = (dateString: string): string => {
  const date = new Date(dateString);
  const now = new Date();
  const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days < 7) return `${days}d ago`;
  const weeks = Math.floor(days / 7);
  return `${weeks}w ago`;
};
</script>
