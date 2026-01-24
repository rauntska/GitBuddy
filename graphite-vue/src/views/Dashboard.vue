<template>
  <div class="min-h-screen bg-slate-900 text-slate-100">
    <header class="sticky top-0 z-10 bg-slate-900/95 backdrop-blur border-b border-slate-800">
      <div class="max-w-screen-2xl mx-auto px-4 py-4">
        <div class="flex items-center justify-between">
          <h1 class="text-xl font-semibold text-white flex items-center gap-2">
            <svg class="w-6 h-6 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" />
            </svg>
            Graphite
          </h1>
          <div class="flex items-center gap-3">
            <span
              v-if="lastRefresh"
              class="text-xs text-slate-500 flex items-center gap-1"
            >
              <span class="w-2 h-2 rounded-full bg-green-500 animate-pulse"></span>
              Updated {{ formatRelativeTime(lastRefresh) }}
            </span>
            <button
              @click="refreshPullRequests"
              :disabled="loading"
              class="px-4 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
            >
              <svg
                class="w-4 h-4"
                :class="{ 'animate-spin': loading }"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
                />
              </svg>
              Refresh
            </button>
            <button
              @click="showSettings = true"
              class="p-2 rounded-lg hover:bg-slate-800 text-slate-400 transition-colors"
              title="Settings"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 00-1.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 001.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 00-1.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"
                />
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </header>

    <main class="max-w-screen-2xl mx-auto px-4 py-6">
      <!-- Error Banner -->
      <div 
        v-if="error" 
        class="mb-6 p-4 rounded-lg bg-red-500/10 border border-red-500/30 text-red-400 text-sm flex items-start gap-3"
      >
        <svg class="w-5 h-5 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <div class="flex-1">
          <div class="font-medium mb-1">Failed to load pull requests</div>
          <div class="text-xs text-red-400/80">{{ error }}</div>
        </div>
        <button
          @click="handleRetry"
          class="px-3 py-1.5 rounded bg-red-500/20 hover:bg-red-500/30 text-red-400 text-xs font-medium transition-colors"
        >
          Retry
        </button>
      </div>

      <!-- Stats Summary (hide during initial load) -->
      <StatsSummary v-if="!loading || hasPRData" :stats="stats" />

      <!-- Loading State: Skeleton Screens -->
      <div v-if="loading && !hasPRData" class="space-y-6">
        <div class="space-y-3">
          <div class="flex items-center justify-between mb-3">
            <div class="h-6 w-40 bg-slate-700/50 rounded shimmer"></div>
          </div>
          <SkeletonPRRow v-for="i in 3" :key="`skeleton-${i}`" />
        </div>
        <div class="space-y-3">
          <div class="flex items-center justify-between mb-3">
            <div class="h-6 w-40 bg-slate-700/50 rounded shimmer"></div>
          </div>
          <SkeletonPRRow v-for="i in 2" :key="`skeleton-2-${i}`" />
        </div>
      </div>

      <!-- Empty State: No Configuration -->
      <EmptyState
        v-else-if="!loading && !hasPRData && !hasAttemptedLoad"
        type="no-config"
        title="Welcome to Graphite!"
        description="Track pull requests across your GitHub organization. Configure your GitHub settings to get started."
        :primary-action="{
          label: 'Configure Settings',
          icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 00-1.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 001.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 00-1.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z'
        }"
        :help-link="{
          label: 'Need help? View setup guide',
          url: 'https://github.com/settings/tokens/new'
        }"
        @primary-action="handleOpenSettings"
      />

      <!-- Empty State: No PRs Found -->
      <EmptyState
        v-else-if="!loading && !hasPRData && hasAttemptedLoad && !error"
        type="no-prs"
        title="No Open Pull Requests"
        description="Your team is all caught up! There are no open pull requests at the moment."
        :primary-action="{
          label: 'Refresh',
          icon: 'M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15'
        }"
        :secondary-action="{
          label: 'Check Settings',
          icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 00-1.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 001.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 00-1.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z'
        }"
        @primary-action="handleRetry"
        @secondary-action="handleOpenSettings"
      />

      <!-- Empty State: Error -->
      <EmptyState
        v-else-if="!loading && error && !hasPRData"
        type="error"
        title="Failed to Load Pull Requests"
        description="There was an error loading pull requests. This might be due to an invalid GitHub token, network issues, or insufficient permissions."
        :primary-action="{
          label: 'Try Again',
          icon: 'M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15'
        }"
        :secondary-action="{
          label: 'Update Settings',
          icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 00-1.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 001.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 00-1.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z'
        }"
        :help-link="{
          label: 'Need help? Create a new token',
          url: 'https://github.com/settings/tokens/new'
        }"
        @primary-action="handleRetry"
        @secondary-action="handleOpenSettings"
      />

      <!-- PR Groups -->
      <div v-if="hasPRData" class="space-y-6">
        <PRGroup
          v-for="(group, status) in pullRequests"
          :key="status"
          :title="groupTitle(status)"
          :pull-requests="group"
          :status="status"
          :expanded="expandedGroups[status] ?? true"
          @toggle="toggleGroup(status)"
        />
      </div>
    </main>

    <SettingsModal v-if="showSettings" @close="showSettings = false" @saved="handleSettingsSaved" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { usePullRequests } from '../composables/usePullRequests';
import StatsSummary from '../components/StatsSummary.vue';
import PRGroup from '../components/PRGroup.vue';
import SettingsModal from './SettingsModal.vue';
import SkeletonPRRow from '../components/SkeletonPRRow.vue';
import EmptyState from '../components/EmptyState.vue';

const {
  pullRequests,
  stats,
  loading,
  error,
  lastRefresh,
  fetchPullRequests,
  refreshPullRequests,
} = usePullRequests();

const showSettings = ref(false);
const expandedGroups = ref<Record<string, boolean>>({});
const hasAttemptedLoad = ref(false);

// Determine if we have any PR data
const hasPRData = computed(() => Object.keys(pullRequests.value).length > 0);

window.setInterval(() => {
  refreshPullRequests();
}, 60000);

const groupTitle = (status: string): string => {
  const titles: Record<string, string> = {
    AwaitingReview: 'Awaiting Review',
    Approved: 'Approved',
    Reviewed: 'Reviewed',
    ChangesRequested: 'Changes Requested',
    Draft: 'Drafts',
  };
  return titles[status] || status;
};

const toggleGroup = (status: string) => {
  expandedGroups.value[status] = !(expandedGroups.value[status] ?? true);
};

const formatRelativeTime = (dateString: string): string => {
  const date = new Date(dateString);
  const now = new Date();
  const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
};

const handleSettingsSaved = () => {
  showSettings.value = false;
  hasAttemptedLoad.value = false;
  fetchPullRequests();
};

const handleRetry = () => {
  fetchPullRequests();
};

const handleOpenSettings = () => {
  showSettings.value = true;
};

onMounted(() => {
  hasAttemptedLoad.value = true;
  fetchPullRequests();
});
</script>

<style scoped>
@keyframes shimmer {
  0% {
    background-position: -1000px 0;
  }
  100% {
    background-position: 1000px 0;
  }
}

.shimmer {
  animation: shimmer 2s infinite linear;
  background: linear-gradient(
    to right,
    transparent 0%,
    rgba(148, 163, 184, 0.1) 50%,
    transparent 100%
  );
  background-size: 1000px 100%;
}
</style>
