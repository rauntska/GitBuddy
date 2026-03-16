<template>
  <div class="space-y-3">
    <div class="flex items-center justify-between">
      <h3 class="text-sm font-medium text-slate-300">Reviewers</h3>
      <button
        v-if="!showAddInput"
        @click="openAddReviewer"
        class="text-xs text-blue-400 hover:text-blue-300 flex items-center gap-1"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        Add
      </button>
    </div>

    <div v-if="showAddInput" class="space-y-2">
      <SearchableDropdown
        :items="potentialReviewers"
        :excluded-names="existingReviewerNames"
        :loading="loadingPotentialReviewers"
        placeholder="Search users or teams..."
        @select="handleSelectReviewer"
      />
      <div class="flex gap-2">
        <button
          @click="showAddInput = false"
          class="px-3 py-1.5 text-sm bg-slate-700 text-slate-300 rounded hover:bg-slate-600"
        >
          Cancel
        </button>
      </div>
    </div>

    <div v-if="loading" class="flex justify-center py-4">
      <div class="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-500"></div>
    </div>

    <div v-else-if="reviewers.length === 0" class="text-sm text-slate-500 py-2">
      No reviewers requested
    </div>

    <div v-else class="space-y-2">
      <div
        v-for="reviewer in reviewers"
        :key="reviewer.username"
        class="flex items-center justify-between p-2 rounded bg-slate-800/50 hover:bg-slate-800"
      >
        <div class="flex items-center gap-2">
          <img
            v-if="reviewer.avatar"
            :src="reviewer.avatar"
            :alt="reviewer.username"
            class="w-6 h-6 rounded-full"
          />
          <div v-else class="w-6 h-6 rounded-full bg-purple-500/20 flex items-center justify-center">
            <svg class="w-4 h-4 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
            </svg>
          </div>
          <div>
            <div class="text-sm text-slate-200">{{ reviewer.username }}</div>
            <div v-if="reviewer.reviewState" class="text-xs" :class="getStateColor(reviewer.reviewState)">
              {{ getStateLabel(reviewer.reviewState) }}
              <span v-if="reviewer.reviewedAt" class="text-slate-500">
                {{ formatDate(reviewer.reviewedAt) }}
              </span>
            </div>
            <div v-else-if="reviewer.isRequested" class="text-xs text-slate-500">
              Review requested
            </div>
          </div>
        </div>
        <div class="flex items-center gap-2">
          <span
            v-if="reviewer.reviewState === 'APPROVED'"
            class="w-2 h-2 rounded-full bg-green-500"
            title="Approved"
          />
          <span
            v-else-if="reviewer.reviewState === 'CHANGES_REQUESTED'"
            class="w-2 h-2 rounded-full bg-orange-500"
            title="Changes Requested"
          />
          <span
            v-else-if="reviewer.reviewState === 'COMMENTED'"
            class="w-2 h-2 rounded-full bg-blue-500"
            title="Commented"
          />
          <button
            v-if="reviewer.isRequested"
            @click="removeReviewer(reviewer.username)"
            :disabled="removingReviewer === reviewer.username"
            class="p-1 text-slate-500 hover:text-red-400 disabled:opacity-50"
            title="Remove reviewer"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <div v-if="summary" class="pt-2 border-t border-slate-700">
      <div class="flex gap-4 text-xs">
        <span class="text-green-400">
          {{ summary.approved }} approved
        </span>
        <span class="text-orange-400">
          {{ summary.changesRequested }} changes requested
        </span>
        <span class="text-slate-500">
          {{ summary.pending }} pending
        </span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { apiService } from '../services/api';
import SearchableDropdown from './SearchableDropdown.vue';
import type { ReviewerStatus, PotentialReviewer } from '../types';

const props = defineProps<{
  pullRequestId: number;
}>();

const emit = defineEmits<{
  error: [message: string];
}>();

const reviewers = ref<ReviewerStatus[]>([]);
const potentialReviewers = ref<PotentialReviewer[]>([]);
const loading = ref(true);
const loadingPotentialReviewers = ref(false);
const showAddInput = ref(false);
const removingReviewer = ref<string | null>(null);

const existingReviewerNames = computed(() => 
  reviewers.value.map(r => r.username)
);

const summary = computed(() => {
  const approved = reviewers.value.filter(r => r.reviewState === 'APPROVED').length;
  const changesRequested = reviewers.value.filter(r => r.reviewState === 'CHANGES_REQUESTED').length;
  const pending = reviewers.value.filter(r => !r.reviewState).length;
  
  return { approved, changesRequested, pending };
});

const getStateColor = (state: string): string => {
  const colors: Record<string, string> = {
    APPROVED: 'text-green-400',
    CHANGES_REQUESTED: 'text-orange-400',
    COMMENTED: 'text-blue-400',
  };
  return colors[state] || 'text-slate-400';
};

const getStateLabel = (state: string): string => {
  const labels: Record<string, string> = {
    APPROVED: 'Approved',
    CHANGES_REQUESTED: 'Changes requested',
    COMMENTED: 'Reviewed',
  };
  return labels[state] || state;
};

const formatDate = (date: string): string => {
  const d = new Date(date);
  const now = new Date();
  const diffMs = now.getTime() - d.getTime();
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
  
  if (diffDays === 0) return 'today';
  if (diffDays === 1) return 'yesterday';
  if (diffDays < 7) return `${diffDays} days ago`;
  return d.toLocaleDateString();
};

const fetchReviewers = async () => {
  try {
    loading.value = true;
    reviewers.value = await apiService.getReviewers(props.pullRequestId);
  } catch {
    emit('error', 'Failed to load reviewers');
  } finally {
    loading.value = false;
  }
};

const fetchPotentialReviewers = async () => {
  try {
    loadingPotentialReviewers.value = true;
    potentialReviewers.value = await apiService.getPotentialReviewers(props.pullRequestId);
  } catch {
    emit('error', 'Failed to load potential reviewers');
  } finally {
    loadingPotentialReviewers.value = false;
  }
};

const openAddReviewer = () => {
  showAddInput.value = true;
  if (potentialReviewers.value.length === 0) {
    fetchPotentialReviewers();
  }
};

const handleSelectReviewer = async (item: PotentialReviewer) => {
  try {
    await apiService.addReviewers(props.pullRequestId, [item.name]);
    showAddInput.value = false;
    await fetchReviewers();
  } catch (err: unknown) {
    const error = err as { response?: { data?: { message?: string } } };
    emit('error', error.response?.data?.message || 'Failed to add reviewer');
  }
};

const removeReviewer = async (username: string) => {
  try {
    removingReviewer.value = username;
    await apiService.removeReviewer(props.pullRequestId, username);
    await fetchReviewers();
  } catch (err: unknown) {
    const error = err as { response?: { data?: { message?: string } } };
    emit('error', error.response?.data?.message || 'Failed to remove reviewer');
  } finally {
    removingReviewer.value = null;
  }
};

onMounted(fetchReviewers);

watch(() => props.pullRequestId, fetchReviewers);
</script>
