<template>
  <div class="min-h-screen bg-slate-900 text-slate-100">
    <main class="max-w-screen-2xl mx-auto px-4 py-6 overflow-x-hidden">
      <!-- Auth Required State -->
      <div
        v-if="!authStore.isAuthenticated"
        class="flex flex-col items-center justify-center py-20"
      >
        <div class="text-center">
          <div class="mb-6">
            <svg class="w-20 h-20 text-blue-500 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
            </svg>
          </div>
          <h2 class="text-2xl font-bold text-white mb-2">Authentication Required</h2>
          <p class="text-slate-400 mb-6 max-w-md">
            Please log in with your GitHub account to access the pull request dashboard.
          </p>
        </div>
      </div>

      <!-- Error Banner (only show when authenticated) -->
      <div 
        v-if="authStore.isAuthenticated && error" 
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

      <!-- PAT Warning Banner -->
      <div
        v-if="authStore.isAuthenticated && showPATWarning"
        class="mb-6 p-4 rounded-lg bg-amber-500/10 border border-amber-500/30 text-amber-400 text-sm"
      >
        <div class="flex items-start gap-3">
          <svg class="w-5 h-5 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
          <div class="flex-1">
            <div class="font-medium mb-1">Personal Access Token not configured</div>
            <p class="text-xs text-amber-400/80 mb-2">
              Some features require a PAT: submitting reviews, posting comments, marking files as viewed, and merging PRs.
            </p>
            <p class="text-xs text-amber-400/80 mb-3">
              Required permissions: <code class="px-1 py-0.5 bg-amber-500/20 rounded text-amber-300">repo</code> and <code class="px-1 py-0.5 bg-amber-500/20 rounded text-amber-300">read:org</code>
            </p>
            <div class="flex gap-3">
              <a
                href="https://github.com/settings/tokens/new?description=Graphite%20PR%20Dashboard&scopes=repo,read:org"
                target="_blank"
                rel="noopener noreferrer"
                class="inline-flex items-center gap-1 text-xs text-amber-300 hover:text-amber-200 underline"
              >
                Create token on GitHub
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                </svg>
              </a>
              <router-link
                to="/settings"
                class="inline-flex items-center gap-1 text-xs text-amber-300 hover:text-amber-200 underline"
              >
                Go to Settings
              </router-link>
            </div>
          </div>
          <button
            @click="dismissPATWarning"
            class="p-1 rounded hover:bg-amber-500/20 text-amber-400 hover:text-amber-300 transition-colors"
            title="Dismiss for this session"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Authenticated Content -->
      <template v-if="authStore.isAuthenticated">
        <!-- Connection Status + Stats Summary (hide during initial load) -->
        <div v-if="!loading || hasPRData" class="flex flex-wrap items-center justify-between gap-4 mb-4">
          <StatsSummary :stats="stats" />
          <div class="flex items-center gap-2 flex-shrink-0">
            <button
              @click="toggleCompactMode"
              :title="isCompactMode ? 'Switch to normal view' : 'Switch to compact view'"
              class="p-2 rounded-lg text-slate-400 hover:text-slate-200 hover:bg-slate-700/50 transition-colors"
            >
              <svg v-if="isCompactMode" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4" />
              </svg>
              <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 9V4.5M9 9H4.5M9 9L3.75 3.75M9 15v4.5M9 15H4.5M9 15l-5.25 5.25M15 9h4.5M15 9V4.5M15 9l5.25-5.25M15 15h4.5M15 15v4.5m0-4.5l5.25 5.25" />
              </svg>
            </button>
          </div>
        </div>

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
      </template>

      <!-- Empty State: No Configuration -->
      <EmptyState
        v-if="authStore.isAuthenticated && !loading && !hasPRData && !hasAttemptedLoad"
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
      />

      <!-- Empty State: No PRs Found -->
      <EmptyState
        v-if="authStore.isAuthenticated && !loading && !hasPRData && hasAttemptedLoad && !error"
        type="no-prs"
        title="No Open Pull Requests"
        description="Your team is all caught up! There are no open pull requests at the moment."
        :primary-action="{
          label: 'Refresh',
          icon: 'M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15'
        }"
        :secondary-action="{
          label: 'Check Settings',
          icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 00-1.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-1.066-2.573c-.94-1.543.826-3.31 2.37 2.37a1.724 1.724 0 00-1.066-2.573c-.94-1.543.826-3.31-2.37 2.37a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31 2.37.996.608 2.296.07 2.572-1.065z'
        }"
        @primary-action="handleRetry"
      />

      <!-- Empty State: Error -->
      <EmptyState
        v-if="authStore.isAuthenticated && !loading && error && !hasPRData"
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
      />

      <!-- PR Groups -->
      <div v-if="authStore.isAuthenticated && hasPRData" class="space-y-6">
        <PRGroup
          v-for="(group, status) in pullRequests"
          :key="status"
          :title="groupTitle(status)"
          :pull-requests="group"
          :status="status"
          :expanded="expandedGroups[status] ?? true"
          :compact="isCompactMode"
          @toggle="toggleGroup(status)"
        />
      </div>

      <!-- Merged / Closed PRs -->
      <div v-if="authStore.isAuthenticated && mergedPRs.length > 0" class="mt-8">
        <PRGroup
          title="Merged / Closed"
          :pull-requests="mergedPRs"
          status="Merged"
          :expanded="expandedGroups.Merged ?? true"
          :compact="isCompactMode"
          @toggle="toggleGroup('Merged')"
        />

        <div v-if="mergedPRsHasMore" class="text-center mt-4">
          <button
            @click="loadMoreMergedPRs"
            :disabled="mergedPRsLoading"
            class="px-4 py-2 bg-slate-700 hover:bg-slate-600 disabled:opacity-50 disabled:cursor-not-allowed text-slate-300 text-sm rounded transition-colors"
          >
            {{ mergedPRsLoading ? 'Loading...' : 'Load More Merged / Closed PRs' }}
          </button>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
  import { usePullRequests } from '../composables/usePullRequests';
  import { useUserPreferences } from '../composables/useUserPreferences';
  import { useUserSettings } from '../composables/useUserSettings';
  import { useFaviconBadge } from '../composables/useFaviconBadge';
  import { apiService } from '../services/api';
  import { useAuthStore } from '../stores/auth';
  import StatsSummary from '../components/StatsSummary.vue';
  import PRGroup from '../components/PRGroup.vue';
  import SkeletonPRRow from '../components/SkeletonPRRow.vue';
  import EmptyState from '../components/EmptyState.vue';

  const authStore = useAuthStore();
  const { preferences, loadPreferences, setListViewMode } = useUserPreferences();
  const { hasPersonalAccessToken, fetchUserSettings } = useUserSettings();
  const { initFavicon, updateBadge } = useFaviconBadge();

   const {
     pullRequests,
     stats,
     loading,
     error,
     fetchPullRequests,
     refreshPullRequests,
     mergedPRs,
     mergedPRsLoading,
     mergedPRsHasMore,
     loadMergedPRs,
     loadMoreMergedPRs,
     signalR,
   } = usePullRequests();

const expandedGroups = ref<Record<string, boolean>>({});
const hasAttemptedLoad = ref(false);
const patWarningDismissed = ref(false);

const isCompactMode = computed(() => preferences.value.listViewMode === 'compact');

const hasPRData = computed(() => Object.keys(pullRequests.value).length > 0);

const showPATWarning = computed(() => {
  return hasPersonalAccessToken.value === false && !patWarningDismissed.value;
});

const dismissPATWarning = () => {
  patWarningDismissed.value = true;
};

const toggleCompactMode = async () => {
  const newMode = isCompactMode.value ? 'normal' : 'compact';
  await setListViewMode(newMode);
};

const fetchUnreadCount = async () => {
  try {
    const result = await apiService.getUnreadCount();
    updateBadge(result.count);
  } catch (error) {
    console.error('Failed to fetch unread count:', error);
  }
};

const refreshInterval = window.setInterval(() => {
  if (authStore.isAuthenticated) {
    refreshPullRequests();
    fetchUnreadCount();
  }
}, 60000);

const groupTitle = (status: string): string => {
  const titles: Record<string, string> = {
    ReadyToMerge: 'Ready to Merge',
    AwaitingReview: 'Awaiting Review',
    Approved: 'Approved',
    Reviewed: 'Reviewed',
    ChangesRequested: 'Changes Requested',
    Draft: 'Drafts',
    Merged: 'Merged / Closed',
  };
  return titles[status] || status;
};

const toggleGroup = (status: string) => {
  expandedGroups.value[status] = !(expandedGroups.value[status] ?? true);
};

const handleRetry = () => {
   fetchPullRequests();
  };

onMounted(async () => {
  initFavicon();
  
  if (authStore.isAuthenticated) {
    hasAttemptedLoad.value = true;
    await loadPreferences();
    await fetchUserSettings();
    await fetchPullRequests();
    await fetchUnreadCount();
    loadMergedPRs(true);
    
    if (authStore.token) {
      await signalR.connect(authStore.token);
    }
  }
});

onUnmounted(async () => {
  clearInterval(refreshInterval);
  await signalR.disconnect();
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
