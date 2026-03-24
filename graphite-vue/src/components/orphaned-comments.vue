<template>
  <div v-if="groupedThreads.length > 0" class="border-t border-slate-700/30 bg-slate-950/50">
    <div class="px-3 py-1.5 bg-slate-800/40 border-b border-slate-700/30 flex items-center gap-2">
      <svg class="w-3.5 h-3.5 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
      </svg>
      <span class="text-[11px] font-medium text-slate-300">Comments on lines not visible in diff</span>
    </div>
    <div class="p-2 space-y-2">
      <CommentThread
        v-for="[threadId, comments] in groupedThreads"
        :key="threadId || 'standalone'"
        :comments="comments"
        :thread-id="threadId"
        :thread-info="getThreadInfo(threadId)"
        :line-number="comments[0]?.line"
        :current-username="currentUsername"
        :editing-comment-id="editingCommentId"
        :replying-to-comment-id="replyingToCommentId"
        :deleting-comment-id="deletingCommentId"
        :is-submitting="isSubmitting"
        :expanded="isThreadExpanded(threadId)"
        @toggle-thread="toggleThread"
        @resolve-thread="resolveThread"
        @start-edit="startEdit"
        @cancel-edit="cancelEdit"
        @submit-edit="submitEdit"
        @start-reply="startReply"
        @cancel-reply="cancelReply"
        @submit-reply="submitReply"
        @delete-comment="deleteComment"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { Comment, ReviewThread } from '../types';
import CommentThread from './comment-thread.vue';

const props = defineProps<{
  comments: Comment[];
  reviewThreads: ReviewThread[];
  currentUsername?: string;
  editingCommentId: number | null;
  replyingToCommentId: number | null;
  deletingCommentId: number | null;
  expandedThreads: Set<string>;
  isSubmitting?: boolean;
}>();

const emit = defineEmits<{
  toggleThread: [threadId: number];
  resolveThread: [threadId: number, resolved: boolean];
  startEdit: [commentId: number, body: string];
  cancelEdit: [];
  submitEdit: [text: string];
  startReply: [commentId: number, threadGitId: string | null];
  cancelReply: [];
  submitReply: [text: string];
  deleteComment: [commentId: number];
}>();

const groupedThreads = computed(() => {
  const threadMap = new Map<number | null, Comment[]>();
  
  props.comments.forEach(comment => {
    const threadId = comment.reviewThreadId ?? null;
    if (!threadMap.has(threadId)) {
      threadMap.set(threadId, []);
    }
    threadMap.get(threadId)!.push(comment);
  });
  
  threadMap.forEach(threadComments => {
    threadComments.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
  });
  
  return Array.from(threadMap.entries());
});

const getThreadInfo = (threadId: number | null): ReviewThread | null => {
  if (!threadId) return null;
  return props.reviewThreads.find(rt => rt.id === threadId) ?? null;
};

const isThreadExpanded = (threadId: number | null): boolean => {
  if (!threadId) return true;
  const threadInfo = getThreadInfo(threadId);
  if (!threadInfo) return true;
  return props.expandedThreads.has(threadInfo.gitHubId);
};

const toggleThread = (threadId: number) => {
  emit('toggleThread', threadId);
};

const resolveThread = (threadId: number, resolved: boolean) => {
  emit('resolveThread', threadId, resolved);
};

const startEdit = (commentId: number, body: string) => {
  emit('startEdit', commentId, body);
};

const cancelEdit = () => {
  emit('cancelEdit');
};

const submitEdit = (text: string) => {
  emit('submitEdit', text);
};

const startReply = (commentId: number, threadGitId: string | null) => {
  emit('startReply', commentId, threadGitId);
};

const cancelReply = () => {
  emit('cancelReply');
};

const submitReply = (text: string) => {
  emit('submitReply', text);
};

const deleteComment = (commentId: number) => {
  emit('deleteComment', commentId);
};
</script>
