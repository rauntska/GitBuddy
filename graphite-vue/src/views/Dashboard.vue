<template>
  <div class="min-h-screen bg-slate-900 text-slate-100">
    <main class="max-w-screen-2xl mx-auto px-4 py-6">
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

      <!-- Authenticated Content -->
      <template v-if="authStore.isAuthenticated">
        <!-- Connection Status + Stats Summary (hide during initial load) -->
        <div v-if="!loading || hasPRData" class="flex items-center justify-between mb-4">
          <StatsSummary :stats="stats" />
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
  import { useAuthStore } from '../stores/auth';
  import StatsSummary from '../components/StatsSummary.vue';
  import PRGroup from '../components/PRGroup.vue';
  import SkeletonPRRow from '../components/SkeletonPRRow.vue';
  import EmptyState from '../components/EmptyState.vue';

  const authStore = useAuthStore();

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

// Determine if we have any PR data
const hasPRData = computed(() => Object.keys(pullRequests.value).length > 0);

window.setInterval(() => {
  if (authStore.isAuthenticated) {
    refreshPullRequests();
  }
}, 60000);

const groupTitle = (status: string): string => {
  const titles: Record<string, string> = {
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
  if (authStore.isAuthenticated) {
    hasAttemptedLoad.value = true;
    await fetchPullRequests();
    loadMergedPRs(true);
    
    if (authStore.token) {
      await signalR.connect(authStore.token);
    }
  }
});

onUnmounted(async () => {
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
