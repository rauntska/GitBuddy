<template>
  <div
    class="border border-slate-700/30 rounded-xl overflow-hidden"
    :class="{ 'opacity-50': threadId && threadInfo?.isResolved && !isExpanded }"
  >
    <ThreadHeader
      v-if="threadId"
      :thread-id="threadId"
      :thread-git-id="threadInfo?.gitHubId"
      :comment-count="comments.length"
      :is-expanded="isExpanded"
      :is-resolved="threadInfo?.isResolved ?? false"
      :is-outdated="threadInfo?.isOutdated ?? false"
      :is-resolving="isResolving"
      :line-number="lineNumber"
      @toggle="$emit('toggleThread', threadId)"
      @resolve="$emit('resolveThread', threadId, $event)"
    />

    <div v-if="!threadId || isExpanded" class="p-4 space-y-4">
      <div
        v-for="(comment, index) in comments"
        :key="comment.id"
        class="flex gap-3 relative"
        :class="{ 'ml-8': index > 0 }"
      >
        <div
          v-if="index > 0"
          class="absolute left-4 top-0 bottom-0 w-0.5 bg-slate-700/30"
          style="margin-left: -2px;"
        ></div>

        <img
          v-if="comment.authorAvatar"
          :src="comment.authorAvatar"
          :alt="comment.author"
          class="w-8 h-8 rounded-full flex-shrink-0 ring-2 ring-slate-700/50 z-10 relative"
          :class="{ 'w-6 h-6': index > 0 }"
        />
        <div v-else class="w-8 h-8 rounded-full bg-slate-700 flex items-center justify-center flex-shrink-0 z-10 relative">
          <span class="text-sm font-medium text-slate-300">{{ comment.author?.charAt(0)?.toUpperCase() }}</span>
        </div>
        
        <div class="flex-1 min-w-0">
          <div class="flex items-center gap-2 mb-1">
            <span class="text-sm font-medium text-slate-200">{{ comment.author }}</span>
            <span class="text-xs text-slate-500">{{ formatTime(comment.createdAt) }}</span>
            <span
              v-if="comment.isOutdated && !threadId"
              class="px-2 py-0.5 text-xs bg-amber-900/30 text-amber-400 rounded-full border border-amber-700/50"
            >
              Outdated
            </span>
            <template v-if="canEdit(comment) && !isEditing(comment.id) && !isReplyingTo(comment.id)">
              <button
                @click="startEdit(comment)"
                class="p-1 text-slate-500 hover:text-slate-300 hover:bg-slate-700/50 rounded transition-colors"
                title="Edit comment"
              >
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
              </button>
              <button
                @click="$emit('deleteComment', comment.id)"
                :disabled="deletingCommentId === comment.id"
                class="p-1 text-slate-500 hover:text-rose-400 hover:bg-rose-500/10 rounded transition-colors disabled:opacity-50"
                title="Delete comment"
              >
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </template>
          </div>

          <div v-if="isEditing(comment.id)" class="mt-2">
            <RichTextEditor
              v-model="editText"
              placeholder="Edit your comment..."
              :min-height="80"
              @save="submitEdit"
              @cancel="cancelEdit"
            />
            <div class="flex gap-2 justify-end mt-3">
              <button
                @click="cancelEdit"
                class="px-4 py-2 bg-slate-700/50 hover:bg-slate-700 rounded-lg text-xs text-slate-200 transition-all duration-200 border border-slate-600/50 hover:border-slate-500/50"
              >
                Cancel
              </button>
              <button
                @click="submitEdit"
                :disabled="!editText.trim()"
                class="px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-lg text-xs text-white disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200 shadow-lg shadow-blue-500/20 hover:shadow-blue-500/30"
              >
                Save
              </button>
            </div>
          </div>

          <p v-else class="text-sm text-slate-300 leading-relaxed whitespace-pre-wrap">{{ comment.body }}</p>

          <div v-if="isReplyingTo(comment.id)" class="mt-3">
            <RichTextEditor
              v-model="replyText"
              placeholder="Add your reply..."
              :min-height="80"
              @save="submitReply"
              @cancel="cancelReply"
            />
            <div v-if="replyError" class="mt-2 text-xs text-rose-400">{{ replyError }}</div>
            <div class="flex gap-2 justify-end mt-3">
              <button
                @click="cancelReply"
                class="px-4 py-2 bg-slate-700/50 hover:bg-slate-700 rounded-lg text-xs text-slate-200 transition-all duration-200 border border-slate-600/50 hover:border-slate-500/50"
              >
                Cancel
              </button>
              <button
                @click="submitReply"
                :disabled="!replyText.trim()"
                class="px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-lg text-xs text-white disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200 shadow-lg shadow-blue-500/20 hover:shadow-blue-500/30"
              >
                Reply
              </button>
            </div>
          </div>

          <div
            v-if="showReplyButton(comment, index)"
            class="flex-shrink-0 mt-2"
          >
            <button
              @click="startReply(comment)"
              class="flex items-center gap-1 px-3 py-1 text-xs text-blue-400 hover:text-blue-300 hover:bg-blue-500/10 rounded-lg transition-all duration-200"
            >
              <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v10M3 10l6 6m-6-6v6" />
              </svg>
              Reply
            </button>
          </div>
        </div>
      </div>

      <template v-if="threadId && pendingReplies && pendingReplies.length > 0">
        <div
          v-for="pendingReply in pendingReplies"
          :key="pendingReply.gitHubId"
          class="flex gap-3 relative ml-8 group"
        >
          <div class="absolute left-4 top-0 bottom-0 w-0.5 bg-amber-500/30" style="margin-left: -2px;"></div>
          <img
            v-if="pendingReply.authorAvatar"
            :src="pendingReply.authorAvatar"
            :alt="pendingReply.author"
            class="w-6 h-6 rounded-full flex-shrink-0 ring-2 ring-amber-500/30 z-10 relative"
          />
          <div class="flex-1 min-w-0 bg-amber-500/5 border border-amber-500/20 rounded-lg p-3">
            <div class="flex items-center gap-2 mb-1">
              <span class="text-sm font-medium text-amber-300">{{ pendingReply.author }}</span>
              <span class="text-xs text-amber-500/60">{{ formatTime(pendingReply.createdAt) }}</span>
              <span class="px-1.5 py-0.5 text-[10px] bg-amber-500/20 text-amber-400 rounded border border-amber-500/30">Pending</span>
            </div>
            <p class="text-sm text-slate-300 whitespace-pre-wrap">{{ pendingReply.body }}</p>
          </div>
          <button
            @click="$emit('deletePendingComment', pendingReply.gitHubId)"
            class="opacity-0 group-hover:opacity-100 p-1.5 hover:bg-rose-500/20 rounded-lg transition-all self-start"
            title="Delete pending reply"
          >
            <svg class="w-4 h-4 text-rose-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import type { Comment, ReviewThread, PendingReviewComment } from '../types';
import ThreadHeader from './thread-header.vue';
import RichTextEditor from './RichTextEditor.vue';

const props = defineProps<{
  comments: Comment[];
  threadId: number | null;
  threadInfo?: ReviewThread | null;
  lineNumber?: number;
  currentUsername?: string;
  pendingReplies?: PendingReviewComment[];
  editingCommentId: number | null;
  replyingToCommentId: number | null;
  deletingCommentId: number | null;
  replyError?: string;
  isLastInThread: boolean;
}>();

const emit = defineEmits<{
  toggleThread: [threadId: number];
  resolveThread: [threadId: number, resolved: boolean];
  startEdit: [commentId: number, body: string];
  cancelEdit: [];
  submitEdit: [];
  startReply: [commentId: number, threadGitId: string | null];
  cancelReply: [];
  submitReply: [];
  deleteComment: [commentId: number];
  deletePendingComment: [commentId: string];
}>();

const isExpanded = computed(() => {
  if (!props.threadId || !props.threadInfo) return true;
  return true;
});

const isResolving = computed(() => false);

const editText = ref('');
const replyText = ref('');

const formatTime = (dateString: string): string => {
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
  return date.toLocaleDateString();
};

const canEdit = (comment: Comment): boolean => {
  return props.currentUsername !== undefined && 
         comment.author.toLowerCase() === props.currentUsername.toLowerCase();
};

const isEditing = (commentId: number): boolean => {
  return props.editingCommentId === commentId;
};

const isReplyingTo = (commentId: number): boolean => {
  return props.replyingToCommentId === commentId;
};

const showReplyButton = (comment: Comment, index: number): boolean => {
  return props.isLastInThread && 
         index === props.comments.length - 1 && 
         !isReplyingTo(comment.id) && 
         !isEditing(comment.id);
};

const startEdit = (comment: Comment) => {
  editText.value = comment.body;
  emit('startEdit', comment.id, comment.body);
};

const cancelEdit = () => {
  editText.value = '';
  emit('cancelEdit');
};

const submitEdit = () => {
  emit('submitEdit');
};

const startReply = (comment: Comment) => {
  replyText.value = '';
  emit('startReply', comment.id, props.threadInfo?.gitHubId ?? null);
};

const cancelReply = () => {
  replyText.value = '';
  emit('cancelReply');
};

const submitReply = () => {
  emit('submitReply');
};
</script>
