<template>
  <div class="flex flex-1 min-w-0">
    <!-- File Tree Toggle Rail (always visible, left edge) -->
    <button
      type="button"
      @click="toggleFileTree"
      :style="{ top: railTop }"
      :class="[
        'hidden md:flex sticky self-start z-20 flex-shrink-0 group',
        'flex-col items-center justify-center gap-2',
        'w-9 py-4 mt-6 -ml-2',
        'rounded-l-lg border border-r-0',
        'transition-all duration-150',
        'shadow-lg shadow-black/30',
        preferences.fileTreeVisible
          ? 'bg-slate-800 border-slate-700 text-slate-200 hover:bg-slate-700 hover:-translate-x-0.5'
          : 'bg-slate-800/80 border-slate-700 text-slate-300 hover:bg-slate-700 hover:-translate-x-0.5'
      ]"
      :title="preferences.fileTreeVisible ? 'Hide file tree' : 'Show file tree'"
      :aria-label="preferences.fileTreeVisible ? 'Hide file tree' : 'Show file tree'"
      :aria-expanded="preferences.fileTreeVisible"
    >
      <span class="text-[10px] font-semibold tracking-widest uppercase rotate-180 [writing-mode:vertical-rl]">
        Files
      </span>
      <svg
        class="w-4 h-4 transition-transform duration-150 group-hover:scale-110"
        :class="preferences.fileTreeVisible ? 'text-blue-400' : 'text-slate-400'"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
        aria-hidden="true"
      >
        <path
          v-if="preferences.fileTreeVisible"
          stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M15 19l-7-7 7-7"
        />
        <path
          v-else
          stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M9 5l7 7-7 7"
        />
      </svg>
    </button>

    <div
      v-if="preferences.fileTreeVisible"
      :style="{ width: `${fileTreeWidth}px`, top: treeTop, height: treeHeight }"
      class="hidden md:block flex-shrink-0 bg-slate-900/40 border-r border-slate-800 relative sticky"
    >
      <FileTree
        :files="prDetail.files"
        :selected-file="selectedFile || undefined"
        :viewed-files="viewedFileSet"
        @select-file="handleSelectFile"
        @toggle-viewed="(path: string, viewed: boolean) => onToggleViewed(path, viewed)"
      />
      <!-- Resize Handle -->
      <div
        class="absolute top-0 right-0 w-1.5 h-full cursor-ew-resize bg-slate-800 hover:bg-slate-600 transition-colors duration-150"
        @mousedown="startResizeFileTree"
      />
    </div>

    <!-- Diffs -->
    <div class="flex-1 min-w-0">
      <div class="px-3 sm:px-6 py-4 sm:py-5">
        <div class="flex items-center justify-between py-2">
          <h2 class="text-sm font-semibold text-slate-300 uppercase tracking-wider">
            Files Changed <span class="text-slate-500 font-normal font-mono tabular-nums">({{ prDetail.files.length }})</span>
          </h2>
          <div class="flex items-center gap-2">
            <div class="relative" ref="settingsDropdownRef">
              <button
                @click="showSettingsDropdown = !showSettingsDropdown"
                class="p-2 border border-slate-800 rounded-lg hover:bg-slate-800 text-slate-300 transition-all duration-150"
                title="Diff settings"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
              </button>
              <Transition
                enter-active-class="transition-opacity duration-200"
                enter-from-class="opacity-0"
                enter-to-class="opacity-100"
                leave-active-class="transition-opacity duration-200"
                leave-from-class="opacity-100"
                leave-to-class="opacity-0"
              >
                <div
                  v-if="showSettingsDropdown"
                  class="absolute right-0 top-full mt-2 bg-slate-900 border border-slate-800 rounded-lg shadow-xl z-50 min-w-[220px] p-2"
                >
                  <div class="space-y-1">
                    <div class="text-xs font-semibold text-slate-400 px-3 py-2 uppercase tracking-wider">Diff View</div>
                    <button
                      @click="setDiffViewMode('unified'); showSettingsDropdown = false"
                      :class="[
                        'w-full flex items-center gap-3 px-3 py-2 rounded text-xs text-left transition-all duration-150',
                        preferences.diffViewMode === 'unified'
                          ? 'bg-slate-800 text-slate-100 border border-slate-700'
                          : 'text-slate-300 hover:bg-slate-800/60 border border-transparent'
                      ]"
                    >
                      <svg class="w-4 h-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
                      </svg>
                      Unified
                    </button>
                    <button
                      @click="setDiffViewMode('split'); showSettingsDropdown = false"
                      :class="[
                        'w-full flex items-center gap-3 px-3 py-2 rounded text-xs text-left transition-all duration-150',
                        preferences.diffViewMode === 'split'
                          ? 'bg-slate-800 text-slate-100 border border-slate-700'
                          : 'text-slate-300 hover:bg-slate-800/60 border border-transparent'
                      ]"
                    >
                      <svg class="w-4 h-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 4v16M4 4h16" />
                      </svg>
                      Split
                    </button>
                    <button
                      @click="toggleContext(); showSettingsDropdown = false"
                      :class="[
                        'w-full flex items-center gap-3 px-3 py-2 rounded text-xs text-left transition-all duration-150',
                        preferences.showContext
                          ? 'bg-slate-800 text-slate-100 border border-slate-700'
                          : 'text-slate-300 hover:bg-slate-800/60 border border-transparent'
                      ]"
                    >
                      <svg
                        v-if="preferences.showContext"
                        class="w-4 h-4 flex-shrink-0"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                      </svg>
                      <svg
                        v-else
                        class="w-4 h-4 flex-shrink-0"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542 7z" />
                      </svg>
                      {{ preferences.showContext ? 'Hide Context' : 'Show Context' }}
                    </button>
                  </div>
                </div>
              </Transition>
            </div>
          </div>
        </div>

        <!-- File Diffs -->
        <FileDiffViewer
          v-for="file in prDetail.files"
          :key="file.path"
          :ref="el => setFileRef(file.path!, el)"
          :file="file"
          :viewed="isFileViewed(file.path!)"
          :comments="prDetail.allComments"
          :review-threads="prDetail.reviewThreads"
          :pending-review-comments="prDetail.pendingReview?.comments"
          :current-username="currentUsername"
          :on-add-comment="(line: number, body: string, side: string) => onAddComment(file.path!, line, body, side)"
          :on-delete-pending-comment="onDeletePendingComment"
          :on-reply-to-thread="onReplyToThread"
          :on-resolve-thread="onResolveThread"
          :on-edit-comment="onEditComment"
          :on-delete-comment="onDeleteComment"
          :on-toggle-viewed="onToggleViewed"
          :initial-expanded="!isFileViewed(file.path!)"
          :pr-id="prDetail.id"
          class="mb-4"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import FileDiffViewer from './FileDiffViewer.vue';
import FileTree from './FileTree.vue';
import { useUserPreferences } from '../composables/useUserPreferences';
import { usePRDetail } from '../composables/usePRDetail';
import { useToast } from '../composables/useToast';
import type { PRDetail } from '../types';

const props = defineProps<{
  prDetail: PRDetail;
  currentUsername: string;
  initialFilePath?: string;
  /** Distance from the top of the viewport the sticky file tree should sit at, e.g. "8.5rem". */
  stickyTop: string;
  onAddComment: (path: string, line: number, body: string, side: string) => Promise<void>;
  onDeletePendingComment: (commentId: string) => Promise<void>;
  onReplyToThread: (threadId: string, line: number, body: string) => Promise<void>;
  onResolveThread: (threadId: string, resolved: boolean) => Promise<void>;
  onEditComment: (commentId: number, body: string) => Promise<void>;
  onDeleteComment: (commentId: number) => Promise<void>;
  onToggleViewed: (path: string, viewed: boolean) => Promise<void>;
}>();

const emit = defineEmits<{
  (e: 'select-file', path: string): void;
}>();

const toast = useToast();
const { toggleFileTree } = usePRDetail();
const { preferences, setFileTreeWidth, setDiffViewMode, setShowContext } = useUserPreferences();

const selectedFile = ref<string | null>(props.initialFilePath ?? null);
const showSettingsDropdown = ref(false);
const settingsDropdownRef = ref<HTMLElement | null>(null);
const fileRefs = ref<Map<string, any>>(new Map());

const fileTreeWidth = ref(preferences.value.fileTreeWidth);
const isResizingFileTree = ref(false);

// The file tree hangs below the app header + the PR header, both of which can change height.
const railTop = computed(() => `calc(${props.stickyTop} + 1.75rem)`);
const treeTop = computed(() => props.stickyTop);
const treeHeight = computed(() => `calc(100vh - ${props.stickyTop})`);

const isFileViewed = (filePath: string): boolean => {
  const file = props.prDetail.files.find(f => f.path === filePath);
  return file?.viewedState === 'VIEWED' || file?.viewed === true || (props.prDetail.viewedFiles?.includes(filePath) ?? false);
};

const viewedFileSet = computed(() => {
  const files = props.prDetail.files ?? [];
  const viewedPaths = new Set(props.prDetail.viewedFiles ?? []);

  return new Set(
    files
      .filter(f => f.path && (f.viewedState === 'VIEWED' || f.viewed === true || viewedPaths.has(f.path)))
      .map(f => f.path!)
  );
});

const setFileRef = (path: string, el: any) => {
  if (el) {
    fileRefs.value.set(path, el);
  }
};

const scrollToFile = (path: string, line?: number) => {
  selectedFile.value = path;
  const fileRef = fileRefs.value.get(path);
  if (fileRef && fileRef.$el) {
    // Ensure file is expanded
    if (line && fileRef.expanded !== undefined && !fileRef.expanded) {
      fileRef.expanded = true;
      // Wait for expansion before scrolling
      setTimeout(() => {
        fileRef.$el.scrollIntoView({ behavior: 'smooth', block: 'start' });
        if (fileRef.highlightLine) {
          fileRef.highlightLine(line);
        }
      }, 100);
    } else {
      fileRef.$el.scrollIntoView({ behavior: 'smooth', block: 'start' });
      if (line && fileRef.highlightLine) {
        fileRef.highlightLine(line);
      }
    }
  }
};

const scrollToThread = async (threadId: string, line?: number) => {
  const thread = props.prDetail.reviewThreads.find(rt => rt.gitHubId === threadId);
  if (!thread?.path) return;

  const fileRef = fileRefs.value.get(thread.path);
  if (fileRef) {
    if (fileRef.expanded !== undefined && !fileRef.expanded) {
      fileRef.expanded = true;
      await nextTick();
    }

    if (fileRef.scrollToThread) {
      await nextTick();
      fileRef.scrollToThread(threadId);
    } else {
      scrollToFile(thread.path, line ?? undefined);
    }
  }
};

const handleSelectFile = (path: string) => {
  scrollToFile(path);
  emit('select-file', path);
};

const applyDeepLink = async (path: string | undefined) => {
  if (!path) return;
  // A path that matches nothing (stale or hand-edited link) is ignored rather than erroring.
  if (!props.prDetail.files.some(f => f.path === path)) return;
  await nextTick();
  scrollToFile(path);
};

const toggleContext = async () => {
  try {
    await setShowContext(!preferences.value.showContext);
  } catch (error) {
    console.error('Error toggling context:', error);
    toast.error('Failed to toggle context. Please try again.');
  }
};

// Resize handling
const startResizeFileTree = () => {
  isResizingFileTree.value = true;
};

const handleMouseMove = (e: MouseEvent) => {
  if (isResizingFileTree.value) {
    fileTreeWidth.value = Math.max(200, Math.min(600, e.clientX));
  }
};

const handleMouseUp = async () => {
  if (isResizingFileTree.value) {
    await setFileTreeWidth(fileTreeWidth.value);
    isResizingFileTree.value = false;
  }
};

const handleClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  if (showSettingsDropdown.value && !settingsDropdownRef.value?.contains(target)) {
    showSettingsDropdown.value = false;
  }
};

onMounted(async () => {
  document.addEventListener('mousemove', handleMouseMove);
  document.addEventListener('mouseup', handleMouseUp);
  document.addEventListener('click', handleClickOutside);

  fileTreeWidth.value = preferences.value.fileTreeWidth;

  await applyDeepLink(props.initialFilePath);
});

onUnmounted(() => {
  document.removeEventListener('mousemove', handleMouseMove);
  document.removeEventListener('mouseup', handleMouseUp);
  document.removeEventListener('click', handleClickOutside);
});

watch(() => props.initialFilePath, (path) => {
  if (path && path !== selectedFile.value) applyDeepLink(path);
});

defineExpose({ scrollToFile, scrollToThread });
</script>
