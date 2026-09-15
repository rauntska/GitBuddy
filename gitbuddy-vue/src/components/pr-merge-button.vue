<template>
  <div class="relative" ref="rootRef">
    <div class="flex items-stretch gap-0">
      <button
        @click="canMerge && emit('merge', selectedMergeMethod)"
        :disabled="!canMerge || merging"
        :class="[
          'px-3 sm:px-4 py-2 rounded-l-lg text-xs transition-all duration-150',
          canMerge && !merging
            ? 'bg-slate-200 text-slate-900 hover:bg-white'
            : 'bg-slate-800 text-slate-500 cursor-not-allowed',
        ]"
      >
        <span v-if="merging">Merging...</span>
        <span v-else-if="prDetail.isMerged">Merged</span>
        <span v-else>Merge</span>
      </button>
      <button
        @click="toggleDropdown"
        :disabled="!canMerge || merging"
        :class="[
          'px-2 py-2 rounded-r-lg text-xs border-l transition-all duration-150',
          canMerge && !merging
            ? 'bg-slate-200 text-slate-900 hover:bg-white border-slate-300'
            : 'bg-slate-800 text-slate-500 cursor-not-allowed border-slate-700',
        ]"
      >
        <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
        </svg>
      </button>
    </div>

    <!-- Merge Dropdown -->
    <div
      v-if="showDropdown"
      role="dialog"
      aria-modal="true"
      aria-label="Merge options"
      class="absolute right-0 top-full mt-2 w-80 bg-slate-900 border border-slate-800 rounded-lg shadow-xl z-50"
    >
      <div class="p-3 border-b border-slate-800">
        <div class="flex items-center justify-between">
          <span class="text-xs text-slate-400">Merge Method</span>
          <span :class="['text-xs font-mono', mergeableStatusColor]">{{ mergeableStatusText }}</span>
        </div>
      </div>

      <div class="p-2">
        <label
          v-for="method in availableMergeMethods"
          :key="method.value"
          class="flex items-start gap-3 p-2 rounded-lg cursor-pointer hover:bg-slate-800 transition-colors"
        >
          <input
            type="radio"
            :value="method.value"
            v-model="selectedMergeMethod"
            class="mt-1 accent-slate-300"
          />
          <div class="flex-1 min-w-0">
            <div class="text-sm text-slate-200">{{ method.label }}</div>
            <div class="text-xs text-slate-400 mt-0.5">{{ method.description }}</div>
          </div>
        </label>
      </div>

      <div v-if="mergeError" class="px-3 py-2 border-t border-slate-800">
        <p class="text-xs text-red-400">{{ mergeError }}</p>
      </div>

      <div class="p-3 border-t border-slate-800 flex justify-end gap-2">
        <button
          @click="closeDropdown"
          class="px-3 py-1.5 text-xs text-slate-400 hover:text-slate-200 transition-colors"
        >
          Cancel
        </button>
        <button
          @click="emit('merge', selectedMergeMethod)"
          :disabled="!canMerge || merging"
          class="px-4 py-1.5 bg-slate-200 hover:bg-white rounded text-xs text-slate-900 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {{ merging ? 'Merging...' : 'Confirm merge' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
import { usePRDetail } from '../composables/usePRDetail';
import type { MergeMethod, PRDetail } from '../types';

const props = defineProps<{
  prDetail: PRDetail;
  merging: boolean;
  mergeError: string | null;
}>();

const emit = defineEmits<{
  (e: 'merge', method: MergeMethod): void;
  (e: 'dismiss-error'): void;
}>();

const { getMergeOptions } = usePRDetail();

const rootRef = ref<HTMLElement | null>(null);
const showDropdown = ref(false);
const selectedMergeMethod = ref<MergeMethod>('squash');
const mergeOptions = ref<{
  mergeCommitAllowed: boolean;
  squashMergeAllowed: boolean;
  rebaseMergeAllowed: boolean;
  defaultMergeMethod: string;
  mergeableState?: string;
  isMerged: boolean;
  isDraft: boolean;
} | null>(null);

const canMerge = computed(() => {
  const pr = props.prDetail;
  if (pr.draft) return false;
  if (pr.isMerged) return false;
  if (pr.mergeableState === 'CONFLICTING') return false;
  if (pr.requiredApprovingReviews !== undefined && !pr.isMergeReady) return false;
  return true;
});

const mergeableStatusText = computed(() => {
  const pr = props.prDetail;
  if (pr.isMerged) return 'Already merged';
  if (pr.draft) return 'Draft PR';
  if (pr.mergeableState === 'CONFLICTING') return 'Has conflicts';
  if (pr.mergeableState === 'UNKNOWN') return 'Checking...';
  if (pr.mergeBlockReason) return pr.mergeBlockReason;
  if (pr.isMergeReady) return 'Ready to merge';
  return 'Review required';
});

const mergeableStatusColor = computed(() => {
  const pr = props.prDetail;
  if (pr.isMerged) return 'text-emerald-400';
  if (pr.draft) return 'text-amber-400';
  if (pr.mergeableState === 'CONFLICTING') return 'text-red-400';
  if (pr.mergeableState === 'UNKNOWN') return 'text-slate-400';
  if (pr.isMergeReady) return 'text-emerald-400';
  if (pr.mergeBlockReason) return 'text-amber-400';
  return 'text-slate-400';
});

const availableMergeMethods = computed(() => {
  if (!mergeOptions.value) return [];
  const methods: { value: MergeMethod; label: string; description: string }[] = [];

  if (mergeOptions.value.squashMergeAllowed) {
    methods.push({
      value: 'squash',
      label: 'Squash and merge',
      description: 'All commits from this branch will be combined into one commit in the base branch.',
    });
  }
  if (mergeOptions.value.mergeCommitAllowed) {
    methods.push({
      value: 'merge',
      label: 'Create a merge commit',
      description: 'All commits from this branch will be added to the base branch via a merge commit.',
    });
  }
  if (mergeOptions.value.rebaseMergeAllowed) {
    methods.push({
      value: 'rebase',
      label: 'Rebase and merge',
      description: 'All commits from this branch will be rebased and added to the base branch.',
    });
  }
  return methods;
});

const loadMergeOptions = async () => {
  const options = await getMergeOptions(props.prDetail.id);
  if (options) {
    mergeOptions.value = options;
    selectedMergeMethod.value = options.defaultMergeMethod as MergeMethod;
  }
};

const closeDropdown = () => {
  showDropdown.value = false;
  emit('dismiss-error');
};

const toggleDropdown = () => {
  showDropdown.value = !showDropdown.value;
  if (showDropdown.value) loadMergeOptions();
};

// A successful merge closes the dropdown; a failed one keeps it open to show the error.
watch(() => props.merging, (merging, wasMerging) => {
  if (wasMerging && !merging && !props.mergeError) {
    showDropdown.value = false;
  }
});

const handleClickOutside = (e: MouseEvent) => {
  if (showDropdown.value && !rootRef.value?.contains(e.target as HTMLElement)) {
    closeDropdown();
  }
};

onMounted(() => document.addEventListener('click', handleClickOutside));
onUnmounted(() => document.removeEventListener('click', handleClickOutside));
</script>
