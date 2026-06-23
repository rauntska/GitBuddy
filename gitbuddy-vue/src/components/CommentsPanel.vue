<template>
  <div class="h-full flex flex-col bg-slate-900 border-l border-slate-800 overflow-hidden">
    <!-- Header -->
    <div class="flex items-center justify-between px-4 py-3 border-b border-slate-800">
      <h3 class="text-sm font-semibold text-slate-300 uppercase tracking-wider">
        Comments
        <span class="text-slate-500 font-normal font-mono tabular-nums">({{ comments.length }})</span>
      </h3>
      <button
        @click="$emit('close')"
        class="p-1.5 hover:bg-slate-800 rounded transition-colors text-slate-400 hover:text-slate-200"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>
    </div>

    <!-- Filter Tabs -->
    <div class="flex gap-0.5 px-2 py-1.5 border-b border-slate-800 bg-slate-900/60">
      <button
        v-for="filter in filters"
        :key="filter.value"
        @click="activeFilter = filter.value"
        :class="[
          'flex items-center gap-1 px-2 py-1 rounded text-xs transition-all flex-1 justify-center min-w-0 font-mono tabular-nums',
          activeFilter === filter.value
            ? 'bg-slate-800 text-slate-100 border border-slate-700'
            : 'text-slate-400 hover:bg-slate-800 hover:text-slate-300 border border-transparent'
        ]"
      >
        <component :is="filter.icon" class="w-3 h-3 flex-shrink-0" />
        <span class="truncate">{{ filter.label }}</span>
        <span class="text-[10px] opacity-60 flex-shrink-0">{{ filter.count }}</span>
      </button>
    </div>

    <!-- Comments List -->
    <div class="flex-1 overflow-y-auto">
      <!-- Threaded Comments -->
      <div v-if="groupedThreadsWithComments.length > 0" class="divide-y divide-slate-800">
        <div
          v-for="thread in groupedThreadsWithComments"
          :key="thread.threadInfo.id"
          class="border-l-2 transition-colors"
          :class="[
            thread.threadInfo.isResolved
              ? 'border-l-emerald-500/50 bg-emerald-950/10'
              : thread.threadInfo.isOutdated
                ? 'border-l-orange-500/50 bg-orange-950/10'
                : 'border-l-slate-600'
          ]"
        >
          <!-- Thread Header (always visible) -->
          <div
            @click="toggleThread(thread.threadInfo.id)"
            class="flex items-start gap-3 px-4 py-3 cursor-pointer hover:bg-slate-800/50 transition-colors"
          >
            <div class="relative flex-shrink-0">
              <img
                v-if="thread.comments[0]?.authorAvatar"
                :src="thread.comments[0].authorAvatar"
                :alt="thread.threadInfo.firstCommentAuthor"
                class="w-8 h-8 rounded-full ring-2 ring-slate-700"
              />
              <div v-else class="w-8 h-8 rounded-full bg-slate-700 ring-2 ring-slate-600 flex items-center justify-center">
                <span class="text-xs font-medium text-slate-400">{{ thread.threadInfo.firstCommentAuthor?.charAt(0)?.toUpperCase() }}</span>
              </div>
              <div
                v-if="thread.threadInfo.isResolved"
                class="absolute -bottom-0.5 -right-0.5 w-3 h-3 bg-emerald-500 rounded-full border-2 border-slate-900"
              />
            </div>
            
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 mb-1">
                <span class="text-sm text-slate-200">
                  {{ thread.threadInfo.firstCommentAuthor }}
                </span>
                <span class="text-xs text-slate-500 font-mono">
                  {{ formatRelativeTime(thread.threadInfo.createdAt) }}
                </span>
              </div>

              <p class="text-xs text-slate-400 line-clamp-2 mb-1.5">
                {{ thread.threadInfo.firstCommentBody }}
              </p>

              <div class="flex items-center gap-2 text-xs">
                <button
                  @click.stop="$emit('scrollToThread', thread.threadInfo.gitHubId, thread.threadInfo.line)"
                  class="flex items-center gap-1 px-1.5 py-0.5 rounded bg-slate-800/50 hover:bg-slate-700 hover:text-slate-100 transition-colors group cursor-pointer"
                >
                  <svg class="w-3 h-3 text-slate-500 group-hover:text-slate-300 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                  </svg>
                  <span class="font-mono text-slate-400 group-hover:text-slate-200 truncate max-w-[150px]">
                    {{ thread.threadInfo.path }}
                  </span>
                  <span class="text-slate-600 group-hover:text-slate-400">:</span>
                  <span class="text-slate-400 group-hover:text-slate-200 font-mono tabular-nums">{{ thread.threadInfo.line }}</span>
                </button>
                <span v-if="thread.comments.length > 1" class="px-1.5 py-0.5 rounded bg-slate-800/50 text-slate-400 text-[10px] font-mono tabular-nums">
                  {{ thread.comments.length - 1 }} {{ thread.comments.length - 1 === 1 ? 'reply' : 'replies' }}
                </span>
              </div>
            </div>

            <!-- Status badges & expand icon -->
            <div class="flex items-center gap-2 flex-shrink-0">
              <span
                v-if="thread.threadInfo.isResolved"
                class="px-2 py-0.5 text-[10px] bg-emerald-950/30 text-emerald-400 rounded border border-emerald-900/50 font-mono"
              >
                Resolved
              </span>
              <span
                v-else-if="thread.threadInfo.isOutdated"
                class="px-2 py-0.5 text-[10px] bg-orange-950/30 text-orange-400 rounded border border-orange-900/50 font-mono"
              >
                Outdated
              </span>
              <svg 
                class="w-4 h-4 text-slate-500 transition-transform duration-200"
                :class="{ 'rotate-180': expandedThreads.has(thread.threadInfo.id) }"
                fill="none" 
                stroke="currentColor" 
                viewBox="0 0 24 24"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
              </svg>
            </div>
          </div>
          
          <!-- Expanded Thread Content -->
          <Transition
            enter-active-class="transition-all duration-200 ease-out"
            enter-from-class="opacity-0 max-h-0"
            enter-to-class="opacity-100 max-h-[2000px]"
            leave-active-class="transition-all duration-150 ease-in"
            leave-from-class="opacity-100 max-h-[2000px]"
            leave-to-class="opacity-0 max-h-0"
          >
            <div v-if="expandedThreads.has(thread.threadInfo.id)" class="overflow-hidden">
              <div class="border-t border-slate-800">
                <!-- Code Snippet -->
                <div 
                  v-if="thread.threadInfo.path && thread.threadInfo.line"
                  class="border-b border-slate-800"
                >
                  <div v-if="loadingSnippet[thread.threadInfo.id]" class="px-4 py-3 bg-slate-950/50">
                    <div class="animate-pulse space-y-1">
                      <div class="flex gap-2">
                        <div class="w-8 h-4 bg-slate-800 rounded"></div>
                        <div class="w-3/4 h-4 bg-slate-800 rounded"></div>
                      </div>
                      <div class="flex gap-2">
                        <div class="w-8 h-4 bg-slate-800 rounded"></div>
                        <div class="w-1/2 h-4 bg-slate-800 rounded"></div>
                      </div>
                      <div class="flex gap-2">
                        <div class="w-8 h-4 bg-slate-800 rounded"></div>
                        <div class="w-2/3 h-4 bg-slate-800 rounded"></div>
                      </div>
                    </div>
                  </div>
                  
                  <div 
                    v-else-if="codeSnippets[thread.threadInfo.id]" 
                    @click="$emit('scrollToThread', thread.threadInfo.gitHubId, thread.threadInfo.line)"
                    class="px-4 py-2 bg-slate-950/50 cursor-pointer hover:bg-slate-950/70 transition-colors group"
                  >
                    <div class="font-mono text-xs overflow-x-auto">
                      <div 
                        v-for="line in codeSnippets[thread.threadInfo.id]?.lines" 
                        :key="line.lineNumber"
                        :class="[
                          'flex items-center gap-2 py-0.5 px-1 -mx-1 rounded',
                          line.isHighlighted ? 'bg-yellow-500/20' : ''
                        ]"
                      >
                        <span 
                          class="w-8 text-right text-slate-600 select-none flex-shrink-0"
                          :class="{ 'text-yellow-500': line.isHighlighted }"
                        >
                          {{ line.lineNumber }}
                        </span>
                        <pre 
                          class="flex-1 whitespace-pre overflow-hidden"
                          :class="line.isHighlighted ? 'text-yellow-200' : 'text-slate-400'"
                        >{{ line.content || ' ' }}</pre>
                      </div>
                    </div>
                    <div class="text-[10px] text-slate-600 mt-1 opacity-0 group-hover:opacity-100 transition-opacity">
                      Click to view in diff
                    </div>
                  </div>
                </div>
                
                <!-- Chat Messages (excluding first comment - shown in header) -->
                <div v-if="thread.comments.length > 1" class="p-4 space-y-3 bg-slate-900/30">
                  <div
                    v-for="comment in thread.comments.slice(1)"
                    :key="comment.id"
                    class="flex gap-2.5 ml-6 pl-4 border-l border-slate-800"
                  >
                    <img
                      v-if="comment.authorAvatar"
                      :src="comment.authorAvatar"
                      :alt="comment.author"
                      class="w-6 h-6 rounded-full flex-shrink-0 mt-0.5 ring-1 ring-slate-800"
                    />
                    <div v-else class="w-6 h-6 rounded-full bg-slate-800 flex-shrink-0 mt-0.5 flex items-center justify-center">
                      <span class="text-[10px] text-slate-400">{{ comment.author?.charAt(0)?.toUpperCase() }}</span>
                    </div>

                    <div class="flex-1 min-w-0 bg-slate-800/50 rounded p-2.5">
                      <div class="flex items-center gap-2 mb-1">
                        <span
                          class="text-xs"
                          :class="comment.author === currentUsername ? 'text-slate-100' : 'text-slate-200'"
                        >
                          {{ comment.author }}
                          <span v-if="comment.author === currentUsername" class="text-[9px] text-slate-500 ml-1 font-mono">(you)</span>
                        </span>
                        <span class="text-[10px] text-slate-500 font-mono">
                          {{ formatRelativeTime(comment.createdAt) }}
                        </span>
                        <span
                          v-if="comment.updatedAt && comment.updatedAt !== comment.createdAt"
                          class="text-[10px] text-slate-600 italic font-mono"
                        >
                          (edited)
                        </span>
                      </div>
                      <p class="text-xs text-slate-200 whitespace-pre-wrap leading-relaxed break-words">
                        {{ comment.body }}
                      </p>
                    </div>
                  </div>
                </div>

                <!-- Reply Input -->
                <div class="px-4 pb-4 pt-1">
                  <div class="relative">
                    <div class="flex items-start gap-2 bg-slate-800/50 rounded border border-slate-800 p-2 focus-within:border-slate-600 focus-within:ring-1 focus-within:ring-slate-600/50 transition-colors">
                      <textarea
                        v-model="replyText[thread.threadInfo.id]"
                        @input="handleReplyInputWithResize(thread.threadInfo.id, $event)"
                        @keydown.enter.exact="handleEnterKey(thread.threadInfo.id, $event)"
                        @focus="handleReplyFocus"
                        placeholder="Write a reply... (@ to mention)"
                        rows="1"
                        class="flex-1 bg-transparent text-xs text-slate-200 placeholder-slate-500 resize-none focus:outline-none min-h-[24px] max-h-[120px]"
                      />
                      <button
                        @click="submitReply(thread.threadInfo.id)"
                        :disabled="!replyText[thread.threadInfo.id]?.trim() || submittingReply[thread.threadInfo.id]"
                        class="px-2.5 py-1 bg-slate-200 hover:bg-white disabled:bg-slate-800 disabled:text-slate-500 disabled:cursor-not-allowed rounded text-xs text-slate-900 transition-colors flex-shrink-0"
                      >
                        <span v-if="submittingReply[thread.threadInfo.id]" class="flex items-center gap-1">
                          <svg class="w-3 h-3 animate-spin" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                          </svg>
                          Sending
                        </span>
                        <span v-else>Reply</span>
                      </button>
                    </div>
                    
                    <!-- Mentions Dropdown -->
                    <Transition
                      enter-active-class="transition-all duration-150 ease-out"
                      enter-from-class="opacity-0 -translate-y-1"
                      enter-to-class="opacity-100 translate-y-0"
                      leave-active-class="transition-all duration-100 ease-in"
                      leave-from-class="opacity-100 translate-y-0"
                      leave-to-class="opacity-0 -translate-y-1"
                    >
                      <div
                        v-if="showMentions[thread.threadInfo.id] && filteredMentionableUsers(thread.threadInfo.id).length > 0"
                        class="absolute left-0 right-0 bottom-full mb-1 bg-slate-900 border border-slate-800 rounded shadow-xl overflow-hidden z-20"
                      >
                        <div class="max-h-40 overflow-y-auto">
                          <button
                            v-for="user in filteredMentionableUsers(thread.threadInfo.id)"
                            :key="user.username"
                            @click="selectMention(thread.threadInfo.id, user.username)"
                            class="w-full flex items-center gap-2 px-3 py-2 hover:bg-slate-800 transition-colors text-left"
                          >
                            <img
                              v-if="user.avatarUrl"
                              :src="user.avatarUrl"
                              class="w-5 h-5 rounded-full"
                            />
                            <div v-else class="w-5 h-5 rounded-full bg-slate-800 flex items-center justify-center">
                              <span class="text-[9px] text-slate-400">{{ user.username?.charAt(0)?.toUpperCase() }}</span>
                            </div>
                            <span class="text-xs text-slate-200">@{{ user.username }}</span>
                            <span v-if="user.name" class="text-xs text-slate-500">{{ user.name }}</span>
                          </button>
                        </div>
                      </div>
                    </Transition>
                  </div>
                  
                  <!-- Action Buttons -->
                  <div class="flex items-center mt-2">
                    <button
                      @click.stop="toggleResolve(thread.threadInfo.id, thread.threadInfo.isResolved)"
                      :class="[
                        'flex items-center gap-1.5 px-2 py-1 rounded text-xs transition-colors',
                        thread.threadInfo.isResolved
                          ? 'text-emerald-400 hover:text-emerald-300 hover:bg-emerald-950/20'
                          : 'text-slate-500 hover:text-emerald-400 hover:bg-emerald-950/20'
                      ]"
                    >
                      <svg v-if="thread.threadInfo.isResolved" class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                      </svg>
                      <svg v-else class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                      </svg>
                      {{ thread.threadInfo.isResolved ? 'Unresolve' : 'Resolve' }}
                    </button>
                    
                  </div>
                </div>
              </div>
            </div>
          </Transition>
        </div>
      </div>
      
      <!-- Standalone Comments Section -->
      <div
        v-if="standaloneComments.length > 0 && activeFilter === 'open'"
        class="border-t border-slate-800"
      >
        <div class="px-4 py-2 bg-slate-800/30 flex items-center gap-2">
          <svg class="w-3.5 h-3.5 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
          </svg>
          <span class="text-xs text-slate-400 uppercase tracking-wider font-mono tabular-nums">
            General Comments ({{ standaloneComments.length }})
          </span>
        </div>

        <div class="divide-y divide-slate-800">
          <div
            v-for="comment in standaloneComments"
            :key="comment.id"
            class="flex items-start gap-3 px-4 py-3 cursor-pointer hover:bg-slate-800/50 transition-colors"
            @click="handleStandaloneClick(comment)"
          >
            <div class="relative flex-shrink-0">
              <img
                v-if="comment.authorAvatar"
                :src="comment.authorAvatar"
                :alt="comment.author"
                class="w-8 h-8 rounded-full ring-2 ring-slate-800"
              />
              <div v-else class="w-8 h-8 rounded-full bg-slate-800 ring-2 ring-slate-700 flex items-center justify-center">
                <span class="text-xs text-slate-400">{{ comment.author?.charAt(0)?.toUpperCase() }}</span>
              </div>
            </div>

            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 mb-1">
                <span
                  class="text-sm"
                  :class="comment.author === currentUsername ? 'text-slate-100' : 'text-slate-200'"
                >
                  {{ comment.author }}
                </span>
                <span class="text-xs text-slate-500 font-mono">{{ formatRelativeTime(comment.createdAt) }}</span>
              </div>
              <p class="text-xs text-slate-200 line-clamp-3 whitespace-pre-wrap">{{ comment.body }}</p>

              <div v-if="comment.path" class="flex items-center gap-2 text-xs mt-1.5 text-slate-500 font-mono">
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                </svg>
                <span class="truncate">{{ comment.path }}</span>
                <span v-if="comment.line" class="tabular-nums">:{{ comment.line }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
      
      <!-- Empty State -->
      <div
        v-if="groupedThreadsWithComments.length === 0 && (activeFilter !== 'open' || standaloneComments.length === 0)"
        class="flex flex-col items-center justify-center py-16 px-4"
      >
        <div class="w-16 h-16 rounded-full bg-slate-800 flex items-center justify-center mb-4">
          <svg class="w-8 h-8 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
          </svg>
        </div>
        <p class="text-sm text-slate-400 text-center">
          No {{ activeFilter !== 'open' ? activeFilter : '' }} comments
        </p>
        <p class="text-xs text-slate-500 mt-1 text-center">
          Comments will appear here when reviewers leave feedback
        </p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, h } from 'vue';
import type { Comment, ReviewThread, MentionableUser } from '../types';
import { apiService } from '../services/api';

interface CodeSnippet {
  path: string;
  line: number;
  lines: {
    lineNumber: number;
    content: string;
    isHighlighted: boolean;
  }[];
}

const props = defineProps<{
  comments: Comment[];
  reviewThreads: ReviewThread[];
  prId: number;
  currentUsername?: string;
}>();

const emit = defineEmits<{
  close: [];
  scrollToComment: [comment: Comment, line: number];
  scrollToThread: [threadId: string, line?: number];
  threadResolved: [threadId: string, resolved: boolean];
  replyAdded: [comment: Comment];
}>();

const activeFilter = ref<'open' | 'resolved'>('open');
const expandedThreads = ref<Set<number>>(new Set());
const replyText = ref<Record<number, string>>({});
const submittingReply = ref<Record<number, boolean>>({});
const codeSnippets = ref<Record<number, CodeSnippet | null>>({});
const loadingSnippet = ref<Record<number, boolean>>({});
const mentionableUsers = ref<MentionableUser[]>([]);
const showMentions = ref<Record<number, boolean>>({});
const mentionQuery = ref<Record<number, string>>({});

const ChatBubbleIcon = {
  render: () => h('svg', { class: 'w-3.5 h-3.5', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
    h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z' })
  ])
};

const CheckIcon = {
  render: () => h('svg', { class: 'w-3.5 h-3.5', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
    h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M5 13l4 4L19 7' })
  ])
};

const filters = computed(() => [
  { 
    value: 'open' as const, 
    label: 'Open', 
    icon: ChatBubbleIcon,
    count: props.reviewThreads.filter(t => !t.isResolved).length 
  },
  { 
    value: 'resolved' as const, 
    label: 'Resolved', 
    icon: CheckIcon,
    count: props.reviewThreads.filter(t => t.isResolved).length 
  },
]);

const filteredThreads = computed(() => {
  return props.reviewThreads.filter(thread => {
    if (activeFilter.value === 'open') return !thread.isResolved;
    if (activeFilter.value === 'resolved') return thread.isResolved;
    return true;
  });
});

const groupedThreadsWithComments = computed(() => {
  return filteredThreads.value
    .map(thread => ({
      threadInfo: thread,
      comments: props.comments
        .filter(c => c.reviewThreadId === thread.id)
        .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())
    }))
    .sort((a, b) => new Date(b.threadInfo.createdAt).getTime() - new Date(a.threadInfo.createdAt).getTime());
});

const standaloneComments = computed(() => {
  return props.comments
    .filter(c => !c.reviewThreadId)
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
});

const toggleThread = async (threadId: number) => {
  if (expandedThreads.value.has(threadId)) {
    expandedThreads.value.delete(threadId);
  } else {
    expandedThreads.value.add(threadId);
    await loadCodeSnippet(threadId);
  }
};

const loadCodeSnippet = async (threadId: number) => {
  const thread = props.reviewThreads.find(t => t.id === threadId);
  if (!thread?.path || !thread?.line || codeSnippets.value[threadId] !== undefined) return;
  
  loadingSnippet.value[threadId] = true;
  try {
    const result = await apiService.getFileContentRange(
      props.prId, 
      thread.path, 
      thread.line, 
      3
    );
    
    codeSnippets.value[threadId] = {
      path: thread.path,
      line: thread.line,
      lines: result.lines.map(l => ({
        lineNumber: l.lineNumber,
        content: l.content,
        isHighlighted: l.lineNumber === thread.line
      }))
    };
  } catch (e) {
    console.error('Failed to load code snippet:', e);
    codeSnippets.value[threadId] = null;
  } finally {
    loadingSnippet.value[threadId] = false;
  }
};

const submitReply = async (threadId: number) => {
  const body = replyText.value[threadId]?.trim();
  if (!body || submittingReply.value[threadId]) return;
  
  const thread = props.reviewThreads.find(t => t.id === threadId);
  if (!thread) return;
  
  submittingReply.value[threadId] = true;
  try {
    const result = await apiService.addCommentReply(props.prId, {
      reviewThreadId: thread.gitHubId,
      body
    });
    
    replyText.value[threadId] = '';
    showMentions.value[threadId] = false;
    
    if (result && 'id' in result) {
      emit('replyAdded', result);
    }
  } catch (e) {
    console.error('Failed to submit reply:', e);
  } finally {
    submittingReply.value[threadId] = false;
  }
};

const toggleResolve = async (threadId: number, currentlyResolved: boolean) => {
  const thread = props.reviewThreads.find(t => t.id === threadId);
  if (!thread) return;
  
  try {
    if (currentlyResolved) {
      await apiService.unresolveReviewThread(props.prId, thread.gitHubId);
    } else {
      await apiService.resolveReviewThread(props.prId, thread.gitHubId, true);
    }
    emit('threadResolved', thread.gitHubId, !currentlyResolved);
  } catch (e) {
    console.error('Failed to toggle resolve:', e);
  }
};

const handleReplyFocus = async () => {
  if (mentionableUsers.value.length === 0) {
    try {
      mentionableUsers.value = await apiService.getMentionableUsers(props.prId);
    } catch (e) {
      console.error('Failed to load mentionable users:', e);
    }
  }
};

const handleReplyInput = (threadId: number) => {
  const text = replyText.value[threadId] || '';
  
  const lastAtIndex = text.lastIndexOf('@');
  if (lastAtIndex !== -1) {
    const textAfterAt = text.slice(lastAtIndex + 1);
    const spaceIndex = textAfterAt.indexOf(' ');
    if (spaceIndex === -1) {
      mentionQuery.value[threadId] = textAfterAt;
      showMentions.value[threadId] = true;
      return;
    }
  }
  showMentions.value[threadId] = false;
};

const handleReplyInputWithResize = (threadId: number, event: Event) => {
  handleReplyInput(threadId);
  autoResizeTextarea(event);
};

const handleEnterKey = (threadId: number, _event: KeyboardEvent) => {
  if (showMentions.value[threadId]) {
    const users = filteredMentionableUsers(threadId);
    if (users.length === 1 && users[0]) {
      selectMention(threadId, users[0].username);
      return;
    }
  }
  submitReply(threadId);
};

const selectMention = (threadId: number, username: string) => {
  const text = replyText.value[threadId] || '';
  const lastAtIndex = text.lastIndexOf('@');
  if (lastAtIndex !== -1) {
    replyText.value[threadId] = text.slice(0, lastAtIndex) + `@${username} `;
  }
  showMentions.value[threadId] = false;
};

const filteredMentionableUsers = (threadId: number) => {
  const query = mentionQuery.value[threadId]?.toLowerCase() || '';
  return mentionableUsers.value
    .filter(u => u.username.toLowerCase().includes(query))
    .slice(0, 5);
};

const autoResizeTextarea = (event: Event) => {
  const target = event.target as HTMLTextAreaElement;
  target.style.height = 'auto';
  target.style.height = Math.min(target.scrollHeight, 120) + 'px';
};

const handleStandaloneClick = (comment: Comment) => {
  if (comment.path && comment.line) {
    emit('scrollToComment', comment, comment.line);
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
  if (weeks < 4) return `${weeks}w ago`;
  const months = Math.floor(days / 30);
  if (months < 12) return `${months}mo ago`;
  const years = Math.floor(days / 365);
  return `${years}y ago`;
};
</script>
