<template>
  <div class="min-h-screen bg-slate-900 text-slate-100">
    <header class="sticky top-0 z-10 bg-slate-900/95 backdrop-blur border-b border-slate-800">
      <div class="max-w-7xl mx-auto px-4 py-4">
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

    <main class="max-w-7xl mx-auto px-4 py-6">
      <div v-if="error" class="mb-6 p-4 rounded-lg bg-red-500/10 border border-red-500/30 text-red-400 text-sm">
        {{ error }}
      </div>

      <StatsSummary v-if="!loading" :stats="stats" />

      <div v-if="loading && !Object.keys(pullRequests).length" class="flex items-center justify-center py-20">
        <div class="flex items-center gap-3 text-slate-500">
          <svg class="w-5 h-5 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
            />
          </svg>
          Loading pull requests...
        </div>
      </div>

      <div v-if="!Object.keys(pullRequests).length && !loading" class="flex items-center justify-center py-20">
        <div class="text-center text-slate-500">
          <svg class="w-16 h-16 mx-auto mb-4 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="1.5"
              d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4"
            />
          </svg>
          <p class="text-lg font-medium mb-2">No pull requests found</p>
          <p class="text-sm">Configure your GitHub settings to get started</p>
        </div>
      </div>

      <div v-else>
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
import { ref, onMounted } from 'vue';
import { usePullRequests } from '../composables/usePullRequests';
import StatsSummary from '../components/StatsSummary.vue';
import PRGroup from '../components/PRGroup.vue';
import SettingsModal from './SettingsModal.vue';

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
  fetchPullRequests();
};

onMounted(() => {
  fetchPullRequests();
});
</script>
