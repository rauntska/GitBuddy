<template>
  <div class="border border-slate-700/30 rounded-xl bg-gradient-to-b from-slate-900/80 to-slate-950/80 shadow-lg mb-4">
    <FileDiffHeader
      :path="file.path || ''"
      :status="file.status"
      :additions="file.additions || 0"
      :deletions="file.deletions || 0"
      :expanded="expanded"
      :viewed="file.viewedState === 'VIEWED' || file.viewed === true"
      @toggle="onHeaderClick"
      @toggle-viewed="toggleViewed"
    />

    <div v-if="expanded && !loading" class="relative bg-slate-950/50 backdrop-blur">
      <div v-if="hunks.length === 0" class="p-8 text-center">
        <div class="flex flex-col items-center gap-3">
          <svg class="w-12 h-12 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          <p class="text-slate-400 text-sm">No diff content available</p>
        </div>
      </div>

      <template v-if="hunks.length > 0">
        <DiffMinimap
          :comments="fileComments"
          :hunks="hunks"
          @scroll-to="scrollToLine"
        />

        <div v-if="viewMode === 'split'" class="overflow-x-auto text-xs font-mono" ref="diffContainer">
          <table class="w-full border-collapse table-fixed">
            <colgroup>
              <col style="width: 56px;">
              <col style="width: 32px;">
              <col style="width: calc(50% - 117px);">
              <col style="width: 56px;">
              <col style="width: 32px;">
              <col style="width: calc(50% - 59px);">
            </colgroup>
            <tbody>
              <template v-if="hunks.length > 0">
                <tr v-if="canExpandBefore && fileContent" class="bg-slate-900/50">
                  <td colspan="6" class="px-4 py-2">
                    <button
                      @click="handleExpand('before')"
                      :disabled="expandingPositions.has('before-0')"
                      class="flex items-center justify-center gap-2 w-full py-1.5 text-xs text-slate-400 hover:text-slate-200 hover:bg-slate-700/50 rounded transition-colors disabled:opacity-50"
                    >
                      <svg v-if="!expandingPositions.has('before-0')" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                      </svg>
                      <svg v-else class="w-4 h-4 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                      </svg>
                      <span>Expand up to 25 lines</span>
                    </button>
                  </td>
                </tr>
              </template>

              <template v-for="(hunk, hunkIndex) in hunks" :key="hunkIndex">
                <tr class="bg-gradient-to-r from-slate-800/40 to-slate-800/30 border-y border-slate-700/30">
                  <td colspan="6" class="px-4 py-2 text-slate-400 text-xs font-mono">
                    <span class="text-slate-500">@</span>@ -{{ hunk.oldStart }},{{ hunk.oldLines }} <span class="text-blue-400">+</span>{{ hunk.newStart }},{{ hunk.newLines }} <span class="text-slate-500">@</span>@
                  </td>
                </tr>

                <template v-for="(row, rowIndex) in getAlignedRows(hunk, hunkIndex)" :key="`${hunkIndex}-${rowIndex}`">
                  <DiffLineRow
                    :row="row"
                    :highlighted-line="highlightedLine"
                    :render-line-content="renderAlignedLineContent"
                    :set-line-ref="setLineRef"
                    @add-comment="(line, side) => startComment(line!, side)"
                  />

                  <tr v-if="row.leftLine?.lineNumber && (getCommentsForLine(row.leftLine.lineNumber, 'left').length > 0 || (commentingLine === row.leftLine.lineNumber && commentingSide === 'left'))">
                    <td colspan="3" class="p-0 bg-gradient-to-b from-slate-900/50 to-slate-950/30 border-t border-slate-700/20">
                      <div v-if="getCommentsForLine(row.leftLine.lineNumber, 'left').length > 0" class="p-2">
                        <CommentThread
                          v-for="[threadId, comments] in getCommentsGroupedByThread(row.leftLine.lineNumber, 'left')"
                          :key="threadId || 'standalone-' + comments[0]?.id"
                          :comments="comments"
                          :thread-id="threadId"
                          :thread-info="getThreadInfo(threadId)"
                          :current-username="currentUsername"
                          :pending-replies="getPendingRepliesForThread(getThreadInfo(threadId)?.gitHubId)"
                          :editing-comment-id="editingCommentId"
                          :replying-to-comment-id="replyingToCommentId"
                          :deleting-comment-id="deletingCommentId"
                          :reply-error="replyErrors.get(replyingToCommentId ?? 0)"
                          :is-submitting="isSubmitting"
                          :expanded="isThreadExpanded(threadId)"
                          @toggle-thread="toggleThreadExpanded"
                          @resolve-thread="handleResolveThread"
                          @start-edit="startEditComment"
                          @cancel-edit="cancelEdit"
                          @submit-edit="submitEdit"
                          @start-reply="(id) => startReplyToComment(id, getThreadInfo(threadId)?.gitHubId ?? null)"
                          @cancel-reply="cancelReply"
                          @submit-reply="submitReply"
                          @delete-comment="handleDeleteComment"
                          @delete-pending-comment="handleDeletePendingComment"
                        />
                      </div>
                      <CommentForm
                        v-if="commentingLine === row.leftLine.lineNumber && commentingSide === 'left'"
                        v-model="newCommentText"
                        :error="commentError"
                        @submit="submitNewComment"
                        @cancel="cancelNewComment"
                      />
                    </td>
                  </tr>

                  <tr v-if="row.rightLine?.lineNumber && (getCommentsForLine(row.rightLine.lineNumber, 'right').length > 0 || (commentingLine === row.rightLine.lineNumber && commentingSide === 'right') || getPendingCommentsForLine(row.rightLine.lineNumber).length > 0)">
                    <td colspan="3" class="p-0"></td>
                    <td colspan="3" class="p-0 bg-gradient-to-b from-slate-900/50 to-slate-950/30 border-t border-slate-700/20">
                      <div v-if="getCommentsForLine(row.rightLine.lineNumber, 'right').length > 0" class="p-2">
                        <CommentThread
                          v-for="[threadId, comments] in getCommentsGroupedByThread(row.rightLine.lineNumber, 'right')"
                          :key="threadId || 'standalone-' + comments[0]?.id"
                          :comments="comments"
                          :thread-id="threadId"
                          :thread-info="getThreadInfo(threadId)"
                          :current-username="currentUsername"
                          :pending-replies="getPendingRepliesForThread(getThreadInfo(threadId)?.gitHubId)"
                          :editing-comment-id="editingCommentId"
                          :replying-to-comment-id="replyingToCommentId"
                          :deleting-comment-id="deletingCommentId"
                          :reply-error="replyErrors.get(replyingToCommentId ?? 0)"
                          :is-submitting="isSubmitting"
                          :expanded="isThreadExpanded(threadId)"
                          @toggle-thread="toggleThreadExpanded"
                          @resolve-thread="handleResolveThread"
                          @start-edit="startEditComment"
                          @cancel-edit="cancelEdit"
                          @submit-edit="submitEdit"
                          @start-reply="(id) => startReplyToComment(id, getThreadInfo(threadId)?.gitHubId ?? null)"
                          @cancel-reply="cancelReply"
                          @submit-reply="submitReply"
                          @delete-comment="handleDeleteComment"
                          @delete-pending-comment="handleDeletePendingComment"
                        />
                      </div>

                      <div v-if="getPendingCommentsForLine(row.rightLine.lineNumber).length > 0" class="p-4 space-y-3 border-t border-slate-700/20">
                        <div
                          v-for="pendingComment in getPendingCommentsForLine(row.rightLine.lineNumber)"
                          :key="pendingComment.gitHubId"
                          class="flex gap-3 p-3 bg-amber-900/10 border border-amber-700/30 rounded-lg"
                        >
                          <img
                            v-if="pendingComment.authorAvatar"
                            :src="pendingComment.authorAvatar"
                            :alt="pendingComment.author"
                            class="w-7 h-7 rounded-full flex-shrink-0 ring-2 ring-amber-700/50"
                          />
                          <div class="flex-1 min-w-0">
                            <div class="flex items-center gap-2 mb-1">
                              <span class="font-medium text-slate-200 text-sm">{{ pendingComment.author }}</span>
                              <span class="px-1.5 py-0.5 text-xs bg-amber-900/30 text-amber-400 rounded">Pending</span>
                            </div>
                            <div class="text-sm text-slate-300 whitespace-pre-wrap">{{ pendingComment.body }}</div>
                          </div>
                          <button
                            @click="handleDeletePendingComment(pendingComment.gitHubId)"
                            class="p-1 hover:bg-slate-700/50 rounded transition-colors"
                            title="Delete pending comment"
                          >
                            <svg class="w-4 h-4 text-rose-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                            </svg>
                          </button>
                        </div>
                      </div>

                      <CommentForm
                        v-if="commentingLine === row.rightLine.lineNumber && commentingSide === 'right'"
                        v-model="newCommentText"
                        :error="commentError"
                        @submit="submitNewComment"
                        @cancel="cancelNewComment"
                      />
                    </td>
                  </tr>
                </template>

                <template v-if="hunkIndex < hunks.length - 1 && getGapInfo(hunkIndex) && fileContent">
                  <tr class="bg-slate-900/50">
                    <td colspan="6" class="px-4 py-2">
                      <button
                        @click="handleExpand('between', hunkIndex)"
                        :disabled="expandingPositions.has(`between-${hunkIndex}`)"
                        class="flex items-center justify-center gap-2 w-full py-1.5 text-xs text-slate-400 hover:text-slate-200 hover:bg-slate-700/50 rounded transition-colors disabled:opacity-50"
                      >
                        <svg v-if="!expandingPositions.has(`between-${hunkIndex}`)" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                        </svg>
                        <svg v-else class="w-4 h-4 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                        </svg>
                        <span>Expand {{ getGapInfo(hunkIndex)?.oldLineCount ?? getGapInfo(hunkIndex)?.newLineCount ?? 0 }} lines</span>
                      </button>
                    </td>
                  </tr>
                </template>
              </template>

              <template v-if="hunks.length > 0 && canExpandAfter && fileContent">
                <tr class="bg-slate-900/50">
                  <td colspan="6" class="px-4 py-2">
                    <button
                      @click="handleExpand('after')"
                      :disabled="expandingPositions.has('after-0')"
                      class="flex items-center justify-center gap-2 w-full py-1.5 text-xs text-slate-400 hover:text-slate-200 hover:bg-slate-700/50 rounded transition-colors disabled:opacity-50"
                    >
                      <svg v-if="!expandingPositions.has('after-0')" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                      </svg>
                      <svg v-else class="w-4 h-4 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                      </svg>
                      <span>Expand up to 25 lines</span>
                    </button>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>
      </template>
    </div>

    <OrphanedComments
      v-if="expanded && !loading"
      :comments="orphanedComments"
      :review-threads="fileReviewThreads"
      :current-username="currentUsername"
      :editing-comment-id="editingCommentId"
      :replying-to-comment-id="replyingToCommentId"
      :deleting-comment-id="deletingCommentId"
      :expanded-threads="expandedThreads"
      :is-submitting="isSubmitting"
      @toggle-thread="toggleThreadExpanded"
      @resolve-thread="handleResolveThread"
      @start-edit="startEditComment"
      @cancel-edit="cancelEdit"
      @submit-edit="submitEdit"
      @start-reply="startReplyToComment"
      @cancel-reply="cancelReply"
      @submit-reply="submitReply"
      @delete-comment="handleDeleteComment"
    />

    <div v-if="loading" class="p-8 text-center bg-slate-950/30">
      <div class="flex flex-col items-center gap-3">
        <svg class="animate-spin h-8 w-8 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135-5.824 3-7.938l3 2.647z"></path>
        </svg>
        <span class="text-sm text-slate-400">Loading diff...</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, nextTick, onMounted, watch } from 'vue';
import type { FileDiff, Comment, ReviewThread, DiffHunk, AlignedRow, AlignedLine, ExpandPosition, PendingReviewComment } from '../types';
import { parsePatch, alignDiffLines, renderInlineDiffSegments, calculateExpandRange, getGapBetweenHunks, mergeExpandedLines } from '../utils/diffHelpers';
import { highlightCode, detectLanguageFromPath } from '../utils/syntaxHighlight';
import { useUserPreferences } from '../composables/useUserPreferences';
import { useFileContent } from '../composables/useFileContent';
import { useCommentActions, getPendingCommentsForLine as getPendingForLine, getPendingRepliesForThread as getPendingReplies } from '../composables/use-comment-actions';
import { useThreadActions, getCommentsForLine as getLineComments, getCommentsGroupedByThread as getGroupedThreads } from '../composables/use-thread-actions';
import FileDiffHeader from './file-diff-header.vue';
import DiffMinimap from './diff-minimap.vue';
import DiffLineRow from './diff-line-row.vue';
import CommentThread from './comment-thread.vue';
import CommentForm from './comment-form.vue';
import OrphanedComments from './orphaned-comments.vue';

const props = defineProps<{
  file: FileDiff;
  comments: Comment[];
  reviewThreads: ReviewThread[];
  pendingReviewComments?: PendingReviewComment[];
  currentUsername?: string;
  onAddComment: (line: number, body: string, side: string) => Promise<void>;
  onDeletePendingComment?: (commentId: string) => Promise<void>;
  onReplyToThread?: (threadId: string, line: number, body: string) => Promise<void>;
  onResolveThread?: (threadId: string, resolved: boolean) => Promise<void>;
  onEditComment?: (commentId: number, body: string) => Promise<void>;
  onDeleteComment?: (commentId: number) => Promise<void>;
  initialExpanded?: boolean;
  viewed?: boolean;
  onToggleViewed?: (path: string, viewed: boolean) => void;
  prId?: number;
}>();

const emit = defineEmits<{
  toggleViewed: [path: string, viewed: boolean];
}>();

const { preferences } = useUserPreferences();

const expanded = ref(props.file.viewedState !== 'VIEWED' && !props.file.viewed);

watch(() => [props.file.viewedState, props.file.viewed] as const, ([newViewedState, newViewed]) => {
  expanded.value = newViewedState !== 'VIEWED' && newViewed !== true;
}, { deep: true, immediate: true });

const loading = ref(false);
const hunks = ref<DiffHunk[]>([]);
const expandedBefore = ref(false);
const expandedAfter = ref(false);
const expandingPositions = ref<Set<string>>(new Set());
const lineRefs = ref<Map<number, HTMLElement>>(new Map());
const language = ref(props.file.path ? detectLanguageFromPath(props.file.path) : 'text');
const highlightedLine = ref<number | null>(null);
const alignedRowsCache = ref<Map<number, AlignedRow[]>>(new Map());

const fileContent = props.prId ? useFileContent(props.prId) : null;

const {
  commentingLine,
  commentingSide,
  newCommentText,
  commentError,
  editingCommentId,
  replyingToCommentId,
  replyErrors,
  deletingCommentId,
  isSubmitting,
  startComment,
  cancelNewComment,
  submitNewComment,
  startEditComment,
  cancelEdit,
  submitEdit,
  startReplyToComment,
  cancelReply,
  submitReply,
  handleDeleteComment,
  handleDeletePendingComment,
} = useCommentActions({
  currentUsername: props.currentUsername,
  onAddComment: props.onAddComment,
  onReplyToThread: props.onReplyToThread,
  onEditComment: props.onEditComment,
  onDeleteComment: props.onDeleteComment,
  onDeletePendingComment: props.onDeletePendingComment,
});

const {
  expandedThreads,
  getThreadInfo,
  toggleThreadExpanded,
  handleResolveThread,
  isThreadExpanded,
} = useThreadActions({
  reviewThreads: props.reviewThreads,
  onResolveThread: props.onResolveThread,
});

const canExpandBefore = computed(() => {
  if (hunks.value.length === 0 || expandedBefore.value) return false;
  const firstHunk = hunks.value[0];
  return (firstHunk?.oldStart ?? 0) > 1 || (firstHunk?.newStart ?? 0) > 1;
});

const canExpandAfter = computed(() => {
  if (hunks.value.length === 0 || expandedAfter.value) return false;
  return true;
});

const visibleLineNumbers = computed(() => {
  const lines = new Set<number>();
  hunks.value.forEach(hunk => {
    hunk.lines.forEach(line => {
      if (line.newLineNumber) lines.add(line.newLineNumber);
      if (line.oldLineNumber) lines.add(line.oldLineNumber);
    });
  });
  return lines;
});

const fileComments = computed(() =>
  props.comments.filter(c => c.path === props.file.path && c.line)
);

const fileReviewThreads = computed(() =>
  props.reviewThreads.filter(rt => rt.path === props.file.path)
);

const orphanedComments = computed(() => {
  return fileComments.value.filter(comment => {
    if (!comment.line) return false;
    return !visibleLineNumbers.value.has(comment.line);
  });
});

const viewMode = computed(() => preferences.value.diffViewMode);

const getAlignedRows = (hunk: DiffHunk, hunkIndex: number): AlignedRow[] => {
  if (alignedRowsCache.value.has(hunkIndex)) {
    return alignedRowsCache.value.get(hunkIndex)!;
  }
  const rows = alignDiffLines(hunk.lines);
  alignedRowsCache.value.set(hunkIndex, rows);
  return rows;
};

const renderAlignedLineContent = (line: AlignedLine | undefined): string => {
  if (!line || line.type === 'spacer') {
    return '';
  }
  if (line.inlineDiff && line.inlineDiff.length > 0) {
    return renderInlineDiffSegments(line.inlineDiff);
  }
  return highlightSyntax(line.content);
};

const highlightSyntax = (code: string): string => {
  return highlightCode(code, language.value);
};

const getCommentsForLine = (line: number | undefined, side?: 'left' | 'right'): Comment[] => {
  return getLineComments(props.comments, props.file.path || '', line, props.reviewThreads, side);
};

const getCommentsGroupedByThread = (line: number | undefined, side?: 'left' | 'right') => {
  return getGroupedThreads(props.comments, line, props.file.path || '', props.reviewThreads, side);
};

const getPendingCommentsForLine = (line: number | undefined): PendingReviewComment[] => {
  return getPendingForLine(props.pendingReviewComments || [], props.file.path || '', line);
};

const getPendingRepliesForThread = (threadGitId: string | undefined): PendingReviewComment[] => {
  return getPendingReplies(props.pendingReviewComments || [], threadGitId);
};

const loadHunks = async () => {
  if (hunks.value.length === 0) {
    const patchData = (props.file as any).patch || (props.file as any).Patch || (props.file as any).diff;
    if (patchData) {
      loading.value = true;
      await new Promise(resolve => setTimeout(resolve, 100));
      hunks.value = parsePatch(patchData);
      loading.value = false;
    } else {
      loading.value = false;
    }
  }
};

onMounted(() => {
  if (expanded.value) {
    loadHunks();
  }
});

watch(expanded, (newValue) => {
  if (newValue) {
    loadHunks();
  }
});

watch(hunks, () => {
  alignedRowsCache.value.clear();
}, { deep: true });

const onHeaderClick = () => {
  expanded.value = !expanded.value;
};

const toggleViewed = (viewed: boolean) => {
  if (props.file.path) {
    emit('toggleViewed', props.file.path, viewed);
    if (props.onToggleViewed) {
      props.onToggleViewed(props.file.path, viewed);
    }
  }
};

const setLineRef = (lineNumber: number | undefined, el: HTMLElement | null) => {
  if (lineNumber && el) {
    lineRefs.value.set(lineNumber, el);
  }
};

const scrollToLine = (line: number | undefined) => {
  if (!line) return;
  const el = lineRefs.value.get(line);
  if (el) {
    el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    highlightLine(line);
  }
};

const highlightLine = (lineNumber: number) => {
  highlightedLine.value = lineNumber;
  setTimeout(() => {
    highlightedLine.value = null;
  }, 5000);
};

const scrollToThread = async (threadGitId: string) => {
  const thread = fileReviewThreads.value.find(rt => rt.gitHubId === threadGitId);
  if (!thread || !thread.line) return;

  if (!expandedThreads.value.has(threadGitId)) {
    expandedThreads.value.add(threadGitId);
  }

  await nextTick();
  scrollToLine(thread.line);
};

const handleExpand = async (position: ExpandPosition, hunkIndex?: number) => {
  if (!fileContent || !props.file.path) return;

  const index = hunkIndex ?? 0;
  const positionKey = `${position}-${index}`;
  
  if (expandingPositions.value.has(positionKey)) return;
  expandingPositions.value.add(positionKey);

  try {
    const range = calculateExpandRange(hunks.value, position, index, 25);
    
    if (!range.oldStart && !range.newStart) {
      return;
    }

    const response = await fileContent.fetchFileContent(
      props.file.path,
      range.oldStart,
      range.oldEnd,
      range.newStart,
      range.newEnd
    );

    if (response) {
      hunks.value = mergeExpandedLines(
        hunks.value,
        response.oldLines,
        response.newLines,
        position,
        index
      );

      if (position === 'before') {
        expandedBefore.value = true;
      } else if (position === 'after') {
        expandedAfter.value = true;
      }

      alignedRowsCache.value.clear();
    }
  } finally {
    expandingPositions.value.delete(positionKey);
  }
};

const getGapInfo = (hunkIndex: number) => {
  return getGapBetweenHunks(hunks.value, hunkIndex);
};

defineExpose({
  highlightLine,
  expanded,
  scrollToThread,
});
</script>

<style scoped>
@import url('../styles/github-dark-syntax.css');
</style>
