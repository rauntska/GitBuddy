<template>
  <div
    class="rounded-lg overflow-hidden bg-slate-900/40 border border-slate-700/40"
    :class="{ 'opacity-60': threadId && threadInfo?.isResolved && !isExpanded }"
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
      :last-comment-excerpt="lastCommentExcerpt"
      @toggle="$emit('toggleThread', threadId)"
      @resolve="$emit('resolveThread', threadId, $event)"
    />

    <div v-if="!threadId || isExpanded" class="p-2">
      <div
        v-for="(comment, index) in comments"
        :key="comment.id"
        class="group"
        role="article"
      >
        <div
          class="flex gap-2 transition-all duration-200"
          :class="[
            index === 0 ? 'bg-slate-800/30 rounded p-2 border border-slate-700/20' : 'ml-7 py-1'
          ]"
        >
          <div v-if="index > 0" class="flex flex-col items-center mr-1">
            <div class="w-px h-1 bg-slate-600/40"></div>
            <div class="w-1.5 h-1.5 rounded-full bg-slate-600/50"></div>
          </div>

          <img
            v-if="comment.authorAvatar"
            :src="comment.authorAvatar"
            :alt="comment.author"
            class="rounded-full flex-shrink-0 ring-1 z-10 relative"
            :class="[
              index === 0 
                ? 'w-7 h-7 ring-blue-500/20' 
                : 'w-5 h-5 ring-slate-600/30'
            ]"
          />
          <div 
            v-else 
            class="rounded-full flex items-center justify-center flex-shrink-0 z-10 relative ring-1"
            :class="[
              index === 0 
                ? 'w-7 h-7 bg-slate-700 ring-slate-500/30' 
                : 'w-5 h-5 bg-slate-700 ring-slate-600/30'
            ]"
          >
            <span 
              class="font-semibold text-slate-300"
              :class="index === 0 ? 'text-xs' : 'text-[10px]'"
            >{{ comment.author?.charAt(0)?.toUpperCase() }}</span>
          </div>

          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-1.5">
              <span 
                class="font-medium"
                :class="index === 0 ? 'text-slate-100 text-xs' : 'text-slate-200 text-[11px]'"
              >{{ comment.author }}</span>
              <span class="text-[10px] text-slate-500">{{ formatTime(comment.createdAt) }}</span>
              <span
                v-if="comment.isOutdated && !threadId"
                class="px-1 py-0.5 text-[9px] font-medium bg-amber-500/10 text-amber-400 rounded"
              >
                Outdated
              </span>
              <div v-if="canEdit(comment) && !isEditing(comment.id) && !isReplyingTo(comment.id)" class="flex items-center gap-0.5 ml-auto">
                <button
                  @click="startEdit(comment)"
                  class="p-1 text-slate-500 hover:text-blue-400 hover:bg-blue-500/10 rounded transition-all opacity-0 group-hover:opacity-100"
                  title="Edit"
                  aria-label="Edit comment"
                >
                  <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
                <button
                  @click="$emit('deleteComment', comment.id)"
                  :disabled="deletingCommentId === comment.id"
                  class="p-1 text-slate-500 hover:text-rose-400 hover:bg-rose-500/10 rounded transition-all disabled:opacity-50 opacity-0 group-hover:opacity-100"
                  title="Delete"
                  aria-label="Delete comment"
                >
                  <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </div>
            </div>

            <div v-if="isEditing(comment.id)" class="mt-1.5">
              <RichTextEditor
                ref="editEditorRef"
                v-model="editText"
                placeholder="Edit..."
                :min-height="60"
                @save="submitEdit"
                @cancel="cancelEdit"
              />
              <div class="flex gap-1.5 justify-end mt-2">
                <button
                  @click="cancelEdit"
                  class="px-2 py-1 bg-slate-700/50 hover:bg-slate-600/50 rounded text-[10px] font-medium text-slate-300 transition-all"
                >
                  Cancel
                </button>
                <button
                  @click="submitEdit"
                  :disabled="!editText.trim() || isSubmitting"
                  class="px-2 py-1 bg-blue-600 hover:bg-blue-500 rounded text-[10px] font-medium text-white disabled:opacity-50 transition-all flex items-center gap-1"
                >
                  <svg v-if="isSubmitting" class="w-2.5 h-2.5 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Save
                </button>
              </div>
            </div>

            <p 
              v-else 
              class="text-slate-300/90 leading-snug whitespace-pre-wrap"
              :class="index === 0 ? 'text-xs mt-1' : 'text-[11px]'"
            >{{ comment.body }}</p>

            <div v-if="isReplyingTo(comment.id)" class="mt-2 p-2 bg-slate-800/40 rounded border border-slate-700/20">
              <div class="flex items-center gap-1.5 mb-1.5 text-[10px] text-slate-400">
                <svg class="w-3 h-3 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v10M3 10l6 6m-6-6v6" />
                </svg>
                <span>Reply to <span class="text-blue-400 font-medium">@{{ comment.author }}</span></span>
              </div>
              <RichTextEditor
                ref="replyEditorRef"
                v-model="replyText"
                placeholder="Reply..."
                :min-height="60"
                @save="submitReply"
                @cancel="cancelReply"
              />
              <div v-if="replyError" class="mt-1 text-[10px] text-rose-400">{{ replyError }}</div>
              <div class="flex gap-1.5 justify-end mt-2">
                <button
                  @click="cancelReply"
                  class="px-2 py-1 bg-slate-700/50 hover:bg-slate-600/50 rounded text-[10px] font-medium text-slate-300 transition-all"
                >
                  Cancel
                </button>
                <button
                  @click="submitReply"
                  :disabled="!replyText.trim() || isSubmitting"
                  class="px-2 py-1 bg-blue-600 hover:bg-blue-500 rounded text-[10px] font-medium text-white disabled:opacity-50 transition-all flex items-center gap-1"
                >
                  <svg v-if="isSubmitting" class="w-2.5 h-2.5 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Reply
                </button>
              </div>
            </div>

            <button
              v-if="showReplyButton(comment)"
              @click="startReply(comment)"
              class="mt-1 flex items-center gap-1 px-2 py-0.5 text-[10px] font-medium text-slate-400 hover:text-blue-400 hover:bg-blue-500/10 rounded transition-all opacity-0 group-hover:opacity-100"
              :aria-label="`Reply to ${comment.author}`"
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
          class="ml-7 group py-1"
          role="article"
        >
          <div class="flex gap-2">
            <div class="flex flex-col items-center mr-1">
              <div class="w-px h-1 bg-amber-500/30"></div>
              <div class="w-1.5 h-1.5 rounded-full bg-amber-500/40"></div>
            </div>
            <img
              v-if="pendingReply.authorAvatar"
              :src="pendingReply.authorAvatar"
              :alt="pendingReply.author"
              class="w-5 h-5 rounded-full flex-shrink-0 ring-1 ring-amber-500/30 z-10 relative"
            />
            <div class="flex-1 min-w-0 bg-amber-500/5 border border-amber-500/15 rounded p-2">
              <div class="flex items-center gap-1.5">
                <span class="text-[11px] font-medium text-amber-300">{{ pendingReply.author }}</span>
                <span class="text-[9px] text-amber-500/50">{{ formatTime(pendingReply.createdAt) }}</span>
                <span class="px-1 py-0.5 text-[8px] font-semibold uppercase tracking-wide bg-amber-500/15 text-amber-400 rounded">
                  Pending
                </span>
              </div>
              <p class="text-[11px] text-slate-300/90 whitespace-pre-wrap leading-snug mt-0.5">{{ pendingReply.body }}</p>
            </div>
            <button
              @click="$emit('deletePendingComment', pendingReply.gitHubId)"
              class="opacity-0 group-hover:opacity-100 p-1 hover:bg-rose-500/10 rounded transition-all self-start text-slate-500 hover:text-rose-400"
              title="Delete"
              aria-label="Delete pending reply"
            >
              <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
            </button>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue';
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
  isSubmitting?: boolean;
  expanded?: boolean;
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
  deletePendingComment: [commentId: string];
}>();

const isExpanded = computed(() => {
  if (!props.threadId) return true;
  return props.expanded ?? true;
});

const isResolving = computed(() => false);

const lastCommentExcerpt = computed(() => {
  if (props.comments.length === 0) return '';
  const lastComment = props.comments[props.comments.length - 1];
  if (!lastComment) return '';
  const excerpt = lastComment.body.slice(0, 50);
  return excerpt.length < lastComment.body.length ? `${excerpt}...` : excerpt;
});

const editText = ref('');
const replyText = ref('');
const editEditorRef = ref<InstanceType<typeof RichTextEditor> | null>(null);
const replyEditorRef = ref<InstanceType<typeof RichTextEditor> | null>(null);

watch(() => props.editingCommentId, (newVal) => {
  if (newVal) {
    nextTick(() => {
      editEditorRef.value?.focus();
    });
  }
});

watch(() => props.replyingToCommentId, (newVal) => {
  if (newVal) {
    nextTick(() => {
      replyEditorRef.value?.focus();
    });
  }
});

const formatTime = (dateString: string): string => {
  const date = new Date(dateString);
  const now = new Date();
  const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (seconds < 60) return 'now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h`;
  const days = Math.floor(hours / 24);
  if (days < 7) return `${days}d`;
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

const showReplyButton = (comment: Comment): boolean => {
  return !isReplyingTo(comment.id) && !isEditing(comment.id);
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
  emit('submitEdit', editText.value);
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
  emit('submitReply', replyText.value);
};
</script>
