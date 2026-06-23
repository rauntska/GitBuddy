<template>
  <div class="p-4 border border-slate-800 rounded">
    <div class="flex items-center justify-between mb-3">
      <button
        @click="isExpanded = !isExpanded"
        class="flex items-center gap-2 text-xs font-semibold text-slate-300 uppercase tracking-wider hover:text-slate-100 transition-colors"
      >
        <svg
          :class="['w-4 h-4 transition-transform duration-200', { 'rotate-90': isExpanded }]"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
        <span>General Comments</span>
        <span class="text-slate-500 font-normal font-mono tabular-nums">({{ comments.length }})</span>
      </button>
      <div v-if="isExpanded && !isMerged" class="flex items-center gap-2">
        <button
          @click="showAddForm = !showAddForm"
          class="px-3 py-1.5 border border-slate-800 hover:bg-slate-800 rounded text-xs text-slate-300 transition-all duration-150"
        >
          {{ showAddForm ? 'Cancel' : 'Add Comment' }}
        </button>
      </div>
    </div>

    <Transition
      enter-active-class="transition-all duration-200 ease-out"
      enter-from-class="opacity-0 max-h-0"
      enter-to-class="opacity-100 max-h-[2000px]"
      leave-active-class="transition-all duration-200 ease-in"
      leave-from-class="opacity-100 max-h-[2000px]"
      leave-to-class="opacity-0 max-h-0"
    >
      <div v-show="isExpanded" class="overflow-hidden">
        <div v-if="comments.length === 0 && !showAddForm" class="text-sm text-slate-500 py-4 text-center">
          No general comments yet
        </div>

        <div v-if="showAddForm && !isMerged" class="mb-4 p-3 bg-slate-800/40 rounded border border-slate-800">
          <textarea
            v-model="newCommentBody"
            placeholder="Add a general comment..."
            class="w-full px-3 py-2 bg-slate-900 border border-slate-800 rounded text-slate-200 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-slate-500/40 placeholder:text-slate-500"
            rows="3"
          />
          <div class="flex justify-end gap-2 mt-2">
            <button
              @click="showAddForm = false; newCommentBody = ''"
              class="px-3 py-1.5 text-xs text-slate-400 hover:text-slate-200 transition-colors"
            >
              Cancel
            </button>
            <button
              @click="handleAddComment"
              :disabled="!newCommentBody.trim() || addingComment"
              class="px-3 py-1.5 bg-slate-200 hover:bg-white rounded text-xs text-slate-900 transition-all duration-150 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {{ addingComment ? 'Adding...' : 'Add Comment' }}
            </button>
          </div>
        </div>

        <div class="space-y-3">
          <div
            v-for="comment in comments"
            :key="comment.id"
            class="p-3 bg-slate-800/30 rounded border border-slate-800 group"
          >
            <div class="flex items-start gap-3">
              <img
                v-if="comment.authorAvatar"
                :src="comment.authorAvatar"
                :alt="comment.author"
                class="w-8 h-8 rounded-full ring-2 ring-slate-800 flex-shrink-0"
              />
              <div class="w-8 h-8 rounded-full bg-slate-800 ring-2 ring-slate-700 flex-shrink-0 flex items-center justify-center" v-else>
                <span class="text-xs text-slate-400">{{ comment.author?.charAt(0)?.toUpperCase() }}</span>
              </div>

              <div class="flex-1 min-w-0">
                <div class="flex items-center justify-between gap-2">
                  <div class="flex items-center gap-2">
                    <span class="text-sm text-slate-200">{{ comment.author }}</span>
                    <span class="text-xs text-slate-500 font-mono">{{ formatDate(comment.createdAt) }}</span>
                    <span v-if="comment.updatedAt && comment.updatedAt !== comment.createdAt" class="text-xs text-slate-600 font-mono">(edited)</span>
                  </div>
                  <div v-if="comment.author === currentUsername && !isMerged" class="opacity-0 group-hover:opacity-100 flex items-center gap-1 transition-opacity">
                    <button
                      @click="startEdit(comment)"
                      class="p-1 hover:bg-slate-700/50 rounded transition-colors"
                      title="Edit comment"
                    >
                      <svg class="w-4 h-4 text-slate-400 hover:text-slate-200" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                      </svg>
                    </button>
                    <button
                      @click="handleDeleteComment(comment)"
                      class="p-1 hover:bg-rose-500/20 rounded transition-colors"
                      title="Delete comment"
                    >
                      <svg class="w-4 h-4 text-slate-400 hover:text-rose-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    </button>
                  </div>
                </div>

                <div v-if="editingCommentId === comment.gitHubId" class="mt-2">
                  <textarea
                    v-model="editBody"
                    class="w-full px-3 py-2 bg-slate-900 border border-slate-800 rounded text-slate-200 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-slate-500/40"
                    rows="3"
                  />
                  <div class="flex justify-end gap-2 mt-2">
                    <button
                      @click="cancelEdit"
                      class="px-3 py-1 text-xs text-slate-400 hover:text-slate-200 transition-colors"
                    >
                      Cancel
                    </button>
                    <button
                      @click="handleUpdateComment(comment)"
                      :disabled="!editBody.trim() || updatingComment"
                      class="px-3 py-1 bg-slate-200 hover:bg-white rounded text-xs text-slate-900 transition-all duration-150 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {{ updatingComment ? 'Saving...' : 'Save' }}
                    </button>
                  </div>
                </div>
                <div v-else class="mt-1 text-sm text-slate-200 whitespace-pre-wrap break-words">{{ comment.body }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import type { Comment } from '../types';

const props = defineProps<{
  comments: Comment[];
  currentUsername: string;
  isMerged?: boolean;
  prId: number;
}>();

const emit = defineEmits<{
  (e: 'add-comment', body: string): void;
  (e: 'update-comment', gitHubId: string, body: string): void;
  (e: 'delete-comment', gitHubId: string): void;
}>();

const isExpanded = ref(false);
const showAddForm = ref(false);
const newCommentBody = ref('');
const addingComment = ref(false);

const editingCommentId = ref<string | null>(null);
const editBody = ref('');
const updatingComment = ref(false);

const generalComments = computed(() => {
  return props.comments.filter(c => !c.path && !c.line);
});

const comments = generalComments;

const formatDate = (dateStr: string) => {
  const date = new Date(dateStr);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return 'just now';
  if (diffMins < 60) return `${diffMins}m ago`;
  if (diffHours < 24) return `${diffHours}h ago`;
  if (diffDays < 7) return `${diffDays}d ago`;
  return date.toLocaleDateString();
};

const handleAddComment = async () => {
  if (!newCommentBody.value.trim()) return;
  
  addingComment.value = true;
  try {
    emit('add-comment', newCommentBody.value.trim());
    newCommentBody.value = '';
    showAddForm.value = false;
  } finally {
    addingComment.value = false;
  }
};

const startEdit = (comment: Comment) => {
  editingCommentId.value = comment.gitHubId;
  editBody.value = comment.body;
};

const cancelEdit = () => {
  editingCommentId.value = null;
  editBody.value = '';
};

const handleUpdateComment = async (comment: Comment) => {
  if (!editBody.value.trim()) return;
  
  updatingComment.value = true;
  try {
    emit('update-comment', comment.gitHubId, editBody.value.trim());
    editingCommentId.value = null;
    editBody.value = '';
  } finally {
    updatingComment.value = false;
  }
};

const handleDeleteComment = (comment: Comment) => {
  if (confirm('Are you sure you want to delete this comment?')) {
    emit('delete-comment', comment.gitHubId);
  }
};
</script>
