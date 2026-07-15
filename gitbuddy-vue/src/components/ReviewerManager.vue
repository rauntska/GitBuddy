<template>
  <div class="space-y-3">
    <div class="flex items-center justify-between">
      <h3 class="text-sm font-semibold text-slate-300 uppercase tracking-wider">Reviewers</h3>
      <div class="flex items-center gap-2">
        <button
          v-if="!showAddInput && pendingReviewerNames.length > 0"
          @click="nudgeAllPending"
          :disabled="nudgingAll"
          class="text-xs text-slate-300 hover:text-amber-300 flex items-center gap-1 border border-slate-800 hover:bg-slate-800 rounded px-2 py-0.5 transition-colors"
          title="Re-request review from all reviewers"
        >
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
          </svg>
          Nudge all
        </button>
        <button
          v-if="!showAddInput"
          @click="openAddReviewer"
          class="text-xs text-slate-300 hover:text-slate-100 flex items-center gap-1 border border-slate-800 hover:bg-slate-800 rounded px-2 py-0.5 transition-colors"
        >
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Add
        </button>
      </div>
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
          class="px-3 py-1.5 text-sm border border-slate-800 text-slate-300 rounded hover:bg-slate-800 transition-colors"
        >
          Cancel
        </button>
      </div>
    </div>

    <div v-if="loading" class="flex justify-center py-4">
      <div class="animate-spin rounded-full h-6 w-6 border-b-2 border-slate-400"></div>
    </div>

    <div v-else-if="reviewers.length === 0" class="text-sm text-slate-500 py-2">
      No reviewers requested
    </div>

    <div v-else class="space-y-1.5">
      <div
        v-for="reviewer in reviewers"
        :key="reviewer.username"
        class="flex items-center justify-between p-2 rounded border border-slate-800 hover:bg-slate-800/40 transition-colors"
      >
        <div class="flex items-center gap-2">
          <img
            v-if="reviewer.avatar"
            :src="reviewer.avatar"
            :alt="reviewer.username"
            class="w-6 h-6 rounded-full"
          />
          <div v-else class="w-6 h-6 rounded-full bg-slate-800 border border-slate-700 flex items-center justify-center">
            <svg class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
            </svg>
          </div>
          <div>
            <div class="text-sm text-slate-200">{{ reviewer.username }}</div>
            <div v-if="reviewer.reviewState" class="text-xs px-1.5 py-0.5 rounded font-mono">
              <span :class="getStateColor(reviewer.reviewState)">{{ getStateLabel(reviewer.reviewState) }}</span>
              <span v-if="reviewer.reviewedAt" class="text-slate-500 ml-1">
                {{ formatDate(reviewer.reviewedAt) }}
              </span>
            </div>
            <div v-else-if="reviewer.isRequested" class="text-xs text-slate-500">
              Review requested
            </div>
          </div>
        </div>
        <div class="flex items-center gap-2">
          <button
            @click="nudgeReviewer(reviewer.username)"
            :disabled="nudgingReviewer === reviewer.username || nudgingAll"
            class="p-1 text-slate-500 hover:text-amber-400 disabled:opacity-50 transition-colors"
            :title="`Nudge ${reviewer.username} (re-request review)`"
          >
            <svg v-if="nudgingReviewer === reviewer.username" class="w-4 h-4 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
            </svg>
          </button>
          <button
            v-if="reviewer.isRequested"
            @click="removeReviewer(reviewer.username)"
            :disabled="removingReviewer === reviewer.username"
            class="p-1 text-slate-500 hover:text-rose-400 disabled:opacity-50"
            title="Remove reviewer"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <div v-if="summary" class="pt-2 border-t border-slate-800">
      <div class="flex gap-4 text-xs font-mono tabular-nums">
        <span class="text-emerald-400">
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
import { useToast } from '../composables/useToast';
import SearchableDropdown from './SearchableDropdown.vue';
import type { ReviewerStatus, PotentialReviewer } from '../types';

const props = defineProps<{
  pullRequestId: number;
}>();

const toast = useToast();

const emit = defineEmits<{
  error: [message: string];
}>();

const reviewers = ref<ReviewerStatus[]>([]);
const potentialReviewers = ref<PotentialReviewer[]>([]);
const loading = ref(true);
const loadingPotentialReviewers = ref(false);
const showAddInput = ref(false);
const removingReviewer = ref<string | null>(null);
const nudgingReviewer = ref<string | null>(null);
const nudgingAll = ref(false);

const existingReviewerNames = computed(() =>
  reviewers.value.map(r => r.username)
);

// All reviewers are valid nudge targets: GitHub re-request review works for any
// collaborator regardless of whether they already submitted a review (clearing
// the "requested" flag). This covers the common case of nudging a reviewer who
// previously requested changes after the author has addressed them.
const pendingReviewerNames = computed(() =>
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
    Approved: 'text-emerald-400',
    ChangesRequested: 'text-amber-400',
    Commented: 'text-violet-400',
  };
  return colors[state] || 'text-slate-400';
};

const getStateLabel = (state: string): string => {
  const labels: Record<string, string> = {
    Approved: 'Approved',
    ChangesRequested: 'Changes requested',
    Commented: 'Reviewed',
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

const nudgeReviewer = async (username: string) => {
  try {
    nudgingReviewer.value = username;
    await apiService.nudgeReviewers(props.pullRequestId, [username]);
    toast.success(`${username} nudged`);
  } catch (err: unknown) {
    const error = err as { response?: { data?: { message?: string } } };
    const message = error.response?.data?.message || 'Failed to nudge reviewer';
    const isRateLimited = /nudged recently|wait/i.test(message);
    if (isRateLimited) {
      toast.info(message, 6000);
    } else {
      toast.error(message);
      emit('error', message);
    }
  } finally {
    nudgingReviewer.value = null;
  }
};

const nudgeAllPending = async () => {
  const targets = pendingReviewerNames.value;
  if (targets.length === 0) return;
  try {
    nudgingAll.value = true;
    await apiService.nudgeReviewers(props.pullRequestId, targets);
    toast.success(`Nudged ${targets.length} reviewer${targets.length === 1 ? '' : 's'}`);
  } catch (err: unknown) {
    const error = err as { response?: { data?: { message?: string } } };
    const message = error.response?.data?.message || 'Failed to nudge reviewers';
    const isRateLimited = /nudged recently|wait/i.test(message);
    if (isRateLimited) {
      toast.info(message, 6000);
    } else {
      toast.error(message);
      emit('error', message);
    }
  } finally {
    nudgingAll.value = false;
  }
};

onMounted(fetchReviewers);

watch(() => props.pullRequestId, fetchReviewers);
</script>
