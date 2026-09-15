<template>
  <div ref="headerRef" class="sticky top-20 z-20 bg-slate-900/95 border-b border-slate-800">
    <div class="px-3 sm:px-5 py-3 flex flex-wrap items-center gap-2 sm:gap-3">
      <Breadcrumb
        v-if="prDetail"
        :items="[
          { label: 'Dashboard', to: '/' },
          { label: prDetail.repository },
          { label: `#${prDetail.gitHubId}` },
        ]"
      />
      <div v-else class="text-slate-500 text-sm font-mono">Loading...</div>

      <div class="flex-1 min-w-0 flex items-center gap-2 sm:gap-3 overflow-hidden">
        <StatusBadge v-if="prDetail" :status="prDetail.status" />

        <div
          v-if="prDetail && !prDetail.isMerged && !prDetail.draft && prDetail.status !== 'Merged' && prDetail.status !== 'Closed'"
          :class="[
            'px-2 sm:px-3 py-1 rounded-full text-xs border',
            prDetail.isMergeReady
              ? 'bg-emerald-950/30 text-emerald-400 border-emerald-900/50'
              : 'bg-amber-950/20 text-amber-400 border-amber-900/50'
          ]"
          :title="prDetail.mergeBlockReason || 'Ready to merge'"
        >
          <span v-if="prDetail.isMergeReady" class="flex items-center gap-1 sm:gap-1.5">
            <span class="font-mono leading-none text-emerald-400">✓</span>
            <span class="hidden sm:inline">Ready to merge</span>
          </span>
          <span v-else-if="prDetail.requiredApprovingReviews" class="flex items-center gap-1 sm:gap-1.5 font-mono tabular-nums">
            <span class="leading-none text-amber-400">⋯</span>
            <span class="hidden sm:inline">{{ prDetail.currentApprovingReviews }}/{{ prDetail.requiredApprovingReviews }} approvals</span>
            <span class="sm:hidden">{{ prDetail.currentApprovingReviews }}/{{ prDetail.requiredApprovingReviews }}</span>
            <button
              v-if="prDetail.hasUnresolvedThreads"
              @click="emit('jump-to', 'pr-card-reviewers')"
              class="ml-1 hidden sm:inline hover:text-amber-300 transition-colors"
              title="Go to reviewers"
            >• {{ unresolvedActiveCount }} unresolved</button>
          </span>
          <span v-else class="flex items-center gap-1 sm:gap-1.5">
            <span class="hidden sm:inline">{{ prDetail.mergeBlockReason || 'Review required' }}</span>
          </span>
        </div>

        <div class="flex-1 min-w-0 flex items-center gap-2 group/title">
          <template v-if="isEditingTitle">
            <input
              ref="titleInputRef"
              v-model="editTitleValue"
              @keydown="handleTitleKeydown"
              @blur="savingTitle ? null : saveTitle()"
              :disabled="savingTitle"
              class="flex-1 min-w-0 bg-slate-800 border border-slate-600 rounded px-2 py-1 text-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-slate-500/50"
              :class="{ 'opacity-50': savingTitle }"
            />
            <svg v-if="savingTitle" class="animate-spin h-4 w-4 text-slate-400 flex-shrink-0" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
          </template>
          <template v-else>
            <h1 class="text-lg text-slate-100 truncate tracking-tight min-w-0 flex-shrink">
              {{ prDetail?.title || 'Loading...' }}
            </h1>
            <button
              v-if="prDetail && !prDetail.isMerged"
              @click="startEditingTitle"
              class="p-1 rounded opacity-0 group-hover/title:opacity-100 hover:bg-slate-800 text-slate-400 hover:text-slate-200 transition-all flex-shrink-0"
              title="Edit title"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
              </svg>
            </button>
          </template>
        </div>
      </div>

      <div class="flex flex-wrap items-center gap-2 sm:gap-3 text-xs flex-shrink-0">
        <span class="text-slate-400 hidden sm:inline font-mono tabular-nums">{{ prDetail?.repository }} #{{ prDetail?.gitHubId }}</span>

        <button
          v-if="!prDetail?.isMerged"
          @click="emit('review')"
          :class="[
            'px-3 sm:px-4 py-2 rounded-lg text-xs transition-all duration-150 relative',
            prDetail?.pendingReview?.comments?.length
              ? 'bg-slate-200 text-slate-900 hover:bg-white'
              : 'border border-slate-800 text-slate-300 hover:bg-slate-800'
          ]"
        >
          <span class="sm:hidden">Review</span>
          <span class="hidden sm:inline">{{ prDetail?.pendingReview?.comments?.length ? 'Finish Review' : 'Review' }}</span>
          <span
            v-if="prDetail?.pendingReview?.comments?.length"
            class="absolute -top-1.5 -right-1.5 bg-amber-400 text-slate-900 text-[10px] rounded-full min-w-[18px] h-[18px] flex items-center justify-center px-0.5 font-mono tabular-nums"
          >
            {{ prDetail.pendingReview.comments.length }}
          </span>
        </button>

        <button
          v-if="prDetail?.draft && !prDetail?.isMerged"
          @click="emit('publish-draft')"
          :disabled="publishingDraft"
          class="px-3 sm:px-4 py-2 bg-slate-200 hover:bg-white rounded-lg text-xs text-slate-900 transition-all duration-150 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <span class="sm:hidden">Pub</span>
          <span class="hidden sm:inline">{{ publishingDraft ? 'Publishing...' : 'Publish' }}</span>
        </button>

        <pr-merge-button
          v-if="prDetail && !prDetail.draft && !prDetail.isMerged"
          :pr-detail="prDetail"
          :merging="merging"
          :merge-error="mergeError"
          @merge="(method: MergeMethod) => emit('merge', method)"
          @dismiss-error="emit('dismiss-merge-error')"
        />

        <button
          @click="emit('toggle-comments')"
          :class="[
            'p-2 rounded-lg relative transition-all duration-150',
            commentsPanelOpen
              ? 'bg-slate-200 text-slate-900'
              : 'border border-slate-800 text-slate-300 hover:bg-slate-800'
          ]"
          title="Comments"
        >
          <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M18 10c0 3.866-3.582 7-8 7a8.841 8.841 0 01-4.083-.98L2 17l1.338-3.123C2.493 12.767 2 11.434 2 10c0-3.866 3.582-7 8-7s8 3.134 8 7zM7 9H5v2h2V9zm8 0h-2v2h2V9zM9 9h2v2H9V9z" clip-rule="evenodd" />
          </svg>
          <span
            v-if="unresolvedCount > 0"
            class="absolute -top-1.5 -right-1.5 bg-slate-200 text-slate-900 text-[10px] rounded-full min-w-[18px] h-[18px] flex items-center justify-center px-0.5 font-mono tabular-nums"
          >
            {{ unresolvedCount }}
          </span>
        </button>

        <button
          @click="emit('refresh-view-states')"
          :disabled="refreshingViewStates"
          class="hidden sm:flex px-3 py-2 border border-slate-800 hover:bg-slate-800 rounded-lg text-xs text-slate-300 transition-all duration-150 disabled:opacity-50 disabled:cursor-not-allowed items-center gap-2"
          title="Refresh file viewed states from GitHub"
        >
          <svg
            :class="['w-3.5 h-3.5', { 'animate-spin': refreshingViewStates }]"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
          <span>{{ refreshingViewStates ? 'Refreshing...' : 'Refresh' }}</span>
        </button>

        <a
          :href="prDetail?.url"
          target="_blank"
          rel="noopener noreferrer"
          class="px-3 py-2 border border-slate-800 hover:bg-slate-800 rounded-lg text-xs text-slate-300 transition-all duration-150"
        >
          GitHub
        </a>
      </div>
    </div>

    <!-- Condensed context strip — appears once the rail scrolls out of view.
         Opacity-only transition: animating height would re-trigger the rail observer. -->
    <Transition
      enter-active-class="transition-opacity duration-200"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-opacity duration-150"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <pr-context-strip
        v-if="prDetail && condensed"
        :pr-detail="prDetail"
        @jump-to="(cardId: string) => emit('jump-to', cardId)"
      />
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { nextTick, onMounted, onUnmounted, ref } from 'vue';
import Breadcrumb from './Breadcrumb.vue';
import StatusBadge from './StatusBadge.vue';
import PrMergeButton from './pr-merge-button.vue';
import PrContextStrip from './pr-context-strip.vue';
import type { MergeMethod, PRDetail } from '../types';

const props = defineProps<{
  prDetail: PRDetail | null;
  /** True once the context rail has scrolled out of view. */
  condensed: boolean;
  /** All unresolved threads — matches the comments panel's own count. */
  unresolvedCount: number;
  /** Unresolved and not outdated — what a reviewer still has to act on. */
  unresolvedActiveCount: number;
  commentsPanelOpen: boolean;
  publishingDraft: boolean;
  merging: boolean;
  mergeError: string | null;
  refreshingViewStates: boolean;
  savingTitle: boolean;
}>();

const emit = defineEmits<{
  (e: 'jump-to', cardId: string): void;
  (e: 'review'): void;
  (e: 'publish-draft'): void;
  (e: 'merge', method: MergeMethod): void;
  (e: 'dismiss-merge-error'): void;
  (e: 'toggle-comments'): void;
  (e: 'refresh-view-states'): void;
  (e: 'save-title', title: string): void;
}>();

// Publish the header's height so panels below it can stick under it without magic numbers.
const headerRef = ref<HTMLElement | null>(null);
let headerObserver: ResizeObserver | null = null;

onMounted(() => {
  if (!headerRef.value) return;

  const write = () => {
    if (!headerRef.value) return;
    document.documentElement.style.setProperty('--pr-header-h', `${headerRef.value.offsetHeight}px`);
  };

  write();
  headerObserver = new ResizeObserver(write);
  headerObserver.observe(headerRef.value);
});

onUnmounted(() => {
  headerObserver?.disconnect();
  headerObserver = null;
  document.documentElement.style.removeProperty('--pr-header-h');
});

// Title editing
const isEditingTitle = ref(false);
const editTitleValue = ref('');
const titleInputRef = ref<HTMLInputElement | null>(null);

const startEditingTitle = () => {
  if (props.prDetail?.isMerged) return;
  editTitleValue.value = props.prDetail?.title || '';
  isEditingTitle.value = true;
  nextTick(() => {
    titleInputRef.value?.focus();
    titleInputRef.value?.select();
  });
};

const cancelEditingTitle = () => {
  isEditingTitle.value = false;
  editTitleValue.value = '';
};

const saveTitle = () => {
  const title = editTitleValue.value.trim();
  if (!title || title === props.prDetail?.title) {
    cancelEditingTitle();
    return;
  }

  emit('save-title', title);
  isEditingTitle.value = false;
};

const handleTitleKeydown = (event: KeyboardEvent) => {
  if (event.key === 'Enter') {
    event.preventDefault();
    saveTitle();
  }
  if (event.key === 'Escape') {
    cancelEditingTitle();
  }
};
</script>
