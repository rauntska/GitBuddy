<template>
  <div class="min-h-screen bg-slate-950 text-slate-200">
    <!-- Header / Navigation -->
    <div class="sticky top-0 z-20 bg-slate-900/95 backdrop-blur-sm border-b border-slate-700/50">
      <div class="max-w-screen-2xl mx-auto px-4 py-3">
        <div class="flex items-center gap-3">
          <router-link
            to="/"
            class="p-1.5 hover:bg-slate-800 rounded transition-colors"
            title="Back to Dashboard"
          >
            <svg class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </router-link>
          
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2">
              <StatusBadge v-if="prDetail" :status="prDetail.status" />
              <h1 class="text-lg font-semibold text-slate-100 truncate">
                {{ prDetail?.title || 'Loading...' }}
              </h1>
            </div>
            <div v-if="prDetail" class="flex items-center gap-2 mt-0.5 text-xs text-slate-400">
              <span>{{ prDetail.repository }} #{{ prDetail.gitHubId }}</span>
              <span>•</span>
              <span>{{ prDetail.author }}</span>
              <span>•</span>
              <span class="flex items-center gap-1">
                <span class="text-green-400">{{ prDetail.sourceBranch }}</span>
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                </svg>
                <span class="text-blue-400">{{ prDetail.targetBranch }}</span>
              </span>
            </div>
          </div>

          <!-- Action Buttons -->
          <div class="flex items-center gap-2">
            <button
              @click="toggleCommentsPanel"
              :class="[
                'flex items-center gap-1.5 px-3 py-1.5 rounded text-sm border transition-colors',
                commentsPanel
                  ? 'bg-blue-600 border-blue-500 text-white'
                  : 'bg-slate-800 border-slate-700 text-slate-300 hover:bg-slate-700'
              ]"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                      d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
              </svg>
              <span>{{ prDetail?.allComments?.length || 0 }}</span>
            </button>

            <a
              v-if="prDetail"
              :href="prDetail.url"
              target="_blank"
              rel="noopener noreferrer"
              class="flex items-center gap-1.5 px-3 py-1.5 bg-slate-800 border border-slate-700 rounded hover:bg-slate-700 text-slate-300 text-sm transition-colors"
            >
              <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
              </svg>
              <span>GitHub</span>
            </a>
          </div>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="text-center">
        <svg class="animate-spin h-12 w-12 mx-auto text-blue-500 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <p class="text-slate-400">Loading PR details...</p>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="flex items-center justify-center py-20">
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

    <!-- Main Content -->
    <div v-else-if="prDetail" class="flex h-[calc(100vh-64px)]">
      <!-- File Tree Sidebar -->
      <div
        v-if="fileTreeVisible"
        class="w-64 flex-shrink-0 overflow-auto bg-slate-900/50 border-r border-slate-700/50"
      >
        <FileTree
          :files="prDetail.files"
          :selected-file="selectedFile"
          @select-file="scrollToFile"
        />
      </div>

      <!-- Main Content Area -->
      <div class="flex-1 overflow-auto bg-slate-950">
        <div class="max-w-7xl mx-auto px-4 py-4 space-y-3">
          <!-- PR Description -->
          <div class="p-4 bg-slate-900/50 border border-slate-700/50 rounded-lg">
            <h2 class="text-sm font-semibold text-slate-200 mb-2">Description</h2>
            <div v-if="prDetail.description" class="text-sm text-slate-300 whitespace-pre-wrap leading-relaxed">
              {{ prDetail.description }}
            </div>
            <p v-else class="text-sm text-slate-500 italic">No description provided</p>
          </div>

          <!-- PR Stats - Compact Row -->
          <div class="grid grid-cols-4 gap-2">
            <div class="p-3 bg-slate-900/50 border border-slate-700/50 rounded-lg">
              <div class="text-xs text-slate-400">Files</div>
              <div class="text-xl font-semibold text-slate-200">{{ prDetail.changedFiles }}</div>
            </div>
            <div class="p-3 bg-slate-900/50 border border-slate-700/50 rounded-lg">
              <div class="text-xs text-slate-400">Additions</div>
              <div class="text-xl font-semibold text-green-400">+{{ prDetail.additions }}</div>
            </div>
            <div class="p-3 bg-slate-900/50 border border-slate-700/50 rounded-lg">
              <div class="text-xs text-slate-400">Deletions</div>
              <div class="text-xl font-semibold text-red-400">-{{ prDetail.deletions }}</div>
            </div>
            <div class="p-3 bg-slate-900/50 border border-slate-700/50 rounded-lg">
              <div class="text-xs text-slate-400">Comments</div>
              <div class="text-xl font-semibold text-blue-400">{{ prDetail.allComments.length }}</div>
            </div>
          </div>

          <!-- Reviews Section - Compact -->
          <div v-if="prDetail.reviews.length > 0" class="p-4 bg-slate-900/50 border border-slate-700/50 rounded-lg">
            <h2 class="text-sm font-semibold text-slate-200 mb-3">Reviews</h2>
            <div class="space-y-2">
              <div
                v-for="review in prDetail.reviews"
                :key="review.id"
                class="flex items-center gap-2 p-2 bg-slate-800/50 rounded border border-slate-700/50"
              >
                <img
                  v-if="review.reviewerAvatar"
                  :src="review.reviewerAvatar"
                  :alt="review.reviewer"
                  class="w-7 h-7 rounded-full"
                />
                <div class="flex-1 min-w-0">
                  <div class="text-sm font-medium text-slate-200">{{ review.reviewer }}</div>
                  <div class="text-xs text-slate-500">{{ review.submittedAt ? formatDate(review.submittedAt) : 'Pending' }}</div>
                </div>
                <span
                  :class="[
                    'px-2 py-1 rounded text-xs font-medium',
                    review.state === 'APPROVED' ? 'bg-green-900/30 text-green-400 border border-green-700/50' :
                    review.state === 'CHANGES_REQUESTED' ? 'bg-red-900/30 text-red-400 border border-red-700/50' :
                    'bg-slate-700/50 text-slate-300 border border-slate-600/50'
                  ]"
                >
                  {{ review.state.replace('_', ' ') }}
                </span>
              </div>
            </div>
          </div>

          <!-- Actions Bar - Compact -->
          <div class="p-3 bg-slate-900/50 border border-slate-700/50 rounded-lg">
            <div class="flex items-center justify-between">
              <div class="text-sm text-slate-400">
                Submit your review
              </div>
              <div class="flex gap-2">
                <button
                  @click="showReviewModal = true; reviewAction = 'COMMENT'"
                  class="px-3 py-1.5 bg-slate-700/50 hover:bg-slate-700 border border-slate-600/50 rounded text-sm text-slate-200 transition-colors"
                >
                  Comment
                </button>
                <button
                  @click="showReviewModal = true; reviewAction = 'APPROVED'"
                  class="px-3 py-1.5 bg-green-600/90 hover:bg-green-600 border border-green-500/50 rounded text-sm text-white transition-colors"
                >
                  Approve
                </button>
                <button
                  @click="showReviewModal = true; reviewAction = 'CHANGES_REQUESTED'"
                  class="px-3 py-1.5 bg-red-600/90 hover:bg-red-600 border border-red-500/50 rounded text-sm text-white transition-colors"
                >
                  Request Changes
                </button>
              </div>
            </div>
          </div>

          <!-- Files Changed Header -->
          <div class="flex items-center justify-between mb-3">
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
              <span>{{ fileTreeVisible ? 'Hide' : 'Show' }} Tree</span>
            </button>
          </div>

          <!-- File Diffs -->
          <div class="space-y-3">
            <FileDiffViewer
              v-for="file in prDetail.files"
              :key="file.path"
              :ref="el => setFileRef(file.path, el)"
              :file="file"
              :comments="prDetail.allComments"
              :on-add-comment="(line: number, body: string) => handleAddComment(file.path, line, body)"
            />
          </div>

          <!-- Keyboard Shortcuts Help -->
          <div class="mt-6 p-3 bg-slate-900/50 border border-slate-700/50 rounded-lg">
            <div class="text-xs text-slate-400 mb-2 font-medium">Keyboard Shortcuts</div>
            <div class="grid grid-cols-2 gap-2 text-xs text-slate-500">
              <div><kbd class="px-1.5 py-0.5 bg-slate-800/50 rounded text-slate-400">c</kbd> Toggle comments</div>
              <div><kbd class="px-1.5 py-0.5 bg-slate-800/50 rounded text-slate-400">f</kbd> Toggle file tree</div>
              <div><kbd class="px-1.5 py-0.5 bg-slate-800/50 rounded text-slate-400">j</kbd> Next file</div>
              <div><kbd class="px-1.5 py-0.5 bg-slate-800/50 rounded text-slate-400">k</kbd> Previous file</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Comments Panel -->
      <Transition
        enter-active-class="transition-transform duration-200"
        enter-from-class="translate-x-full"
        enter-to-class="translate-x-0"
        leave-active-class="transition-transform duration-200"
        leave-from-class="translate-x-0"
        leave-to-class="translate-x-full"
      >
        <div v-if="commentsPanel" class="w-80 flex-shrink-0">
          <CommentsPanel
            :comments="prDetail.allComments"
            @close="toggleCommentsPanel"
            @scroll-to-comment="scrollToComment"
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
          <h3 class="text-base font-semibold text-slate-200 mb-3">
            {{ reviewAction === 'APPROVED' ? 'Approve PR' : reviewAction === 'CHANGES_REQUESTED' ? 'Request Changes' : 'Add Comment' }}
          </h3>
          <textarea
            v-model="reviewComment"
            placeholder="Add your review comment (optional)..."
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
import { ref, onMounted, onUnmounted, nextTick } from 'vue';
import { useRoute } from 'vue-router';
import { usePRDetail } from '../composables/usePRDetail';
import FileDiffViewer from '../components/FileDiffViewer.vue';
import FileTree from '../components/FileTree.vue';
import CommentsPanel from '../components/CommentsPanel.vue';
import StatusBadge from '../components/StatusBadge.vue';
import type { Comment } from '../types';

const props = defineProps<{
  id: number;
}>();

const route = useRoute();
const {
  prDetail,
  loading,
  error,
  commentsPanel,
  fileTreeVisible,
  fetchPRDetail,
  addComment,
  submitReview,
  toggleCommentsPanel,
  toggleFileTree,
} = usePRDetail();

const selectedFile = ref<string | null>(null);
const showReviewModal = ref(false);
const reviewAction = ref<'APPROVED' | 'CHANGES_REQUESTED' | 'COMMENT'>('COMMENT');
const reviewComment = ref('');
const submittingReview = ref(false);
const fileRefs = ref<Map<string, any>>(new Map());

onMounted(async () => {
  await fetchPRDetail(props.id);
  
  // Setup keyboard shortcuts
  document.addEventListener('keydown', handleKeyPress);
});

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeyPress);
});

const handleKeyPress = (e: KeyboardEvent) => {
  // Ignore if user is typing in an input
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
  
  scrollToFile(prDetail.value.files[nextIndex].path);
};

const formatDate = (dateString: string): string => {
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};
</script>
