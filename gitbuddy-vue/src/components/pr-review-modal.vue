<template>
  <Transition
    enter-active-class="transition-opacity duration-300 ease-out"
    enter-from-class="opacity-0"
    enter-to-class="opacity-100"
    leave-active-class="transition-opacity duration-200 ease-in"
    leave-from-class="opacity-100"
    leave-to-class="opacity-0"
  >
    <div
      v-if="open"
      role="dialog"
      aria-modal="true"
      aria-label="Review pull request"
      class="fixed inset-0 bg-black/80 flex items-center justify-center z-50 p-4"
      @click.self="emit('close')"
    >
      <div class="bg-slate-900 border border-slate-800 rounded-lg p-6 max-w-lg w-full shadow-2xl shadow-black/50 max-h-[90vh] overflow-y-auto">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg text-slate-100">Review PR</h3>
          <div v-if="pendingComments.length" class="flex items-center gap-2">
            <span class="px-2 py-1 text-xs bg-amber-950/30 text-amber-400 rounded border border-amber-900/50 font-mono tabular-nums">
              {{ pendingComments.length }} pending comment{{ pendingComments.length !== 1 ? 's' : '' }}
            </span>
          </div>
        </div>

        <!-- Pending Review Comments -->
        <div v-if="pendingComments.length" class="mb-4 border border-slate-800 rounded overflow-hidden">
          <div class="px-4 py-2 bg-slate-800/40 border-b border-slate-800">
            <span class="text-xs text-slate-400 uppercase tracking-wider">Your Draft Comments</span>
          </div>
          <div class="divide-y divide-slate-800 max-h-48 overflow-y-auto">
            <div
              v-for="comment in pendingComments"
              :key="comment.gitHubId"
              class="p-3 hover:bg-slate-800/40 group"
            >
              <div class="flex items-start justify-between gap-2">
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2 text-xs text-slate-400 mb-1 font-mono">
                    <span class="truncate">{{ comment.path }}</span>
                    <span v-if="comment.line" class="text-slate-500">:{{ comment.line }}</span>
                  </div>
                  <p class="text-sm text-slate-200 line-clamp-2">{{ comment.body }}</p>
                </div>
                <button
                  @click="emit('delete-pending-comment', comment.gitHubId)"
                  class="opacity-0 group-hover:opacity-100 p-1 hover:bg-rose-500/20 rounded transition-all"
                  title="Delete comment"
                >
                  <svg class="w-4 h-4 text-rose-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </div>
            </div>
          </div>
        </div>

        <div class="grid grid-cols-3 gap-2 mb-4">
          <button
            v-for="action in actions"
            :key="action.value"
            @click="reviewAction = action.value"
            :class="[
              'px-3 py-2.5 rounded-lg text-xs transition-all duration-150 border',
              reviewAction === action.value
                ? 'bg-slate-200 border-slate-200 text-slate-900'
                : 'border-slate-800 text-slate-300 hover:bg-slate-800'
            ]"
          >
            {{ action.label }}
          </button>
        </div>

        <textarea
          v-model="reviewComment"
          :placeholder="placeholder"
          :class="[
            'w-full px-4 py-3 border rounded text-slate-200 text-sm resize-none focus:outline-none focus:ring-2 mb-4 transition-all duration-150 placeholder:text-slate-500',
            bodyRequired && !reviewComment.trim()
              ? 'bg-slate-900 border-rose-500 focus:ring-rose-500/40 focus:border-rose-500'
              : 'bg-slate-900 border-slate-800 focus:ring-slate-500/40 focus:border-slate-600'
          ]"
          rows="4"
        />
        <div v-if="reviewAction === 'CHANGES_REQUESTED' && !reviewComment.trim()" class="text-rose-400 text-xs mb-4">
          Please provide details about the changes you're requesting.
        </div>
        <div v-if="reviewAction === 'COMMENT' && !reviewComment.trim()" class="text-rose-400 text-xs mb-4">
          Please provide a comment.
        </div>
        <div class="flex gap-3 justify-end">
          <button
            v-if="pendingComments.length"
            @click="emit('discard')"
            class="px-4 py-2 bg-rose-500/10 hover:bg-rose-500/20 rounded-lg text-sm text-rose-400 transition-all duration-150 border border-rose-500/30"
          >
            Discard Draft
          </button>
          <div class="flex-1"></div>
          <button
            @click="emit('close')"
            class="px-4 py-2 border border-slate-800 hover:bg-slate-800 rounded-lg text-sm text-slate-300 transition-all duration-150"
          >
            Cancel
          </button>
          <button
            @click="handleSubmit"
            :disabled="submitting || (bodyRequired && !reviewComment.trim())"
            :class="[
              'px-5 py-2 rounded-lg text-sm transition-all duration-150',
              'bg-slate-200 hover:bg-white text-slate-900',
              { 'opacity-50 cursor-not-allowed': submitting || (bodyRequired && !reviewComment.trim()) }
            ]"
          >
            {{ submitting ? 'Submitting...' : (pendingComments.length ? 'Submit Review' : 'Submit') }}
          </button>
        </div>
      </div>
    </div>
  </Transition>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { PendingReviewComment } from '../types';

type ReviewAction = 'APPROVED' | 'CHANGES_REQUESTED' | 'COMMENT';

const props = defineProps<{
  open: boolean;
  pendingComments: PendingReviewComment[];
  submitting: boolean;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'discard'): void;
  (e: 'delete-pending-comment', gitHubId: string): void;
  (e: 'submit', state: ReviewAction, body: string): void;
}>();

const actions: { value: ReviewAction; label: string }[] = [
  { value: 'APPROVED', label: 'Approve' },
  { value: 'CHANGES_REQUESTED', label: 'Changes' },
  { value: 'COMMENT', label: 'Comment' },
];

const reviewAction = ref<ReviewAction>('COMMENT');
const reviewComment = ref('');

const bodyRequired = computed(
  () => reviewAction.value === 'CHANGES_REQUESTED' || reviewAction.value === 'COMMENT'
);

const placeholder = computed(() => {
  if (reviewAction.value === 'APPROVED') return 'Add your approval comment (optional)...';
  if (reviewAction.value === 'CHANGES_REQUESTED') return 'Describe changes requested... (required)';
  return 'Add your comment... (required)';
});

// A fresh open starts from a clean slate rather than the last attempt's text.
watch(() => props.open, (open) => {
  if (open) reviewComment.value = '';
});

const handleSubmit = () => {
  if (bodyRequired.value && !reviewComment.value.trim()) return;
  emit('submit', reviewAction.value, reviewComment.value);
};
</script>
