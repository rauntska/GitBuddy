<script setup lang="ts">
import { computed, ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { PlusIcon, ArrowPathIcon, Cog6ToothIcon } from '@heroicons/vue/24/outline';
import { useAuthStore } from './stores/auth';
import LoginButton from './components/LoginButton.vue';
import UserMenu from './components/UserMenu.vue';
import SettingsModal from './views/SettingsModal.vue';
import MainLayout from './components/layout/MainLayout.vue';
import CreatePRModal from './components/create-pr-modal.vue';
import ToastContainer from './components/ToastContainer.vue';
import NotificationPermissionBanner from './components/NotificationPermissionBanner.vue';
import { usePullRequests } from './composables/usePullRequests';

const authStore = useAuthStore();
const route = useRoute();
const router = useRouter();
const { loading, lastRefresh, refreshPullRequests, fetchPullRequests } = usePullRequests();

onMounted(() => {
  if (authStore.isAuthenticated) {
    authStore.refreshUserData();
  }
});

const isDashboardRoute = computed(() => route.name === 'dashboard');
const shouldShowDashboardControls = computed(() => 
  authStore.isAuthenticated && isDashboardRoute.value
);

const showSettings = ref(false);
const showCreatePRModal = ref(false);

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

const handlePRCreated = (pr: { id: number; url: string }) => {
  fetchPullRequests();
  showCreatePRModal.value = false;
  router.push({ name: 'pr-detail', params: { id: pr.id } });
};
</script>

<template>
  <MainLayout>
    <NotificationPermissionBanner />
    <header class="sticky top-0 z-20 bg-slate-900/95 backdrop-blur border-b border-slate-800 pl-14">
      <div class="px-4 py-4">
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
            Git Buddy
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
              @click="showCreatePRModal = true"
              aria-label="Create new pull request"
              class="px-4 py-2 rounded-lg bg-green-600 hover:bg-green-500 text-white text-sm font-medium transition-colors flex items-center gap-2"
            >
              <PlusIcon class="w-4 h-4" />
              Create PR
            </button>
            <button
              v-if="shouldShowDashboardControls"
              @click="refreshPullRequests"
              :disabled="loading"
              aria-label="Refresh pull requests"
              class="px-4 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
            >
              <ArrowPathIcon
                class="w-4 h-4"
                :class="{ 'animate-spin': loading }"
              />
              Refresh
            </button>
            <button
              v-if="shouldShowDashboardControls"
              @click="showSettings = true"
              aria-label="Open settings"
              class="p-2 rounded-lg hover:bg-slate-800 text-slate-400 transition-colors"
            >
              <Cog6ToothIcon class="w-5 h-5" />
            </button>
            <LoginButton v-if="!authStore.isAuthenticated" />
            <UserMenu v-else />
          </div>
        </div>
      </div>
    </header>
    <main id="main-content" class="flex-1">
      <RouterView />
    </main>

    <SettingsModal v-if="showSettings" role="dialog" aria-modal="true" @close="showSettings = false" @saved="handleSettingsSaved" />
    <CreatePRModal
      :is-open="showCreatePRModal"
      @close="showCreatePRModal = false"
      @created="handlePRCreated"
    />
  </MainLayout>
  <ToastContainer />
</template>
