<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthStore } from './stores/auth';
import LoginButton from './components/LoginButton.vue';
import UserMenu from './components/UserMenu.vue';
import SettingsModal from './views/SettingsModal.vue';
import { usePullRequests } from './composables/usePullRequests';

const authStore = useAuthStore();
const route = useRoute();
const { loading, lastRefresh, refreshPullRequests } = usePullRequests();

const isDashboardRoute = computed(() => route.name === 'dashboard');
const shouldShowDashboardControls = computed(() => 
  authStore.isAuthenticated && isDashboardRoute.value
);

const showSettings = ref(false);
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
};
</script>

<template>
  <div class="min-h-screen bg-slate-900 text-slate-100">
    <header class="sticky top-0 z-10 bg-slate-900/95 backdrop-blur border-b border-slate-800">
      <div class="max-w-screen-2xl mx-auto px-4 py-4">
        <div class="flex items-center justify-between">
          <router-link to="/" class="text-xl font-semibold text-white flex items-center gap-2 hover:opacity-80 transition-opacity">
            <svg class="w-10 h-10" fill="none" viewBox="0 0 24 24">
              <circle cx="12" cy="4" r="2.5" stroke="#22c55e" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" />
              <path stroke="#22c55e" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 7v3" />
              <path stroke="#22c55e" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 10h6" />
              <path stroke="#22c55e" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 10v2.5c0 1.5 1 2.5 2 2.5s2-1 2-2.5V10" />
              <path stroke="#22c55e" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 15l-1.5 1.5m5.5-1.5l1.5 1.5" />
              <path stroke="#3b82f6" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 13l2 2m0 0l2-2m-2 2V7" />
              <path stroke="#3b82f6" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 13l-2 2m0 0l-2-2m2 2V7" />
            </svg>
            Code Buddy
          </router-link>
          <div class="flex items-center gap-3">
            <span
              v-if="shouldShowDashboardControls && lastRefresh"
              class="text-xs text-slate-500 flex items-center gap-1"
            >
              <span class="w-2 h-2 rounded-full bg-green-500 animate-pulse"></span>
              Updated {{ formatRelativeTime(lastRefresh) }}
            </span>
            <button
              v-if="shouldShowDashboardControls"
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
              v-if="shouldShowDashboardControls"
              @click="showSettings = true"
              class="p-2 rounded-lg hover:bg-slate-800 text-slate-400 transition-colors"
              title="Settings"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 00-1.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-1.066-2.573c-.94-1.543.826-3.31 2.37-2.37a1.724 1.724 0 002.572-1.065z"
                />
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                />
              </svg>
            </button>
            <LoginButton v-if="!authStore.isAuthenticated" />
            <UserMenu v-else />
          </div>
        </div>
      </div>
    </header>
    <main>
      <RouterView />
    </main>

    <SettingsModal v-if="showSettings" @close="showSettings = false" @saved="handleSettingsSaved" />
  </div>
</template>
