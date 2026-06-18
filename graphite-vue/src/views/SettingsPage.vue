<template>
  <div class="flex min-h-[calc(100vh-73px)]">
    <SettingsNav />
    <main class="flex-1 p-6 overflow-auto">
      <div :class="isAnalytics ? 'max-w-[1600px]' : 'max-w-4xl'">
        <UserSettingsPanel v-if="isUserSettings" />
        <GitHubAppPanel v-else-if="isGitHubApp" />
        <AdminPanel v-else-if="isAdmin" />
        <AnalyticsPanel v-else-if="isAnalytics" />
        <div v-else class="text-slate-400 text-center py-8">
          Page not found
        </div>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed, defineAsyncComponent } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import SettingsNav from '../components/settings/SettingsNav.vue';
import UserSettingsPanel from '../components/settings/UserSettingsPanel.vue';
import GitHubAppPanel from '../components/settings/GitHubAppPanel.vue';
import AdminPanel from '../components/settings/AdminPanel.vue';

const AnalyticsPanel = defineAsyncComponent(() => import('../components/analytics/AnalyticsPanel.vue'));

const route = useRoute();
const authStore = useAuthStore();

const isUserSettings = computed(() => route.path === '/settings');
const isGitHubApp = computed(() => route.path === '/settings/github-app');
const isAdmin = computed(() => route.path === '/settings/admin' && authStore.isAdmin);
const isAnalytics = computed(() => route.path === '/settings/analytics' && authStore.isAdmin);
</script>
