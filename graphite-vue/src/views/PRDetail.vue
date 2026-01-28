<template>
  <div class="flex flex-col bg-slate-950 text-slate-200">
    <!-- Compact Header -->
    <div class="bg-slate-900/95 border-b border-slate-700/50">
      <div class="px-4 py-2 flex items-center gap-3">
        <router-link
          to="/"
          class="p-1 hover:bg-slate-800 rounded transition-colors"
          title="Back to Dashboard"
        >
          <svg class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </router-link>

        <div class="flex-1 min-w-0 flex items-center gap-2">
          <StatusBadge v-if="prDetail" :status="prDetail.status" />
          <h1 class="text-base font-semibold text-slate-100 truncate">
            {{ prDetail?.title || 'Loading...' }}
          </h1>
        </div>

        <div class="flex items-center gap-3 text-xs">
          <span class="text-slate-400">{{ prDetail?.repository }} #{{ prDetail?.gitHubId }}</span>

          <button
            @click="showReviewModal = true"
            class="px-3 py-1.5 bg-blue-600 hover:bg-blue-700 rounded text-xs text-white transition-colors font-medium"
          >
            Review
          </button>

          <button
            @click="toggleCommentsPanel"
            :class="[
              'p-1.5 rounded relative transition-colors',
              commentsPanel ? 'bg-blue-600 text-white' : 'bg-slate-700/50 hover:bg-slate-700 text-slate-300'
            ]"
            title="Comments"
          >
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M18 10c0 3.866-3.582 7-8 7a8.841 8.841 0 01-4.083-.98L2 17l1.338-3.123C2.493 12.767 2 11.434 2 10c0-3.866 3.582-7 8-7s8 3.134 8 7zM7 9H5v2h2V9zm8 0h-2v2h2V9zM9 9h2v2H9V9z" clip-rule="evenodd" />
            </svg>
            <span
              v-if="prDetail?.allComments.length > 0"
              class="absolute -top-1 -right-1 bg-blue-500 text-white text-[10px] font-bold rounded-full min-w-[16px] h-4 flex items-center justify-center px-0.5"
            >
              {{ prDetail.allComments.length }}
            </span>
          </button>

          <a
            :href="prDetail?.url"
            target="_blank"
            rel="noopener noreferrer"
            class="px-3 py-1.5 bg-slate-700/50 hover:bg-slate-700 border border-slate-600 rounded text-xs text-slate-200 transition-colors"
          >
            GitHub
          </a>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center flex-1">
      <div class="text-center">
        <svg class="animate-spin h-12 w-12 mx-auto text-blue-500 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <p class="text-slate-400">Loading PR details...</p>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="flex items-center justify-center flex-1">
      <div class="text-center max-w-md">
        <svg class="w-16 h-16 mx-auto text-red-500 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <h2 class="text-xl font-semibold text-slate-200 mb-2">Failed to load PR</h2>
        <p class="text-slate-400 mb-4">{{ error }}</p>
        <button
          @click="fetchPRDetail(props.id)"
          class="px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded text-white transition-colors"
        >
          Retry
        </button>
      </div>
    </div>

    <!-- Main Content - Full Page Layout -->
    <div v-else-if="prDetail" class="flex flex-1">
      <!-- File Tree Sidebar (Resizable) -->
      <div
        v-if="preferences.fileTreeVisible"
        :style="{ width: `${fileTreeWidth}px`, height: '100vh' }"
        class="flex-shrink-0 overflow-auto bg-slate-900/50 border-r border-slate-700/50 relative sticky top-0"
      >
        <FileTree
          :files="prDetail.files"
          :selected-file="selectedFile || undefined"
          @select-file="scrollToFile"
        />
        <!-- Resize Handle -->
        <div
          class="absolute top-0 right-0 w-1 h-full cursor-ew-resize hover:bg-blue-500/50 transition-colors"
          @mousedown="startResizeFileTree"
        />
      </div>

      <!-- Main Content Area -->
      <div class="flex-1 flex flex-col">
        <!-- Top Section: PR Info -->
        <div class="border-b border-slate-700/50">
          <div class="flex gap-4 p-4">
            <!-- Left: Description (Largest) -->
            <div class="flex-1 min-w-0">
              <!-- Branch Info -->
              <div class="p-3 bg-slate-900/50 border border-slate-700/50 rounded-lg mb-3">
                <div class="text-xs text-slate-400 mb-1">Branches</div>
                <div class="flex items-center gap-1 text-sm">
                  <span class="text-green-400">{{ prDetail.sourceBranch }}</span>
                  <svg class="w-3 h-3 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                  </svg>
                  <span class="text-blue-400">{{ prDetail.targetBranch }}</span>
                </div>
              </div>

              <!-- Description -->
              <div class="p-3 bg-slate-900/50 border border-slate-700/50 rounded-lg">
                <div class="text-xs font-semibold text-slate-200 mb-2">Description</div>
                <div v-if="prDetail.description" class="text-xs text-slate-300 whitespace-pre-wrap leading-relaxed max-h-48 overflow-y-auto">
                  {{ prDetail.description }}
                </div>
                <p v-else class="text-xs text-slate-500 italic">No description provided</p>
              </div>
            </div>

            <!-- Right: Info & Stats -->
            <div class="w-80 flex-shrink-0 space-y-3">
              <!-- Stats - Compact Table -->
              <div class="p-3 bg-slate-900/50 border border-slate-700/50 rounded-lg">
                <div class="text-xs text-slate-400 mb-2">Stats</div>
                <table class="w-full text-xs">
                  <tr>
                    <td class="text-slate-500 py-0.5">Files</td>
                    <td class="text-slate-200 text-right font-medium">{{ prDetail.changedFiles }}</td>
                  </tr>
                  <tr>
                    <td class="text-slate-500 py-0.5">Comments</td>
                    <td class="text-blue-400 text-right font-medium">{{ prDetail.allComments.length }}</td>
                  </tr>
                  <tr>
                    <td class="text-slate-500 py-0.5">Additions</td>
                    <td class="text-green-400 text-right font-medium">+{{ prDetail.additions }}</td>
                  </tr>
                  <tr>
                    <td class="text-slate-500 py-0.5">Deletions</td>
                    <td class="text-red-400 text-right font-medium">-{{ prDetail.deletions }}</td>
                  </tr>
                </table>
              </div>

              <!-- Reviewers - Compact with Icons -->
              <div v-if="prDetail.reviews.length > 0" class="p-3 bg-slate-900/50 border border-slate-700/50 rounded-lg">
                <div class="text-xs text-slate-400 mb-2">Reviewers</div>
                <div class="space-y-1.5">
                  <div
                    v-for="review in prDetail.reviews"
                    :key="review.id"
                    class="flex items-center gap-2"
                  >
                    <img
                      v-if="review.reviewerAvatar"
                      :src="review.reviewerAvatar"
                      :alt="review.reviewer"
                      class="w-5 h-5 rounded-full"
                    />
                    <span class="text-xs text-slate-200 flex-1 truncate">{{ review.reviewer }}</span>
                    <span
                      :class="[
                        'text-xs',
                        review.state === 'APPROVED' ? 'text-green-400' :
                        review.state === 'CHANGES_REQUESTED' ? 'text-red-400' :
                        review.state === 'COMMENTED' ? 'text-blue-400' :
                        'text-slate-500'
                      ]"
                    >
                      <svg v-if="review.state === 'APPROVED'" class="w-3 h-3 inline" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
                      </svg>
                      <svg v-else-if="review.state === 'CHANGES_REQUESTED'" class="w-3 h-3 inline" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
                      </svg>
                      <svg v-else-if="review.state === 'COMMENTED'" class="w-3 h-3 inline" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M18 10c0 3.866-3.582 7-8 7a8.841 8.841 0 01-4.083-.98L2 17l1.338-3.123C2.493 12.767 2 11.434 2 10c0-3.866 3.582-7 8-7s8 3.134 8 7zM7 9H5v2h2V9zm8 0h-2v2h2V9zM9 9h2v2H9V9z" clip-rule="evenodd" />
                      </svg>
                      <span v-else class="opacity-0">•</span>
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Bottom Section: File Diffs -->
        <div class="flex-1">
          <div class="px-4 py-4">
            <div class="space-y-3">
               <!-- Files Changed Header -->
              <div class="flex items-center justify-between py-2">
                <h2 class="text-sm font-semibold text-slate-200">
                  Files Changed ({{ prDetail.files.length }})
                </h2>
                <button
                  @click="toggleFileTree"
                  class="flex items-center gap-1.5 px-2 py-1 bg-slate-800/50 border border-slate-700/50 rounded hover:bg-slate-800 text-slate-300 text-xs transition-colors"
                >
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                          d="M4 6h16M4 12h16M4 18h16" />
                  </svg>
                  <span>{{ preferences.fileTreeVisible ? 'Hide' : 'Show' }} Tree</span>
                </button>
              </div>

                <!-- File Diffs -->
                <FileDiffViewer
                  v-for="file in prDetail.files"
                  :key="file.path"
                  :ref="el => setFileRef(file.path, el)"
                  :file="file"
                  :viewed="isFileViewed(file.path!)"
                  :comments="prDetail.allComments"
                  :on-add-comment="(line: number, body: string) => handleAddComment(file.path!, line, body)"
                  :on-toggle-viewed="handleToggleViewed"
                  :initial-expanded="!isFileViewed(file.path!)"
                />
            </div>
          </div>
        </div>
      </div>

      <!-- Comments Panel (Resizable) -->
      <Transition
        enter-active-class="transition-transform duration-200"
        enter-from-class="translate-x-full"
        enter-to-class="translate-x-0"
        leave-active-class="transition-transform duration-200"
        leave-from-class="translate-x-0"
        leave-to-class="translate-x-full"
      >
        <div
          v-if="commentsPanel"
          :style="{ width: `${commentsPanelWidth}px` }"
          class="flex-shrink-0 relative"
        >
          <CommentsPanel
            :comments="prDetail.allComments"
            @close="toggleCommentsPanel"
            @scroll-to-comment="scrollToComment"
          />
          <!-- Resize Handle -->
          <div
            class="absolute top-0 left-0 w-1 h-full cursor-ew-resize hover:bg-blue-500/50 transition-colors"
            @mousedown="startResizeCommentsPanel"
          />
        </div>
      </Transition>
    </div>

    <!-- Review Modal -->
    <Transition
      enter-active-class="transition-opacity duration-200"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-opacity duration-200"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="showReviewModal"
        class="fixed inset-0 bg-black/70 backdrop-blur-sm flex items-center justify-center z-50 p-4"
        @click.self="showReviewModal = false"
      >
        <div class="bg-slate-900 border border-slate-700/50 rounded-lg p-5 max-w-lg w-full shadow-2xl">
          <h3 class="text-base font-semibold text-slate-200 mb-3">Review PR</h3>

          <div class="grid grid-cols-3 gap-2 mb-3">
            <button
              @click="reviewAction = 'APPROVED'"
              :class="[
                'px-2 py-2 rounded text-xs font-medium transition-colors border',
                reviewAction === 'APPROVED'
                  ? 'bg-green-600 border-green-500 text-white'
                  : 'bg-slate-800/50 border-slate-700 hover:bg-slate-800 text-slate-300'
              ]"
            >
              Approve
            </button>
            <button
              @click="reviewAction = 'CHANGES_REQUESTED'"
              :class="[
                'px-2 py-2 rounded text-xs font-medium transition-colors border',
                reviewAction === 'CHANGES_REQUESTED'
                  ? 'bg-red-600 border-red-500 text-white'
                  : 'bg-slate-800/50 border-slate-700 hover:bg-slate-800 text-slate-300'
              ]"
            >
              Changes
            </button>
            <button
              @click="reviewAction = 'COMMENT'"
              :class="[
                'px-2 py-2 rounded text-xs font-medium transition-colors border',
                reviewAction === 'COMMENT'
                  ? 'bg-blue-600 border-blue-500 text-white'
                  : 'bg-slate-800/50 border-slate-700 hover:bg-slate-800 text-slate-300'
              ]"
            >
              Comment
            </button>
          </div>

          <textarea
            v-model="reviewComment"
            :placeholder="reviewAction === 'APPROVED' ? 'Add your approval comment (optional)...' : reviewAction === 'CHANGES_REQUESTED' ? 'Describe the changes requested...' : 'Add your comment (optional)...'"
            class="w-full px-3 py-2 bg-slate-800/50 border border-slate-700 rounded text-slate-200 text-sm resize-none focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500 mb-3"
            rows="4"
          />
          <div class="flex gap-2 justify-end">
            <button
              @click="showReviewModal = false"
              class="px-3 py-1.5 bg-slate-700/50 hover:bg-slate-700 rounded text-sm text-slate-200 transition-colors"
            >
              Cancel
            </button>
            <button
              @click="handleSubmitReview"
              :disabled="submittingReview"
              :class="[
                'px-3 py-1.5 rounded text-sm text-white transition-colors',
                reviewAction === 'APPROVED' ? 'bg-green-600/90 hover:bg-green-600' :
                reviewAction === 'CHANGES_REQUESTED' ? 'bg-red-600/90 hover:bg-red-600' :
                'bg-blue-600/90 hover:bg-blue-600',
                { 'opacity-50 cursor-not-allowed': submittingReview }
              ]"
            >
              {{ submittingReview ? 'Submitting...' : 'Submit' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { usePRDetail } from '../composables/usePRDetail';
import { useUserPreferences } from '../composables/useUserPreferences';
import FileDiffViewer from '../components/FileDiffViewer.vue';
import FileTree from '../components/FileTree.vue';
import CommentsPanel from '../components/CommentsPanel.vue';
import StatusBadge from '../components/StatusBadge.vue';
import type { Comment } from '../types';

const props = defineProps<{
  id: number;
}>();

const {
  prDetail,
  loading,
  error,
  commentsPanel,
  fetchPRDetail,
  addComment,
  submitReview,
  toggleCommentsPanel,
  toggleFileTree,
} = usePRDetail();

const { preferences, loadPreferences, setFileTreeWidth, setCommentsPanelWidth, updatePreferences } = useUserPreferences();

const selectedFile = ref<string | null>(null);
const showReviewModal = ref(false);
const reviewAction = ref<'APPROVED' | 'CHANGES_REQUESTED' | 'COMMENT'>('COMMENT');
const reviewComment = ref('');
const submittingReview = ref(false);
const fileRefs = ref<Map<string, any>>(new Map());

// Resizable widths
const fileTreeWidth = ref(256);
const commentsPanelWidth = ref(320);
const isResizingFileTree = ref(false);
const isResizingComments = ref(false);

onMounted(async () => {
  await loadPreferences();
  fileTreeWidth.value = preferences.value.fileTreeWidth;
  commentsPanelWidth.value = preferences.value.commentsPanelWidth;
  
  await fetchPRDetail(props.id);

  // Load viewed files from preferences
  if (preferences.value.viewedFilesByPr && preferences.value.viewedFilesByPr[props.id]) {
    if (prDetail.value) {
      prDetail.value.viewedFiles = preferences.value.viewedFilesByPr[props.id];
    }
  }
  
  document.addEventListener('keydown', handleKeyPress);
  document.addEventListener('mousemove', handleMouseMove);
  document.addEventListener('mouseup', handleMouseUp);
});

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeyPress);
  document.removeEventListener('mousemove', handleMouseMove);
  document.removeEventListener('mouseup', handleMouseUp);
});

// Resize handlers
const startResizeFileTree = () => {
  isResizingFileTree.value = true;
};

const startResizeCommentsPanel = () => {
  isResizingComments.value = true;
};

const handleMouseMove = (e: MouseEvent) => {
  if (isResizingFileTree.value) {
    const newWidth = Math.max(200, Math.min(600, e.clientX));
    fileTreeWidth.value = newWidth;
  }
  if (isResizingComments.value) {
    const newWidth = Math.max(250, Math.min(800, window.innerWidth - e.clientX));
    commentsPanelWidth.value = newWidth;
  }
};

const handleMouseUp = async () => {
  if (isResizingFileTree.value) {
    await setFileTreeWidth(fileTreeWidth.value);
    isResizingFileTree.value = false;
  }
  if (isResizingComments.value) {
    await setCommentsPanelWidth(commentsPanelWidth.value);
    isResizingComments.value = false;
  }
};

const handleKeyPress = (e: KeyboardEvent) => {
  if ((e.target as HTMLElement).tagName === 'TEXTAREA' || (e.target as HTMLElement).tagName === 'INPUT') {
    return;
  }

  switch (e.key.toLowerCase()) {
    case 'c':
      toggleCommentsPanel();
      break;
    case 'f':
      toggleFileTree();
      break;
    case 'j':
      navigateFile('next');
      break;
    case 'k':
      navigateFile('prev');
      break;
  }
};

const handleAddComment = async (path: string, line: number, body: string) => {
  const comment = await addComment(props.id, { path, line, body });
  if (comment) {
    console.log('Comment added successfully');
  }
};

const handleSubmitReview = async () => {
  submittingReview.value = true;
  const success = await submitReview(props.id, {
    state: reviewAction.value,
    body: reviewComment.value || undefined,
  });
  submittingReview.value = false;
  
  if (success) {
    showReviewModal.value = false;
    reviewComment.value = '';
  }
};

const scrollToFile = (path: string) => {
  selectedFile.value = path;
  const fileRef = fileRefs.value.get(path);
  if (fileRef && fileRef.$el) {
    fileRef.$el.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
};

const scrollToComment = (comment: Comment) => {
  if (comment.path) {
    scrollToFile(comment.path);
  }
};

const setFileRef = (path: string, el: any) => {
  if (el) {
    fileRefs.value.set(path, el);
  }
};

const navigateFile = (direction: 'next' | 'prev') => {
  if (!prDetail.value) return;
  
  const currentIndex = selectedFile.value 
    ? prDetail.value.files.findIndex(f => f.path === selectedFile.value)
    : -1;
  
  const nextIndex = direction === 'next' 
    ? (currentIndex + 1) % prDetail.value.files.length
    : (currentIndex - 1 + prDetail.value.files.length) % prDetail.value.files.length;
  
  const nextFile = prDetail.value.files[nextIndex];
  if (nextFile) {
    scrollToFile(nextFile.path);
  }
};

const saveViewedFiles = async () => {
  if (!prDetail.value) return;

  const viewedFilesByPr = preferences.value.viewedFilesByPr || {};
  viewedFilesByPr[props.id] = prDetail.value.viewedFiles || [];

  await updatePreferences({ viewedFilesByPr });

  // Sync with GitHub using GraphQL
  try {
    await syncViewedFilesToGitHub(props.id, prDetail.value.viewedFiles || []);
  } catch (error) {
    console.error('Failed to sync viewed files to GitHub:', error);
  }
};

const syncViewedFilesToGitHub = async (prId: number, viewedFiles: string[]) => {
  // TODO: Implement GitHub GraphQL mutation to mark files as viewed
  // This would use a GitHub API endpoint or GraphQL mutation to store viewed state
  console.log('Syncing viewed files to GitHub for PR', prId, ':', viewedFiles);
};

const isFileViewed = (filePath: string): boolean => {
  if (!prDetail.value?.viewedFiles) return false;
  return prDetail.value.viewedFiles.includes(filePath);
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

  // Update file object
  const fileIndex = prDetail.value.files.findIndex(f => f.path === filePath);
  if (fileIndex !== -1) {
    prDetail.value.files[fileIndex] = {
      ...prDetail.value.files[fileIndex],
      viewed
    };
  }

  // Save to preferences and sync with GitHub
  await saveViewedFiles();
};
</script>
