<template>
  <div class="flex flex-col bg-slate-900 text-slate-200 min-h-screen">
    <pr-detail-header
      :pr-detail="prDetail"
      :condensed="condensed"
      :unresolved-count="unresolvedThreadsCount"
      :unresolved-active-count="unresolvedActiveThreadsCount"
      :comments-panel-open="commentsPanel"
      :publishing-draft="publishingDraft"
      :merging="merging"
      :merge-error="mergeError"
      :refreshing-view-states="refreshingViewStates"
      :saving-title="savingTitle"
      @jump-to="handleJumpTo"
      @review="showReviewModal = true"
      @publish-draft="handlePublishDraft"
      @merge="handleMerge"
      @dismiss-merge-error="mergeError = null"
      @toggle-comments="toggleCommentsPanel"
      @refresh-view-states="refreshFileViewStates"
      @save-title="handleSaveTitle"
    />

    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center flex-1 min-h-[400px]">
      <div class="text-center">
        <svg class="animate-spin h-16 w-16 mx-auto text-slate-400 mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <p class="text-slate-400 text-sm">Loading PR details...</p>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="flex items-center justify-center flex-1 min-h-[400px]">
      <div class="text-center max-w-md">
        <svg class="w-20 h-20 mx-auto text-red-500 mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <h2 class="text-2xl text-slate-200 mb-3">Failed to load PR</h2>
        <p class="text-slate-400 mb-6 text-sm">{{ error }}</p>
        <button
          @click="fetchPRDetail(props.id)"
          class="px-5 py-2.5 bg-slate-200 hover:bg-white rounded-lg text-slate-900 transition-all duration-150"
        >
          Retry
        </button>
      </div>
    </div>

    <!-- Main Content — one scroll: overview block, then the diffs -->
    <div v-else-if="prDetail" class="flex flex-col flex-1">
      <!-- Overview: description + comments, with the context rail alongside -->
      <div class="border-b border-slate-800">
        <div class="flex flex-col lg:flex-row gap-4 lg:gap-6 p-4 sm:p-6">
          <pr-conversation
            :pr-detail="prDetail"
            :current-username="authStore.username"
            :collapse-description="isRequestedReviewer"
            @save-description="handleDescriptionSave"
            @add-comment="handleAddGeneralComment"
            @update-comment="handleUpdateGeneralComment"
            @delete-comment="handleDeleteGeneralComment"
          />
          <pr-context-rail
            ref="railRef"
            :pr-detail="prDetail"
            :setting-priority="settingPriority"
            @priority-change="handlePriorityChange"
            @reviewer-error="handleReviewerError"
            @timeline-error="handleTimelineError"
          />
        </div>
      </div>

      <pr-files
        ref="filesRef"
        :pr-detail="prDetail"
        :current-username="authStore.username"
        :initial-file-path="selectedFilePath"
        :sticky-top="stickyTop"
        :on-add-comment="handleAddComment"
        :on-delete-pending-comment="handleDeletePendingComment"
        :on-reply-to-thread="handleReplyToThread"
        :on-resolve-thread="handleResolveThread"
        :on-edit-comment="handleEditComment"
        :on-delete-comment="handleDeleteComment"
        :on-toggle-viewed="handleToggleViewed"
        @select-file="setSelectedFilePath"
      />

      <Transition
        enter-active-class="transition-opacity duration-300 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition-opacity duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="commentsPanel"
          class="fixed inset-0 bg-black/50 z-20 md:hidden"
          @click="toggleCommentsPanel"
        />
      </Transition>
      <Transition
        enter-active-class="transition-transform duration-300 ease-out"
        enter-from-class="translate-x-full"
        enter-to-class="translate-x-0"
        leave-active-class="transition-transform duration-200 ease-in"
        leave-from-class="translate-x-0"
        leave-to-class="translate-x-full"
      >
        <div
          v-if="commentsPanel"
          class="fixed right-0 top-0 md:top-20 z-30 h-screen md:h-[calc(100vh-5rem)] w-[90%] sm:w-80 md:w-[320px]"
          :style="{ width: commentsPanelWidth ? `${commentsPanelWidth}px` : undefined }"
        >
          <CommentsPanel
            :comments="prDetail?.allComments || []"
            :review-threads="prDetail?.reviewThreads || []"
            :pr-id="id"
            :current-username="authStore.username"
            @close="toggleCommentsPanel"
            @scroll-to-comment="scrollToComment"
            @scroll-to-thread="scrollToThread"
            @reply-added="handleReplyAdded"
            @thread-resolved="handleThreadResolved"
          />
          <div
            class="hidden md:block absolute top-0 left-0 w-1.5 h-full cursor-ew-resize bg-slate-800 hover:bg-slate-600 transition-colors duration-150"
            @mousedown="startResizeCommentsPanel"
          />
        </div>
      </Transition>
    </div>

    <pr-review-modal
      :open="showReviewModal"
      :pending-comments="prDetail?.pendingReview?.comments || []"
      :submitting="submittingReview"
      @close="showReviewModal = false"
      @submit="handleSubmitReview"
      @discard="handleDiscardPendingReview"
      @delete-pending-comment="handleDeletePendingComment"
    />
  </div>
</template>

<script setup lang="ts">
 import { ref, onMounted, onUnmounted, computed, toRef } from 'vue';
 import { usePRDetail } from '../composables/usePRDetail';
 import { useCondensedRail } from '../composables/useCondensedRail';
 import { useFileDeepLink } from '../composables/useFileDeepLink';
 import { useRequestedReviewer } from '../composables/useRequestedReviewer';
 import { useUserPreferences } from '../composables/useUserPreferences';
 import { useSignalR, type CommentNotification, type ThreadNotification, type CheckRunsNotification, type PRPriorityNotification } from '../composables/useSignalR';
  import { apiService } from '../services/api';
  import CommentsPanel from '../components/CommentsPanel.vue';
  import PrDetailHeader from '../components/pr-detail-header.vue';
  import PrConversation from '../components/pr-conversation.vue';
  import PrContextRail from '../components/pr-context-rail.vue';
  import PrFiles from '../components/pr-files.vue';
  import PrReviewModal from '../components/pr-review-modal.vue';
  import type { Comment, MergeMethod } from '../types';
  import { useAuthStore } from '../stores/auth';
  import { useToast } from '../composables/useToast';

 const authStore = useAuthStore();
 const signalR = useSignalR();
 const toast = useToast();

const props = defineProps<{
  id: number;
}>();

const {
  prDetail,
  loading,
  error,
  commentsPanel,
  fetchPRDetail,
  addPendingReviewComment,
  deletePendingReviewComment,
  addReply,
  submitReview,
  submitPendingReview,
  deletePendingReview,
  mergePR,
  publishDraftPR,
  toggleCommentsPanel,
  editComment,
  deleteComment,
  updatePR,
  addGeneralComment,
  updateGeneralComment,
  deleteGeneralComment,
} = usePRDetail();

const { preferences, loadPreferences, setCommentsPanelWidth, updatePreferences } = useUserPreferences();

const prId = toRef(props, 'id');
const { selectedFilePath, setSelectedFilePath } = useFileDeepLink(prId);
const { isRequestedReviewer, check: checkRequestedReviewer } = useRequestedReviewer(prId);

const filesRef = ref<InstanceType<typeof PrFiles> | null>(null);
const railRef = ref<InstanceType<typeof PrContextRail> | null>(null);

// The rail's sentinel only exists once prDetail has loaded, so hand the observer a ref that
// resolves later rather than an element.
const railSentinel = computed(() => railRef.value?.sentinelRef ?? null);
const { condensed } = useCondensedRail(railSentinel);

const showReviewModal = ref(false);
const submittingReview = ref(false);
const publishingDraft = ref(false);
const merging = ref(false);
const mergeError = ref<string | null>(null);
const refreshingViewStates = ref(false);
const settingPriority = ref(false);

const savingTitle = ref(false);

// pr-detail-header measures itself into --pr-header-h; panels below stick under it.
// 5rem is the app header (`sticky top-20`); the fallback keeps layout sane before first measure.
const stickyTop = 'calc(5rem + var(--pr-header-h, 6.75rem))';

// Resizable width (the file tree owns its own width inside pr-files)
const commentsPanelWidth = ref(320);
const isResizingComments = ref(false);

onMounted(async () => {
  if (!authStore.isAuthenticated) {
    return;
  }

  await loadPreferences();
  commentsPanelWidth.value = preferences.value.commentsPanelWidth;

  await fetchPRDetail(props.id);

  await refreshFileViewStates();

  if (preferences.value.viewedFilesByPr && preferences.value.viewedFilesByPr[props.id]) {
    if (prDetail.value) {
      prDetail.value.viewedFiles = preferences.value.viewedFilesByPr[props.id];
    }
  }

  // Decides whether the description starts collapsed. Resolved while the loading state still
  // covers the page, so the description never visibly folds after paint.
  await checkRequestedReviewer();

  document.addEventListener('mousemove', handleMouseMove);
  document.addEventListener('mouseup', handleMouseUp);

  if (authStore.token) {
    await signalR.connect(authStore.token);
    await signalR.joinPRRoom(props.id);
    setupSignalRHandlers();
  }
});

const setupSignalRHandlers = () => {
  signalR.onCommentChanged.value = (notification: CommentNotification) => {
    if (notification.pullRequestId !== props.id || !prDetail.value) return;

    if (notification.action === 'added') {
      const exists = prDetail.value.allComments.some(c => c.id === notification.comment.id);
      if (!exists) {
        prDetail.value.allComments.push(notification.comment);
      }
    } else if (notification.action === 'updated') {
      const idx = prDetail.value.allComments.findIndex(c => c.id === notification.comment.id);
      if (idx !== -1) {
        prDetail.value.allComments[idx] = notification.comment;
      }
    } else if (notification.action === 'deleted') {
      prDetail.value.allComments = prDetail.value.allComments.filter(c => c.id !== notification.comment.id);
    }
  };

  signalR.onThreadChanged.value = (notification: ThreadNotification) => {
    if (notification.pullRequestId !== props.id || !prDetail.value) return;

    const threadIdx = prDetail.value.reviewThreads.findIndex(t => t.id === notification.threadId);
    if (threadIdx !== -1 && prDetail.value.reviewThreads[threadIdx]) {
      prDetail.value.reviewThreads[threadIdx].isResolved = notification.isResolved;
      prDetail.value.reviewThreads[threadIdx].state = notification.isResolved ? 'RESOLVED' : 'UNRESOLVED';
    }
  };

  signalR.onCheckRunsUpdated.value = (notification: CheckRunsNotification) => {
    if (notification.pullRequestId !== props.id || !prDetail.value) return;

    prDetail.value.checksStatus = notification.checksStatus as any;
    if (notification.checkRuns) {
      prDetail.value.checkRuns = notification.checkRuns.map(cr => ({
        id: cr.id,
        gitHubId: cr.gitHubId,
        name: cr.name,
        status: cr.status,
        conclusion: cr.conclusion,
        url: cr.url,
        startedAt: cr.startedAt,
        completedAt: cr.completedAt
      }));
    }
  };

  signalR.onPRPriorityChanged.value = (notification: PRPriorityNotification) => {
    if (notification.pullRequestId !== props.id || !prDetail.value) return;
    prDetail.value.priority = notification.priority;
    prDetail.value.priorityOverridden = notification.overridden;
  };
};

onUnmounted(async () => {
  document.removeEventListener('mousemove', handleMouseMove);
  document.removeEventListener('mouseup', handleMouseUp);

  await signalR.leavePRRoom(props.id);
  await signalR.disconnect();
});

// Resize handlers
const startResizeCommentsPanel = () => {
  isResizingComments.value = true;
};

const handleMouseMove = (e: MouseEvent) => {
  if (isResizingComments.value) {
    const newWidth = Math.max(250, Math.min(800, window.innerWidth - e.clientX));
    commentsPanelWidth.value = newWidth;
  }
};

const handleMouseUp = async () => {
  if (isResizingComments.value) {
    await setCommentsPanelWidth(commentsPanelWidth.value);
    isResizingComments.value = false;
  }
};

  const handleAddComment = async (path: string, line: number, body: string, side: string = 'RIGHT') => {
    await addPendingReviewComment(props.id, { path, line, body, side });
  };

  const handleReplyToThread = async (threadId: string, _line: number, body: string) => {
    await addReply(props.id, { reviewThreadId: threadId, body });
  };

  const handleResolveThread = async (threadId: string, resolved: boolean) => {
    try {
      if (resolved) {
        await apiService.resolveReviewThread(props.id, threadId, true);
      } else {
        await apiService.unresolveReviewThread(props.id, threadId);
      }

      if (prDetail.value) {
        const thread = prDetail.value.reviewThreads.find(t => t.gitHubId === threadId);
        if (thread) {
          thread.isResolved = resolved;
          thread.state = resolved ? 'RESOLVED' : 'UNRESOLVED';
        }
      }
    } catch (error) {
      console.error('Failed to resolve/unresolve thread:', error);
    }
  };

  const handleReplyAdded = (comment: Comment) => {
    if (prDetail.value && comment) {
      const existingComment = prDetail.value.allComments.find(c => c.id === comment.id);
      if (!existingComment) {
        prDetail.value.allComments.push(comment);

        const thread = prDetail.value.reviewThreads.find(
          t => t.gitHubId === comment.reviewThreadId?.toString() || String(t.id) === comment.reviewThreadId?.toString()
        );
        if (thread) {
          thread.commentCount = (thread.commentCount || 0) + 1;
        }
      }
    }
  };

  const handleThreadResolved = (threadId: string, resolved: boolean) => {
    if (prDetail.value) {
      const thread = prDetail.value.reviewThreads.find(t => t.gitHubId === threadId);
      if (thread) {
        thread.isResolved = resolved;
        thread.state = resolved ? 'RESOLVED' : 'UNRESOLVED';
      }

      const hasUnresolved = prDetail.value.reviewThreads.some(t => !t.isResolved && !t.isOutdated);
      prDetail.value.hasUnresolvedThreads = hasUnresolved;
    }
  };

  const handleAddGeneralComment = async (body: string) => {
    await addGeneralComment(props.id, body);
  };

  const handleUpdateGeneralComment = async (gitHubId: string, body: string) => {
    await updateGeneralComment(props.id, gitHubId, body);
  };

  const handleDeleteGeneralComment = async (gitHubId: string) => {
    await deleteGeneralComment(props.id, gitHubId);
  };

  const handleReviewerError = (message: string) => {
    console.error('Reviewer error:', message);
  };

  const handlePriorityChange = async (value: string) => {
    if (!prDetail.value) return;
    const priority = value === 'auto' ? null : Number(value);
    settingPriority.value = true;
    try {
      const result = await apiService.setPRPriority(props.id, priority);
      prDetail.value.priority = result.priority;
      prDetail.value.priorityOverridden = result.overridden;
      toast.success(value === 'auto' ? 'Priority set to auto (derived)' : `Priority set to ${value === '0' ? 'Low' : value === '1' ? 'Normal' : value === '2' ? 'High' : 'Urgent'}`);
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      toast.error(error.response?.data?.message || 'Failed to set priority');
    } finally {
      settingPriority.value = false;
    }
  };

  const handleTimelineError = (message: string) => {
    console.error('Timeline error:', message);
  };


const handleSubmitReview = async (
  state: 'APPROVED' | 'CHANGES_REQUESTED' | 'COMMENT',
  body: string
) => {
  if (state !== 'APPROVED' && !body.trim()) {
    toast.error(
      state === 'CHANGES_REQUESTED'
        ? 'Please provide details about the changes you are requesting.'
        : 'Please provide a comment when submitting a comment review.'
    );
    return;
  }

  submittingReview.value = true;

  const payload = { state, body: body || undefined };
  const success = prDetail.value?.pendingReview && prDetail.value.pendingReview.comments.length > 0
    ? await submitPendingReview(props.id, payload)
    : await submitReview(props.id, payload);

  submittingReview.value = false;

  if (success) {
    showReviewModal.value = false;
    toast.success('Review submitted successfully');
  }
};

const handleDiscardPendingReview = async () => {
  if (!prDetail.value?.pendingReview) return;

  if (!confirm('Are you sure you want to discard your pending review? All draft comments will be deleted.')) {
    return;
  }

  const success = await deletePendingReview(props.id);
  if (success) {
    showReviewModal.value = false;
  }
};

const handleDeletePendingComment = async (commentId: string) => {
  await deletePendingReviewComment(props.id, commentId);
};

const handlePublishDraft = async () => {
  if (!prDetail.value?.draft) return;

  publishingDraft.value = true;
  const success = await publishDraftPR(props.id);
  publishingDraft.value = false;

  if (!success) {
    toast.error('Failed to publish draft PR');
  } else {
    toast.success('Draft PR published successfully');
  }
};

const unresolvedThreadsCount = computed(() => {
  if (!prDetail.value?.reviewThreads) return 0;
  return prDetail.value.reviewThreads.filter(t => !t.isResolved).length;
});

const unresolvedActiveThreadsCount = computed(() => {
  if (!prDetail.value?.reviewThreads) return 0;
  return prDetail.value.reviewThreads.filter(t => !t.isResolved && !t.isOutdated).length;
});

const handleMerge = async (method: MergeMethod) => {
  merging.value = true;
  mergeError.value = null;

  const result = await mergePR(props.id, { mergeMethod: method });

  merging.value = false;

  if (!result.success) {
    mergeError.value = result.message || 'Failed to merge PR';
    toast.error(result.message || 'Failed to merge PR');
  } else {
    toast.success('PR merged successfully');
  }
};

/** A chip in the condensed strip was clicked — scroll its rail card into view and flash it. */
const handleJumpTo = (cardId: string) => {
  railRef.value?.highlight(cardId);
};

const scrollToComment = (comment: Comment, line?: number) => {
  if (!comment.path) return;
  filesRef.value?.scrollToFile(comment.path, line ?? undefined);
};

const scrollToThread = (threadId: string, line?: number) => {
  filesRef.value?.scrollToThread(threadId, line ?? undefined);
};

const saveViewedFiles = async (filePath?: string, viewed?: boolean) => {
  if (!prDetail.value) return;

  const viewedFilesByPr = preferences.value.viewedFilesByPr || {};
  viewedFilesByPr[props.id] = prDetail.value.viewedFiles?.filter(f => f !== undefined) || [];

  await updatePreferences({ viewedFilesByPr });

  // Sync with GitHub using GraphQL
  if (filePath !== undefined && viewed !== undefined) {
    try {
      await apiService.updateFileViewedState(props.id, filePath, viewed);
    } catch (error) {
      console.error('Failed to sync viewed file to GitHub:', error);
    }
  }
};

const refreshFileViewStates = async () => {
  if (!prDetail.value || refreshingViewStates.value) return;

  refreshingViewStates.value = true;
  try {
    const updatedFiles = await apiService.refreshFileViewStates(props.id);

    if (prDetail.value) {
      // Merge the updated viewed states with existing files
      prDetail.value.files = prDetail.value.files.map(file => {
        const updatedFile = updatedFiles.find(f => f.path === file.path);
        if (updatedFile) {
          return {
            ...file,
            viewedState: updatedFile.viewedState,
            viewedAt: updatedFile.viewedAt
          };
        }
        return file;
      });
    }
  } catch (error) {
    console.error('Failed to refresh file viewed states:', error);
  } finally {
    refreshingViewStates.value = false;
  }
};

const handleToggleViewed = async (filePath: string, viewed: boolean) => {
  if (!prDetail.value) return;

  if (!prDetail.value.viewedFiles) {
    prDetail.value.viewedFiles = [];
  }

  if (viewed) {
    if (!prDetail.value.viewedFiles.includes(filePath)) {
      prDetail.value.viewedFiles.push(filePath);
    }
  } else {
    prDetail.value.viewedFiles = prDetail.value.viewedFiles.filter(f => f !== filePath);
  }

  // Update file object with viewedState
  const fileIndex = prDetail.value.files.findIndex(f => f.path === filePath);
  if (fileIndex !== -1) {
    prDetail.value.files[fileIndex] = {
      ...prDetail.value.files[fileIndex],
      viewed,
      viewedState: viewed ? 'VIEWED' : 'UNVIEWED',
      viewedAt: viewed ? new Date().toISOString() : null
    };
  }

  // Save to preferences and sync with GitHub
  await saveViewedFiles(filePath, viewed);
};

const handleSaveTitle = async (title: string) => {
  savingTitle.value = true;
  await updatePR(props.id, { title });
  savingTitle.value = false;
};

const handleDescriptionSave = async (body: string) => {
  await updatePR(props.id, { body });
};

const handleEditComment = async (commentId: number, body: string): Promise<void> => {
  await editComment(commentId, body);
};

const handleDeleteComment = async (commentId: number): Promise<void> => {
  await deleteComment(commentId);
};
</script>
