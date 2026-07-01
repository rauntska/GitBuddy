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
                href="https://github.com/settings/tokens/new?description=GitBuddy%20PR%20Dashboard&scopes=repo,read:org"
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
            <!-- Density Toggle -->
            <div class="flex items-center rounded border border-slate-800 bg-slate-900/60 p-0.5 font-mono text-xs">
              <button
                @click="setListViewMode('compact')"
                :title="'Compact view'"
                :class="[
                  'px-2 py-1 transition-colors',
                  listViewMode === 'compact' ? 'bg-slate-700 text-slate-100' : 'text-slate-500 hover:text-slate-300'
                ]"
              >C</button>
              <button
                @click="setListViewMode('comfortable')"
                :title="'Comfortable view'"
                :class="[
                  'px-2 py-1 transition-colors',
                  listViewMode === 'comfortable' ? 'bg-slate-700 text-slate-100' : 'text-slate-500 hover:text-slate-300'
                ]"
              >M</button>
              <button
                @click="setListViewMode('expanded')"
                :title="'Expanded view'"
                :class="[
                  'px-2 py-1 transition-colors',
                  listViewMode === 'expanded' ? 'bg-slate-700 text-slate-100' : 'text-slate-500 hover:text-slate-300'
                ]"
              >E</button>
            </div>

            <!-- Column Headers Toggle -->
            <button
              @click="setShowColumnHeaders(!showColumnHeaders)"
              :title="showColumnHeaders ? 'Hide column headers' : 'Show column headers'"
              :class="[
                'flex items-center justify-center rounded border p-1.5 transition-colors',
                showColumnHeaders
                  ? 'border-slate-700 bg-slate-700 text-slate-100'
                  : 'border-slate-800 bg-slate-900/60 text-slate-500 hover:text-slate-300'
              ]"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16M4 6v12M9 6v12M14 6v12M20 6v12" />
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
        title="Welcome to GitBuddy!"
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
      <div v-if="authStore.isAuthenticated && hasPRData" class="space-y-3">
        <!-- Edit Layout Toolbar -->
        <div v-if="editLayoutMode" class="flex items-center gap-3 py-2 px-3 bg-slate-800/40 border border-slate-800 rounded mb-4">
          <span class="text-sm text-slate-400">Drag groups to reorder &bull; Click eye to hide</span>
          <div class="ml-auto flex gap-2">
            <button
              @click="resetDashboardLayout"
              class="px-3 py-1 text-xs rounded border border-slate-700 text-slate-300 hover:bg-slate-800 transition-colors"
            >
              Reset Layout
            </button>
            <button
              @click="editLayoutMode = false"
              class="px-3 py-1 text-xs rounded bg-slate-200 text-slate-900 hover:bg-white transition-colors"
            >
              Done
            </button>
          </div>
        </div>

        <!-- Edit Mode Toggle (shown when not in edit mode) -->
        <div v-if="!editLayoutMode" class="flex justify-end">
          <button
            @click="editLayoutMode = true"
            class="px-2 py-1 text-xs rounded text-slate-500 hover:text-slate-300 hover:bg-slate-800 transition-colors"
            title="Customize dashboard layout"
          >
            <svg class="w-4 h-4 inline-block mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4" />
            </svg>
            Customize
          </button>
        </div>

        <!-- Render groups in order -->
        <div
          v-for="status in orderedGroupKeys"
          :key="status"
          :draggable="editLayoutMode"
          class="relative"
          @dragstart="onDragStart($event, status)"
          @dragover.prevent="onDragOver($event, status)"
          @drop="onDrop($event, status)"
          @dragend="onDragEnd"
        >
          <!-- Drag handle in edit mode -->
          <div v-if="editLayoutMode" class="absolute -left-6 top-3 cursor-grab text-slate-600 hover:text-slate-400">
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path d="M7 2a2 2 0 1 0 0 4 2 2 0 0 0 0-4zM13 2a2 2 0 1 0 0 4 2 2 0 0 0 0-4zM7 8a2 2 0 1 0 0 4 2 2 0 0 0 0-4zM13 8a2 2 0 1 0 0 4 2 2 0 0 0 0-4zM7 14a2 2 0 1 0 0 4 2 2 0 0 0 0-4zM13 14a2 2 0 1 0 0 4 2 2 0 0 0 0-4z" />
            </svg>
          </div>
          <PRGroup
            :title="groupTitle(status)"
            :pull-requests="pullRequests[status] || []"
            :status="status"
            :expanded="expandedGroups[status] ?? true"
            :compact="isCompactMode"
            :density="listViewMode"
            :show-headers="showColumnHeaders"
            @toggle="toggleGroup(status)"
            @contextmenu="onPRContextMenu"
          />
          <!-- Hide button in edit mode -->
          <button
            v-if="editLayoutMode"
            @click.stop="hideGroup(status)"
            class="absolute top-3 right-10 p-1 rounded text-slate-600 hover:text-slate-400 hover:bg-slate-800 transition-colors"
            title="Hide this group"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
            </svg>
          </button>
        </div>

        <!-- Hidden groups (in edit mode) -->
        <div v-if="editLayoutMode && hiddenGroups.length > 0" class="mt-4 pt-4 border-t border-slate-700/50">
          <div class="text-xs text-slate-500 mb-2 uppercase tracking-wider">Hidden Groups</div>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="status in hiddenGroups"
              :key="status"
              @click="showGroup(status)"
              class="px-3 py-1 text-xs rounded-full border border-slate-700 text-slate-500 hover:text-slate-300 hover:border-slate-500 transition-colors"
            >
              {{ groupTitle(status) }} — Show
            </button>
          </div>
        </div>
      </div>

      <!-- Branches Without PRs -->
      <BranchesWithoutPRSection
        v-if="authStore.isAuthenticated"
        :grouped-branches="groupedBranchesWithoutPR"
        :total-branches="totalBranchesWithoutPR"
        :loading="branchesWithoutPRLoading"
        :refreshing="branchesWithoutPRRefreshing"
        :error="branchesWithoutPRError"
        @refresh="manualRefreshBranchesWithoutPR"
      />

      <!-- Merged / Closed PRs -->
      <div v-if="authStore.isAuthenticated && mergedPRs.length > 0" class="mt-8">
        <PRGroup
          title="Merged / Closed"
          :pull-requests="mergedPRs"
          status="Merged"
          :expanded="expandedGroups.Merged ?? true"
          :compact="isCompactMode"
          :density="listViewMode"
          :show-headers="showColumnHeaders"
          @toggle="toggleGroup('Merged')"
          @contextmenu="onPRContextMenu"
        />

        <div v-if="mergedPRsHasMore" class="text-center mt-4">
          <button
            @click="loadMoreMergedPRs"
            :disabled="mergedPRsLoading"
            class="px-4 py-2 border border-slate-800 hover:bg-slate-800 disabled:opacity-50 disabled:cursor-not-allowed text-slate-300 text-sm rounded transition-colors"
          >
            {{ mergedPRsLoading ? 'Loading...' : 'Load More Merged / Closed PRs' }}
          </button>
        </div>
      </div>
    </main>

    <!-- Quick Actions Context Menu -->
    <ContextMenu
      :visible="contextMenuVisible"
      :items="contextMenuItems"
      :x="contextMenuX"
      :y="contextMenuY"
      @close="contextMenuVisible = false"
      @select="onContextMenuSelect"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
  import { usePullRequests } from '../composables/usePullRequests';
  import { useUserPreferences } from '../composables/useUserPreferences';
  import { useUserSettings } from '../composables/useUserSettings';
  import { useFaviconBadge } from '../composables/useFaviconBadge';
  import { useBranchesWithoutPR } from '../composables/useBranchesWithoutPR';
  import { useSignalR } from '../composables/useSignalR';
  import { apiService } from '../services/api';
  import { useAuthStore } from '../stores/auth';
  import StatsSummary from '../components/StatsSummary.vue';
  import PRGroup from '../components/PRGroup.vue';
  import SkeletonPRRow from '../components/SkeletonPRRow.vue';
  import EmptyState from '../components/EmptyState.vue';
  import BranchesWithoutPRSection from '../components/BranchesWithoutPRSection.vue';
  import ContextMenu from '../components/ContextMenu.vue';
  import type { MenuItem } from '../components/ContextMenu.vue';

  const authStore = useAuthStore();
  const { preferences, loadPreferences, setListViewMode, setShowColumnHeaders, togglePinnedPr, isPrPinned, setDashboardGroupOrder, setHiddenDashboardGroups, resetDashboardLayout } = useUserPreferences();
  const { hasPersonalAccessToken, fetchUserSettings } = useUserSettings();
  const { initFavicon, updateBadge } = useFaviconBadge();
  const {
    branches: branchesWithoutPR,
    groupedByRepo: groupedBranchesWithoutPR,
    loading: branchesWithoutPRLoading,
    refreshing: branchesWithoutPRRefreshing,
    error: branchesWithoutPRError,
    fetchBranches: fetchBranchesWithoutPR,
    manualRefresh: manualRefreshBranchesWithoutPR,
    applyBranchResolved: applyPendingBranchResolved,
    applyBranchAdded: applyPendingBranchAdded,
  } = useBranchesWithoutPR();

  const branchSignalR = useSignalR();
  branchSignalR.onPendingBranchResolved.value = (notification) => {
    applyPendingBranchResolved(notification.repoFullName, notification.branchName);
  };
  branchSignalR.onPendingBranchAdded.value = (branch) => {
    applyPendingBranchAdded(branch);
  };

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

const listViewMode = computed(() => preferences.value.listViewMode ?? 'comfortable');
const isCompactMode = computed(() => listViewMode.value === 'compact');
const showColumnHeaders = computed(() => preferences.value.showColumnHeaders ?? true);

// Dashboard layout
const editLayoutMode = ref(false);

const defaultGroupOrder = ['ReadyToMerge', 'AwaitingReview', 'Approved', 'ChangesRequested', 'Reviewed', 'Draft'];

const orderedGroupKeys = computed(() => {
  const customOrder = preferences.value.dashboardGroupOrder;
  const hiddenGroups = preferences.value.hiddenDashboardGroups ?? [];
  const availableKeys = Object.keys(pullRequests.value);

  if (customOrder && customOrder.length > 0) {
    // Merge custom order with any new groups not in the order
    const ordered = customOrder.filter(k => availableKeys.includes(k) && !hiddenGroups.includes(k));
    const remaining = availableKeys.filter(k => !customOrder.includes(k) && !hiddenGroups.includes(k));
    return [...ordered, ...remaining];
  }

  // Default order: use predefined order, then any remaining groups
  const ordered = defaultGroupOrder.filter(k => availableKeys.includes(k) && !hiddenGroups.includes(k));
  const remaining = availableKeys.filter(k => !defaultGroupOrder.includes(k) && !hiddenGroups.includes(k));
  return [...ordered, ...remaining];
});

const hiddenGroups = computed(() => preferences.value.hiddenDashboardGroups ?? []);

const hasPRData = computed(() => Object.keys(pullRequests.value).length > 0);

const totalBranchesWithoutPR = computed(() => branchesWithoutPR.value.length);

const showPATWarning = computed(() => {
  return hasPersonalAccessToken.value === false && !patWarningDismissed.value;
});

const dismissPATWarning = () => {
  patWarningDismissed.value = true;
};

// Context menu state
const contextMenuVisible = ref(false);
const contextMenuX = ref(0);
const contextMenuY = ref(0);
const contextMenuPr = ref<any>(null);

const contextMenuItems = computed<MenuItem[]>(() => {
  if (!contextMenuPr.value) return [];
  const pr = contextMenuPr.value;
  const pinned = isPrPinned(pr.id);
  return [
    { label: 'Open PR Detail', action: 'open', icon: '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />', iconClass: 'text-blue-400' },
    { label: pinned ? 'Unpin from Dashboard' : 'Pin to Dashboard', action: 'pin', icon: '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />', iconClass: pinned ? 'text-amber-400' : 'text-slate-400' },
    { divider: true, label: '' },
    { label: 'Copy PR Link', action: 'copyLink', icon: '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />', iconClass: 'text-slate-400' },
    { label: 'Copy Branch Name', action: 'copyBranch', icon: '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2" />', iconClass: 'text-slate-400' },
    { label: 'View on GitHub', action: 'viewGithub', icon: '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />', iconClass: 'text-purple-400' },
  ];
});

const onPRContextMenu = (payload: { pr: any; x: number; y: number }) => {
  contextMenuPr.value = payload.pr;
  contextMenuX.value = payload.x;
  contextMenuY.value = payload.y;
  contextMenuVisible.value = true;
};

const onContextMenuSelect = async (action: string) => {
  const pr = contextMenuPr.value;
  if (!pr) return;

  switch (action) {
    case 'open':
      // Navigation handled by router-link, but right-click context can also trigger this
      break;
    case 'pin':
      await togglePinnedPr(pr.id);
      break;
    case 'copyLink':
      await navigator.clipboard.writeText(pr.url);
      break;
    case 'copyBranch':
      // Branch name isn't on the PullRequest type, use a fallback
      await navigator.clipboard.writeText(pr.repository ? `${pr.repository}-pr-${pr.gitHubId}` : `pr-${pr.gitHubId}`);
      break;
    case 'viewGithub':
      window.open(pr.url, '_blank');
      break;
  }
  contextMenuVisible.value = false;
};

// Drag-and-drop for dashboard layout
const draggedGroup = ref<string | null>(null);

const onDragStart = (e: DragEvent, status: string) => {
  if (!editLayoutMode.value) return;
  draggedGroup.value = status;
  (e.dataTransfer as DataTransfer).effectAllowed = 'move';
};

const onDragOver = (e: DragEvent, status: string) => {
  if (!draggedGroup.value || draggedGroup.value === status) return;
  (e.target as HTMLElement).closest('[draggable]')?.classList.add('ring-2', 'ring-blue-500/50');
};

const onDrop = async (e: DragEvent, targetStatus: string) => {
  if (!draggedGroup.value || draggedGroup.value === targetStatus) return;
  (e.target as HTMLElement).closest('[draggable]')?.classList.remove('ring-2', 'ring-blue-500/50');

  const currentOrder = [...orderedGroupKeys.value];
  const fromIndex = currentOrder.indexOf(draggedGroup.value);
  const toIndex = currentOrder.indexOf(targetStatus);
  if (fromIndex === -1 || toIndex === -1) return;

  currentOrder.splice(fromIndex, 1);
  currentOrder.splice(toIndex, 0, draggedGroup.value);
  await setDashboardGroupOrder(currentOrder);
  draggedGroup.value = null;
};

const onDragEnd = (_e: DragEvent) => {
  draggedGroup.value = null;
  document.querySelectorAll('.ring-blue-500\\/50').forEach(el => el.classList.remove('ring-2', 'ring-blue-500/50'));
};

const hideGroup = async (status: string) => {
  const current = preferences.value.hiddenDashboardGroups ?? [];
  if (!current.includes(status)) {
    await setHiddenDashboardGroups([...current, status]);
  }
};

const showGroup = async (status: string) => {
  const current = preferences.value.hiddenDashboardGroups ?? [];
  await setHiddenDashboardGroups(current.filter(g => g !== status));
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
    fetchBranchesWithoutPR();
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

    await Promise.all([
      fetchPullRequests(),
      fetchUnreadCount(),
      loadMergedPRs(true),
      fetchBranchesWithoutPR(),
      authStore.token ? signalR.connect(authStore.token) : Promise.resolve(),
    ]);
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
