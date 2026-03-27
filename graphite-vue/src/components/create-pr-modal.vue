<template>
  <Teleport to="body">
    <Transition name="modal">
      <div
        v-if="isOpen"
        class="fixed inset-0 z-50 overflow-y-auto"
        @keydown.esc="handleClose"
      >
        <div class="flex min-h-screen items-center justify-center p-4">
          <div
            class="fixed inset-0 bg-black/70 backdrop-blur-sm transition-opacity"
            @click="handleClose"
          />

          <div class="relative bg-slate-900 rounded-xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-hidden flex flex-col border border-slate-700/50">
            <div class="flex items-center justify-between px-5 py-3 border-b border-slate-700/50">
              <div class="flex items-center gap-2">
                <svg class="w-5 h-5 text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
                <h2 class="text-lg font-semibold text-white">Create Pull Request</h2>
              </div>
              <button
                @click="handleClose"
                class="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-700/50 transition-colors"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <div class="flex-1 overflow-y-auto p-5 space-y-4">
              <div v-if="error" class="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-sm">
                {{ error }}
              </div>

              <div>
                <label class="block text-xs font-medium text-slate-400 mb-1.5">Repository</label>
                <div class="relative">
                  <select
                    v-model="selectedRepository"
                    :disabled="loadingRepositories"
                    class="w-full px-3 py-2 bg-slate-800 border border-slate-700 rounded-lg text-white text-sm focus:ring-2 focus:ring-green-500/50 focus:border-green-500/50 disabled:opacity-50 transition-all appearance-none cursor-pointer"
                  >
                    <option :value="null" disabled>Select a repository</option>
                    <option v-for="repo in repositories" :key="repo.id" :value="repo">
                      {{ repo.fullName }}
                    </option>
                  </select>
                  <div class="absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none flex items-center gap-2">
                    <svg v-if="loadingRepositories" class="w-4 h-4 animate-spin text-slate-400" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>
                    <svg v-else class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                    </svg>
                  </div>
                </div>
              </div>

              <div class="flex items-center gap-2">
                <div class="flex-1">
                  <label class="block text-xs font-medium text-slate-400 mb-1.5">Head</label>
                  <div class="relative">
                    <select
                      v-model="selectedHeadBranch"
                      :disabled="!selectedRepository || loadingBranches"
                      class="w-full px-3 py-2 bg-slate-800 border border-slate-700 rounded-lg text-white text-sm focus:ring-2 focus:ring-green-500/50 focus:border-green-500/50 disabled:opacity-50 transition-all appearance-none cursor-pointer"
                    >
                      <option value="" disabled>Select</option>
                      <option v-for="branch in branches" :key="branch.sha" :value="branch.name">
                        {{ branch.name }}
                      </option>
                    </select>
                    <svg class="absolute right-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400 pointer-events-none" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                    </svg>
                  </div>
                </div>

                <div class="flex items-end pb-2.5">
                  <svg class="w-5 h-5 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6" />
                  </svg>
                </div>

                <div class="flex-1">
                  <label class="block text-xs font-medium text-slate-400 mb-1.5">Base</label>
                  <div class="relative">
                    <select
                      v-model="selectedBaseBranch"
                      :disabled="!selectedRepository || loadingBranches"
                      class="w-full px-3 py-2 bg-slate-800 border border-slate-700 rounded-lg text-white text-sm focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500/50 disabled:opacity-50 transition-all appearance-none cursor-pointer"
                    >
                      <option value="" disabled>Select</option>
                      <option v-for="branch in branches" :key="branch.sha" :value="branch.name">
                        {{ branch.name }}
                      </option>
                    </select>
                    <svg class="absolute right-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400 pointer-events-none" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                    </svg>
                  </div>
                </div>
              </div>

              <div v-if="loadingBranches && selectedRepository" class="flex items-center justify-center py-3">
                <svg class="w-5 h-5 animate-spin text-slate-400" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
              </div>

              <div v-if="loadingComparison" class="flex items-center justify-center py-4">
                <svg class="w-6 h-6 animate-spin text-green-500" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
              </div>

              <Transition name="slide">
                <div v-if="comparison && !loadingComparison" class="border border-slate-700/50 rounded-lg overflow-hidden">
                  <div class="px-3 py-2 bg-slate-800/50 border-b border-slate-700/50">
                    <div class="flex items-center justify-between text-sm">
                      <div class="flex items-center gap-3">
                        <span class="text-slate-300">
                          <span class="font-semibold text-white">{{ comparison.aheadBy }}</span> commits
                        </span>
                        <span class="text-slate-600">|</span>
                        <span class="flex items-center gap-2">
                          <span class="text-green-400">+{{ totalAdditions }}</span>
                          <span class="text-red-400">-{{ totalDeletions }}</span>
                          <span class="text-slate-400">in {{ comparison.files.length }} files</span>
                        </span>
                      </div>
                      <span v-if="comparison.aheadBy === 0" class="text-amber-400 text-xs">No changes</span>
                    </div>
                  </div>

                  <div v-if="comparison.commits.length > 0" class="max-h-32 overflow-y-auto divide-y divide-slate-700/30">
                    <div
                      v-for="commit in comparison.commits"
                      :key="commit.sha"
                      class="px-3 py-2 hover:bg-slate-700/20 transition-colors"
                    >
                      <p class="text-sm text-slate-200 truncate">{{ commit.message.split('\n')[0] }}</p>
                      <p class="text-xs text-slate-500 mt-0.5">
                        <span class="font-mono">{{ commit.sha.slice(0, 7) }}</span> · {{ commit.author }} · {{ formatTime(commit.authoredAt) }}
                      </p>
                    </div>
                  </div>
                </div>
              </Transition>

              <div class="space-y-3">
                <div>
                  <label class="block text-xs font-medium text-slate-400 mb-1.5">Title</label>
                  <input
                    v-model="title"
                    type="text"
                    placeholder="Pull request title"
                    class="w-full px-3 py-2 bg-slate-800 border border-slate-700 rounded-lg text-white text-sm placeholder-slate-500 focus:ring-2 focus:ring-green-500/50 focus:border-green-500/50 transition-all"
                  />
                </div>

                <div>
                  <label class="block text-xs font-medium text-slate-400 mb-1.5">Description <span class="text-slate-600">(optional)</span></label>
                  <textarea
                    v-model="body"
                    rows="3"
                    placeholder="Add a description..."
                    class="w-full px-3 py-2 bg-slate-800 border border-slate-700 rounded-lg text-white text-sm placeholder-slate-500 focus:ring-2 focus:ring-green-500/50 focus:border-green-500/50 transition-all resize-none"
                  />
                </div>

                <label class="flex items-center gap-2 cursor-pointer">
                  <input
                    v-model="isDraft"
                    type="checkbox"
                    class="w-4 h-4 rounded border-slate-600 bg-slate-700 text-green-500 focus:ring-green-500/50 focus:ring-2"
                  />
                  <span class="text-sm text-slate-300">Create as draft</span>
                </label>
              </div>
            </div>

            <div class="flex items-center justify-end gap-2 px-5 py-3 border-t border-slate-700/50 bg-slate-800/30">
              <button
                @click="handleClose"
                class="px-4 py-2 rounded-lg text-slate-300 hover:text-white hover:bg-slate-700/50 transition-colors text-sm"
              >
                Cancel
              </button>
              <button
                @click="handleCreate"
                :disabled="!canCreate || !hasChanges"
                class="px-4 py-2 rounded-lg bg-green-600 hover:bg-green-500 disabled:bg-slate-600 disabled:cursor-not-allowed text-white text-sm font-medium transition-colors flex items-center gap-2"
              >
                <svg v-if="creating" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                {{ isDraft ? 'Create Draft' : 'Create PR' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { watch } from 'vue';
import { useCreatePR } from '../composables/useCreatePR';

const props = defineProps<{
  isOpen: boolean;
}>();

const emit = defineEmits<{
  close: [];
  created: [pr: { id: number; url: string }];
}>();

const {
  repositories,
  branches,
  comparison,
  selectedRepository,
  selectedBaseBranch,
  selectedHeadBranch,
  title,
  body,
  isDraft,
  loadingRepositories,
  loadingBranches,
  loadingComparison,
  creating,
  error,
  canCreate,
  hasChanges,
  totalAdditions,
  totalDeletions,
  fetchRepositories,
  createPullRequest,
  reset
} = useCreatePR();

const handleClose = () => {
  reset();
  emit('close');
};

const handleCreate = async () => {
  const result = await createPullRequest();
  if (result?.success && result.pullRequest) {
    emit('created', {
      id: result.pullRequest.id,
      url: result.pullRequest.url
    });
    handleClose();
  }
};

const formatTime = (dateStr: string) => {
  const date = new Date(dateStr);
  const now = new Date();
  const diff = now.getTime() - date.getTime();
  const minutes = Math.floor(diff / 60000);
  const hours = Math.floor(minutes / 60);
  const days = Math.floor(hours / 24);

  if (days > 0) return `${days}d ago`;
  if (hours > 0) return `${hours}h ago`;
  if (minutes > 0) return `${minutes}m ago`;
  return 'just now';
};

watch(() => props.isOpen, (isOpen) => {
  if (isOpen) {
    fetchRepositories();
  }
});
</script>

<style scoped>
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}

.modal-enter-active .relative,
.modal-leave-active .relative {
  transition: transform 0.2s ease, opacity 0.2s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-from .relative,
.modal-leave-to .relative {
  transform: scale(0.95);
  opacity: 0;
}

.slide-enter-active,
.slide-leave-active {
  transition: all 0.2s ease;
}

.slide-enter-from,
.slide-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
</style>
