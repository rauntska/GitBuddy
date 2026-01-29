<template>
  <div
    class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
    @click.self="$emit('close')"
  >
    <div class="w-full max-w-md bg-slate-900 border border-slate-700 rounded-xl shadow-2xl">
      <div class="flex items-center justify-between p-5 border-b border-slate-800">
        <h2 class="text-lg font-semibold text-white">GitHub Settings</h2>
        <button
          @click="$emit('close')"
          class="text-slate-500 hover:text-slate-300 transition-colors p-1"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <form @submit.prevent="handleSave" class="p-5 space-y-4">
        <div>
          <label class="block text-sm font-medium text-slate-300 mb-2">
            GitHub Organization
          </label>
          <input
            v-model="localSettings.organization"
            type="text"
            required
            placeholder="e.g., myorganization"
            class="w-full px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-300 mb-2">
            Authentication Method
          </label>
          <div class="flex gap-2">
            <button
              type="button"
              @click="localSettings.useGitHubApp = false"
              :class="[
                'flex-1 px-4 py-2.5 rounded-lg font-medium transition-colors',
                localSettings.useGitHubApp 
                  ? 'bg-slate-800 text-slate-300 hover:bg-slate-700' 
                  : 'bg-blue-600 text-white'
              ]"
            >
              Personal Access Token
            </button>
            <button
              type="button"
              @click="localSettings.useGitHubApp = true"
              :class="[
                'flex-1 px-4 py-2.5 rounded-lg font-medium transition-colors',
                localSettings.useGitHubApp 
                  ? 'bg-blue-600 text-white' 
                  : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
              ]"
            >
              GitHub App
            </button>
          </div>
        </div>

        <div v-if="!localSettings.useGitHubApp">
          <label class="block text-sm font-medium text-slate-300 mb-2">
            Personal Access Token
          </label>
          <input
            v-model="localSettings.personalAccessToken"
            type="password"
            required
            placeholder="ghp_xxxxxxxxxxxx"
            class="w-full px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
          />
          <p class="mt-2 text-xs text-slate-500">
            Token requires 'repo' and 'read:org' scopes.
            <a
              href="https://github.com/settings/tokens/new"
              target="_blank"
              rel="noopener noreferrer"
              class="text-blue-400 hover:text-blue-300"
            >
              Create new token →
            </a>
          </p>
        </div>

        <div v-else class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-slate-300 mb-2">
              App ID
            </label>
            <input
              v-model="localSettings.appId"
              type="text"
              required
              placeholder="e.g., 123456"
              class="w-full px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-slate-300 mb-2">
              Private Key (PEM format)
            </label>
            <textarea
              v-model="localSettings.privateKey"
              required
              rows="6"
              placeholder="-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEA...
-----END RSA PRIVATE KEY-----"
              class="w-full px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all font-mono text-xs resize-none"
            />
            <p class="mt-2 text-xs text-slate-500">
              Download from GitHub App settings page.
              <a
                href="https://github.com/settings/apps"
                target="_blank"
                rel="noopener noreferrer"
                class="text-blue-400 hover:text-blue-300"
              >
                Manage GitHub Apps →
              </a>
            </p>
          </div>

          <div>
            <label class="block text-sm font-medium text-slate-300 mb-2">
              Installation ID
            </label>
            <input
              v-model="localSettings.installationId"
              type="text"
              required
              placeholder="e.g., 987654"
              class="w-full px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
            />
            <p class="mt-2 text-xs text-slate-500">
              Found in the URL when viewing app installation: 
              <code class="bg-slate-700 px-1 rounded">/settings/installations/[INSTALLATION_ID]</code>
            </p>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-300 mb-2">
            Auto-refresh Interval: {{ localSettings.refreshIntervalMinutes }} minutes
          </label>
          <input
            v-model.number="localSettings.refreshIntervalMinutes"
            type="range"
            min="5"
            max="60"
            step="5"
            class="w-full h-2 bg-slate-700 rounded-lg appearance-none cursor-pointer accent-blue-500"
          />
          <div class="flex justify-between text-xs text-slate-500 mt-1">
            <span>5 min</span>
            <span>60 min</span>
          </div>
        </div>

        <div
          v-if="localSettings.lastRefresh"
          class="p-3 bg-slate-800/50 rounded-lg border border-slate-700/50"
        >
          <div class="text-xs text-slate-500">Last Refresh</div>
          <div class="text-sm text-slate-300 font-medium">
            {{ formatDate(localSettings.lastRefresh) }}
          </div>
        </div>

        <div
          v-if="error"
          class="p-3 rounded-lg bg-red-500/10 border border-red-500/30 text-red-400 text-sm"
        >
          {{ error }}
        </div>

        <div class="flex gap-3 pt-2">
          <button
            type="button"
            @click="$emit('close')"
            class="flex-1 px-4 py-2.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 font-medium transition-colors"
          >
            Cancel
          </button>
          <button
            type="submit"
            :disabled="loading"
            class="flex-1 px-4 py-2.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            <svg
              v-if="loading"
              class="w-4 h-4 animate-spin"
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
            {{ loading ? 'Saving...' : 'Save Settings' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import { useSettings } from '../composables/useSettings';

const emit = defineEmits<{
  close: [];
  saved: [];
}>();

const { settings, loading, error, fetchSettings, saveSettings: saveSettingsAction } = useSettings();

const localSettings = ref({
  organization: '',
  personalAccessToken: '',
  refreshIntervalMinutes: 5,
  lastRefresh: '',
  appId: '',
  privateKey: '',
  installationId: '',
  useGitHubApp: false,
});

watch(
  () => settings.value,
  (newSettings) => {
    localSettings.value = { 
      ...newSettings, 
      lastRefresh: newSettings.lastRefresh || '',
      useGitHubApp: newSettings.useGitHubApp || false,
      appId: newSettings.appId || '',
      privateKey: newSettings.privateKey || '',
      installationId: newSettings.installationId || '',
    };
  },
  { deep: true, immediate: true }
);

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleString();
};

const handleSave = async () => {
  const settingsToSave = {
    organization: localSettings.value.organization,
    personalAccessToken: localSettings.value.personalAccessToken,
    refreshIntervalMinutes: localSettings.value.refreshIntervalMinutes,
    appId: localSettings.value.appId,
    privateKey: localSettings.value.privateKey,
    installationId: localSettings.value.installationId,
    useGitHubApp: localSettings.value.useGitHubApp,
  };
  const success = await saveSettingsAction(settingsToSave);
  if (success) {
    emit('saved');
  }
};

onMounted(() => {
  fetchSettings();
});
</script>
