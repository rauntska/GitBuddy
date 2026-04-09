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
                <h2 v-if="!showSuccess" class="text-lg font-semibold text-white">Create Pull Request</h2>
                <h2 v-else class="text-lg font-semibold text-white">Pull Request Created</h2>
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

            <div v-if="showSuccess && successResult" class="flex-1 overflow-y-auto p-5">
              <div class="flex flex-col items-center text-center py-8 space-y-4">
                <div class="w-16 h-16 rounded-full bg-green-500/20 flex items-center justify-center">
                  <svg class="w-8 h-8 text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                </div>
                <div>
                  <p class="text-white text-lg font-medium">Pull request created successfully!</p>
                  <p class="text-slate-400 text-sm mt-1">{{ title }}</p>
                </div>
                <div class="flex items-center gap-3 mt-4">
                  <button
                    @click="handleViewPR"
                    class="px-4 py-2 rounded-lg bg-green-600 hover:bg-green-500 text-white text-sm font-medium transition-colors flex items-center gap-2"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                    View PR
                  </button>
                  <a
                    v-if="successResult?.url"
                    :href="successResult.url"
                    target="_blank"
                    rel="noopener noreferrer"
                    class="px-4 py-2 rounded-lg bg-slate-700 hover:bg-slate-600 text-white text-sm font-medium transition-colors flex items-center gap-2"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                    </svg>
                    Open on GitHub
                  </a>
                </div>
              </div>
            </div>

            <div v-if="!showSuccess" class="flex-1 overflow-y-auto p-5 space-y-4">
              <div v-if="error" class="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-sm">
                {{ error }}
              </div>

              <div>
                <label class="block text-xs font-medium text-slate-400 mb-1.5">Repository</label>
                <div class="relative" ref="repoDropdownRef">
                  <input
                    ref="repoInputRef"
                    v-model="repoSearch"
                    type="text"
                    placeholder="Search repositories..."
                    :disabled="loadingRepositories"
                    class="w-full px-3 py-2 bg-slate-800 border border-slate-700 rounded-lg text-white text-sm placeholder-slate-500 focus:ring-2 focus:ring-green-500/50 focus:border-green-500/50 disabled:opacity-50 transition-all"
                    @focus="showRepoDropdown = true"
                    @keydown="handleRepoKeydown"
                  />
                  <div class="absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none flex items-center gap-2">
                    <svg v-if="loadingRepositories" class="w-4 h-4 animate-spin text-slate-400" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>
                  </div>
                  <Teleport to="body">
                    <div
                      v-if="showRepoDropdown && filteredRepos.length > 0"
                      class="fixed bg-slate-800 border border-slate-600 rounded-lg shadow-xl max-h-56 overflow-y-auto z-50"
                      :style="repoDropdownStyle"
                    >
                      <button
                        v-for="(repo, index) in filteredRepos"
                        :key="repo.id"
                        type="button"
                        :class="[
                          'w-full flex items-center gap-3 px-3 py-2 text-left transition-colors',
                          index === repoSelectedIndex ? 'bg-green-600/20' : 'hover:bg-slate-700/50'
                        ]"
                        @click="selectRepo(repo)"
                        @mouseenter="repoSelectedIndex = index"
                      >
                        <svg class="w-4 h-4 text-slate-400 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" />
                        </svg>
                        <span class="text-sm text-slate-200 truncate">{{ repo.fullName }}</span>
                        <span v-if="repo.private" class="text-xs px-1.5 py-0.5 rounded bg-yellow-500/20 text-yellow-400">Private</span>
                      </button>
                    </div>
                  </Teleport>
                </div>
                <div v-if="selectedRepository" class="mt-1.5 flex items-center gap-2 text-sm text-green-400">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                  {{ selectedRepository.fullName }}
                </div>
              </div>

              <div class="flex items-center gap-2">
                <div class="flex-1">
                  <label class="block text-xs font-medium text-slate-400 mb-1.5">Head</label>
                  <div class="relative" ref="headDropdownRef">
                    <input
                      v-model="headSearch"
                      type="text"
                      :placeholder="selectedRepository ? 'Search branches...' : 'Select repo first'"
                      :disabled="!selectedRepository || loadingBranches"
                      class="w-full px-3 py-2 bg-slate-800 border border-slate-700 rounded-lg text-white text-sm placeholder-slate-500 focus:ring-2 focus:ring-green-500/50 focus:border-green-500/50 disabled:opacity-50 transition-all"
                      @focus="showHeadDropdown = true"
                      @keydown="handleHeadKeydown"
                    />
                    <Teleport to="body">
                      <div
                        v-if="showHeadDropdown && filteredHeadBranches.length > 0"
                        class="fixed bg-slate-800 border border-slate-600 rounded-lg shadow-xl max-h-48 overflow-y-auto z-50"
                        :style="headDropdownStyle"
                      >
                        <button
                          v-for="(branch, index) in filteredHeadBranches"
                          :key="branch.name"
                          type="button"
                          :class="[
                            'w-full flex items-center gap-2 px-3 py-2 text-left transition-colors',
                            index === headSelectedIndex ? 'bg-green-600/20' : 'hover:bg-slate-700/50',
                            branch.name === selectedHeadBranch ? 'bg-green-600/10' : ''
                          ]"
                          @click="selectHeadBranch(branch.name)"
                          @mouseenter="headSelectedIndex = index"
                        >
                          <span class="text-sm text-slate-200">{{ branch.name }}</span>
                          <span v-if="branch.protected" class="text-xs text-yellow-400">
                            <svg class="w-3 h-3 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                            </svg>
                          </span>
                        </button>
                      </div>
                    </Teleport>
                  </div>
                </div>

                <div class="flex items-end pb-2.5">
                  <svg class="w-5 h-5 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6" />
                  </svg>
                </div>

                <div class="flex-1">
                  <label class="block text-xs font-medium text-slate-400 mb-1.5">Base</label>
                  <div class="relative" ref="baseDropdownRef">
                    <input
                      v-model="baseSearch"
                      type="text"
                      :placeholder="selectedRepository ? 'Search branches...' : 'Select repo first'"
                      :disabled="!selectedRepository || loadingBranches"
                      class="w-full px-3 py-2 bg-slate-800 border border-slate-700 rounded-lg text-white text-sm placeholder-slate-500 focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500/50 disabled:opacity-50 transition-all"
                      @focus="showBaseDropdown = true"
                      @keydown="handleBaseKeydown"
                    />
                    <Teleport to="body">
                      <div
                        v-if="showBaseDropdown && filteredBaseBranches.length > 0"
                        class="fixed bg-slate-800 border border-slate-600 rounded-lg shadow-xl max-h-48 overflow-y-auto z-50"
                        :style="baseDropdownStyle"
                      >
                        <button
                          v-for="(branch, index) in filteredBaseBranches"
                          :key="branch.name"
                          type="button"
                          :class="[
                            'w-full flex items-center gap-2 px-3 py-2 text-left transition-colors',
                            index === baseSelectedIndex ? 'bg-blue-600/20' : 'hover:bg-slate-700/50',
                            branch.name === selectedBaseBranch ? 'bg-blue-600/10' : ''
                          ]"
                          @click="selectBaseBranch(branch.name)"
                          @mouseenter="baseSelectedIndex = index"
                        >
                          <span class="text-sm text-slate-200">{{ branch.name }}</span>
                          <span v-if="branch.protected" class="text-xs text-yellow-400">
                            <svg class="w-3 h-3 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                            </svg>
                          </span>
                        </button>
                      </div>
                    </Teleport>
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

                  <button
                    v-if="comparison.files.length > 0"
                    @click="showFileList = !showFileList"
                    class="w-full flex items-center justify-between px-3 py-2 text-sm text-slate-400 hover:text-slate-200 hover:bg-slate-700/20 transition-colors border-t border-slate-700/30"
                  >
                    <span>{{ showFileList ? 'Hide' : 'Show' }} {{ comparison.files.length }} changed files</span>
                    <svg
                      class="w-4 h-4 transition-transform"
                      :class="{ 'rotate-180': showFileList }"
                      fill="none" stroke="currentColor" viewBox="0 0 24 24"
                    >
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                    </svg>
                  </button>

                  <Transition name="slide">
                    <div v-if="showFileList" class="max-h-48 overflow-y-auto divide-y divide-slate-700/30">
                      <div
                        v-for="file in comparison.files"
                        :key="file.path"
                        class="flex items-center gap-2 px-3 py-1.5 hover:bg-slate-700/20 transition-colors"
                      >
                        <span :class="['w-5 text-center text-[10px] font-bold', getFileIcon(file.path || '').color]">
                          {{ getFileIcon(file.path || '').icon }}
                        </span>
                        <span
                          :class="[
                            'w-2 h-2 rounded-full flex-shrink-0',
                            file.status === 'added' ? 'bg-green-500' :
                            file.status === 'deleted' ? 'bg-red-500' :
                            file.status === 'renamed' ? 'bg-blue-500' : 'bg-yellow-500'
                          ]"
                        />
                        <span class="text-sm text-slate-300 truncate flex-1" :title="file.path">{{ file.path }}</span>
                        <span v-if="file.additions" class="text-xs text-green-400">+{{ file.additions }}</span>
                        <span v-if="file.deletions" class="text-xs text-red-400">-{{ file.deletions }}</span>
                      </div>
                    </div>
                  </Transition>
                </div>
              </Transition>

              <div class="space-y-3">
                <div>
                  <div class="flex items-center justify-between">
                    <label class="block text-xs font-medium text-slate-400 mb-1.5">Title</label>
                    <span v-if="titleAutoFilled" class="text-[10px] text-slate-500 italic">Auto-filled from latest commit</span>
                  </div>
                  <input
                    v-model="title"
                    type="text"
                    placeholder="Pull request title"
                    class="w-full px-3 py-2 bg-slate-800 border border-slate-700 rounded-lg text-white text-sm placeholder-slate-500 focus:ring-2 focus:ring-green-500/50 focus:border-green-500/50 transition-all"
                  />
                </div>

                <div>
                  <label class="block text-xs font-medium text-slate-400 mb-1.5">Description <span class="text-slate-600">(optional)</span></label>
                  <RichTextEditor
                    v-model="body"
                    placeholder="Add a description..."
                    :min-height="100"
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

            <div v-if="!showSuccess" class="flex items-center justify-end gap-2 px-5 py-3 border-t border-slate-700/50 bg-slate-800/30">
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

            <div v-else class="flex items-center justify-end gap-2 px-5 py-3 border-t border-slate-700/50 bg-slate-800/30">
              <button
                @click="handleClose"
                class="px-4 py-2 rounded-lg text-slate-300 hover:text-white hover:bg-slate-700/50 transition-colors text-sm"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue';
import { useCreatePR } from '../composables/useCreatePR';
import { useFileIcons } from '../composables/useFileIcons';
import RichTextEditor from './RichTextEditor.vue';
import type { Repository } from '../types';

const props = defineProps<{
  isOpen: boolean;
}>();

const emit = defineEmits<{
  close: [];
  created: [pr: { id: number; url: string }];
}>();

const {
  sortedRepositories,
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

const { getFileIcon } = useFileIcons();

const showFileList = ref(false);
const showSuccess = ref(false);
const successResult = ref<{ id: number; url: string } | null>(null);
const titleAutoFilled = ref(false);

const repoSearch = ref('');
const showRepoDropdown = ref(false);
const repoSelectedIndex = ref(0);
const repoDropdownRef = ref<HTMLElement | null>(null);
const repoInputRef = ref<HTMLInputElement | null>(null);
const repoDropdownStyle = ref<Record<string, string>>({});

const headSearch = ref('');
const showHeadDropdown = ref(false);
const headSelectedIndex = ref(0);
const headDropdownRef = ref<HTMLElement | null>(null);
const headDropdownStyle = ref<Record<string, string>>({});

const baseSearch = ref('');
const showBaseDropdown = ref(false);
const baseSelectedIndex = ref(0);
const baseDropdownRef = ref<HTMLElement | null>(null);
const baseDropdownStyle = ref<Record<string, string>>({});

const filteredRepos = computed(() => {
  const query = repoSearch.value.toLowerCase().trim();
  if (!query) return sortedRepositories.value.slice(0, 30);
  return sortedRepositories.value
    .filter(r => r.fullName.toLowerCase().includes(query))
    .slice(0, 30);
});

const sortedBranches = computed(() => {
  const def = selectedRepository.value?.defaultBranch || 'main';
  return [...branches.value].sort((a, b) => {
    if (a.name === def) return -1;
    if (b.name === def) return 1;
    return a.name.localeCompare(b.name);
  });
});

const filteredHeadBranches = computed(() => {
  const query = headSearch.value.toLowerCase().trim();
  if (!query) return sortedBranches.value;
  return sortedBranches.value.filter(b => b.name.toLowerCase().includes(query));
});

const filteredBaseBranches = computed(() => {
  const query = baseSearch.value.toLowerCase().trim();
  if (!query) return sortedBranches.value;
  return sortedBranches.value.filter(b => b.name.toLowerCase().includes(query));
});

const updateDropdownPos = (inputEl: HTMLElement | null, styleRef: typeof repoDropdownStyle) => {
  if (!inputEl) return;
  const rect = inputEl.getBoundingClientRect();
  styleRef.value = {
    top: `${rect.bottom + 4}px`,
    left: `${rect.left}px`,
    width: `${rect.width}px`
  };
};

const selectRepo = (repo: Repository) => {
  selectedRepository.value = repo;
  repoSearch.value = '';
  showRepoDropdown.value = false;
  headSearch.value = '';
  baseSearch.value = '';
};

const selectHeadBranch = (name: string) => {
  selectedHeadBranch.value = name;
  headSearch.value = name;
  showHeadDropdown.value = false;
};

const selectBaseBranch = (name: string) => {
  selectedBaseBranch.value = name;
  baseSearch.value = name;
  showBaseDropdown.value = false;
};

const handleRepoKeydown = (e: KeyboardEvent) => {
  if (!showRepoDropdown.value) {
    if (e.key === 'ArrowDown' || e.key === 'Enter') {
      showRepoDropdown.value = true;
      repoSelectedIndex.value = 0;
      updateDropdownPos(repoInputRef.value, repoDropdownStyle);
    }
    return;
  }
  switch (e.key) {
    case 'ArrowDown':
      e.preventDefault();
      if (repoSelectedIndex.value < filteredRepos.value.length - 1) repoSelectedIndex.value++;
      break;
    case 'ArrowUp':
      e.preventDefault();
      if (repoSelectedIndex.value > 0) repoSelectedIndex.value--;
      break;
    case 'Enter':
      e.preventDefault();
      {
        const item = filteredRepos.value[repoSelectedIndex.value];
        if (item) selectRepo(item);
      }
      break;
    case 'Escape':
      e.preventDefault();
      showRepoDropdown.value = false;
      break;
  }
};

const handleHeadKeydown = (e: KeyboardEvent) => {
  if (!showHeadDropdown.value) {
    if (e.key === 'ArrowDown' || e.key === 'Enter') {
      showHeadDropdown.value = true;
      headSelectedIndex.value = 0;
      updateDropdownPos((headDropdownRef.value?.querySelector('input') as HTMLElement), headDropdownStyle);
    }
    return;
  }
  switch (e.key) {
    case 'ArrowDown':
      e.preventDefault();
      if (headSelectedIndex.value < filteredHeadBranches.value.length - 1) headSelectedIndex.value++;
      break;
    case 'ArrowUp':
      e.preventDefault();
      if (headSelectedIndex.value > 0) headSelectedIndex.value--;
      break;
    case 'Enter':
      e.preventDefault();
      {
        const item = filteredHeadBranches.value[headSelectedIndex.value];
        if (item) selectHeadBranch(item.name);
      }
      break;
    case 'Escape':
      e.preventDefault();
      showHeadDropdown.value = false;
      break;
  }
};

const handleBaseKeydown = (e: KeyboardEvent) => {
  if (!showBaseDropdown.value) {
    if (e.key === 'ArrowDown' || e.key === 'Enter') {
      showBaseDropdown.value = true;
      baseSelectedIndex.value = 0;
      updateDropdownPos((baseDropdownRef.value?.querySelector('input') as HTMLElement), baseDropdownStyle);
    }
    return;
  }
  switch (e.key) {
    case 'ArrowDown':
      e.preventDefault();
      if (baseSelectedIndex.value < filteredBaseBranches.value.length - 1) baseSelectedIndex.value++;
      break;
    case 'ArrowUp':
      e.preventDefault();
      if (baseSelectedIndex.value > 0) baseSelectedIndex.value--;
      break;
    case 'Enter':
      e.preventDefault();
      {
        const item = filteredBaseBranches.value[baseSelectedIndex.value];
        if (item) selectBaseBranch(item.name);
      }
      break;
    case 'Escape':
      e.preventDefault();
      showBaseDropdown.value = false;
      break;
  }
};

const handleClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  if (repoDropdownRef.value && !repoDropdownRef.value.contains(target)) showRepoDropdown.value = false;
  if (headDropdownRef.value && !headDropdownRef.value.contains(target)) showHeadDropdown.value = false;
  if (baseDropdownRef.value && !baseDropdownRef.value.contains(target)) showBaseDropdown.value = false;
};

const handleClose = () => {
  showSuccess.value = false;
  successResult.value = null;
  showFileList.value = false;
  titleAutoFilled.value = false;
  reset();
  emit('close');
};

const handleCreate = async () => {
  const result = await createPullRequest();
  if (result?.success && result.pullRequest) {
    successResult.value = { id: result.pullRequest.id, url: result.pullRequest.url };
    showSuccess.value = true;
    emit('created', successResult.value);
  }
};

const handleViewPR = () => {
  if (successResult.value) {
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

watch([showRepoDropdown, () => filteredRepos.value], () => {
  if (showRepoDropdown.value) {
    nextTick(() => updateDropdownPos(repoInputRef.value, repoDropdownStyle));
  }
});

watch([showHeadDropdown, () => filteredHeadBranches.value], () => {
  if (showHeadDropdown.value) {
    nextTick(() => updateDropdownPos(headDropdownRef.value?.querySelector('input') as HTMLElement | null, headDropdownStyle));
  }
});

watch([showBaseDropdown, () => filteredBaseBranches.value], () => {
  if (showBaseDropdown.value) {
    nextTick(() => updateDropdownPos(baseDropdownRef.value?.querySelector('input') as HTMLElement | null, baseDropdownStyle));
  }
});

watch(selectedRepository, (repo) => {
  if (repo) {
    repoSearch.value = '';
  }
  headSearch.value = '';
  baseSearch.value = '';
});

watch(branches, () => {
  if (selectedBaseBranch.value) {
    baseSearch.value = selectedBaseBranch.value;
  }
});

watch(title, (newVal, oldVal) => {
  if (newVal && !oldVal && comparison.value?.commits.length) {
    const firstLine = comparison.value.commits[0]!.message.split('\n')[0];
    if (newVal === firstLine) {
      titleAutoFilled.value = true;
      return;
    }
  }
  titleAutoFilled.value = false;
});

watch(comparison, (newVal) => {
  if (newVal?.commits.length && !title.value) {
    const firstLine = newVal.commits[0]!.message.split('\n')[0];
    if (firstLine) {
      title.value = firstLine;
      titleAutoFilled.value = true;
    }
  }
});

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
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
