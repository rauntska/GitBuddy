<template>
  <div class="space-y-3">
    <div>
      <h3 class="text-sm font-semibold uppercase tracking-wider text-slate-300 mb-2">GitHub App Configuration</h3>
      <p class="text-sm text-slate-200/60">
        Configure the GitHub App for fetching pull requests and organization data.
        These settings affect all users.
      </p>
    </div>

    <form @submit.prevent="handleSave" class="space-y-4 border-t border-slate-800 pt-4">
      <div>
        <label class="block text-xs uppercase tracking-wider text-slate-500 mb-1.5">
          GitHub Organization
        </label>
        <input
          v-model="localSettings.organization"
          type="text"
          required
          placeholder="e.g., myorganization"
          class="w-full px-3 py-2 bg-slate-900/60 border border-slate-700 rounded text-slate-200 placeholder-slate-500 focus:outline-none focus:border-slate-600 transition-colors font-mono text-sm"
        />
      </div>

      <div>
        <label class="block text-xs uppercase tracking-wider text-slate-500 mb-1.5">
          App ID
        </label>
        <input
          v-model="localSettings.appId"
          type="text"
          required
          placeholder="e.g., 123456"
          class="w-full px-3 py-2 bg-slate-900/60 border border-slate-700 rounded text-slate-200 placeholder-slate-500 focus:outline-none focus:border-slate-600 transition-colors font-mono text-sm tabular-nums"
        />
      </div>

      <div>
        <label class="block text-xs uppercase tracking-wider text-slate-500 mb-1.5">
          Private Key (PEM format)
        </label>
        <textarea
          v-model="localSettings.privateKey"
          required
          rows="6"
          placeholder="-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEA...
-----END RSA PRIVATE KEY-----"
          class="w-full px-3 py-2 bg-slate-900/60 border border-slate-700 rounded text-slate-200 placeholder-slate-500 focus:outline-none focus:border-slate-600 transition-colors font-mono text-xs resize-none"
        />
        <p class="mt-1.5 text-[11px] text-slate-500">
          Download from GitHub App settings page.
          <a
            href="https://github.com/settings/apps"
            target="_blank"
            rel="noopener noreferrer"
            class="text-slate-300 hover:text-white underline underline-offset-2"
          >
            Manage GitHub Apps
          </a>
        </p>
      </div>

      <div>
        <label class="block text-xs uppercase tracking-wider text-slate-500 mb-1.5">
          Installation ID
        </label>
        <input
          v-model="localSettings.installationId"
          type="text"
          required
          placeholder="e.g., 987654"
          class="w-full px-3 py-2 bg-slate-900/60 border border-slate-700 rounded text-slate-200 placeholder-slate-500 focus:outline-none focus:border-slate-600 transition-colors font-mono text-sm tabular-nums"
        />
        <p class="mt-1.5 text-[11px] text-slate-500">
          Found in URL when viewing app installation:
          <code class="bg-slate-800 border border-slate-700 px-1 py-0.5 rounded font-mono text-[11px]">/settings/installations/[INSTALLATION_ID]</code>
        </p>
      </div>

      <div>
        <label class="block text-xs uppercase tracking-wider text-slate-500 mb-1.5">
          Auto-refresh Interval: <span class="font-mono tabular-nums text-slate-300">{{ localSettings.refreshIntervalMinutes }}</span> minutes
        </label>
        <input
          v-model.number="localSettings.refreshIntervalMinutes"
          type="range"
          min="5"
          max="60"
          step="5"
          class="w-full h-1.5 bg-slate-800 rounded-lg appearance-none cursor-pointer accent-slate-400"
        />
        <div class="flex justify-between text-[11px] text-slate-500 font-mono tabular-nums mt-1">
          <span>5 min</span>
          <span>60 min</span>
        </div>
      </div>

      <div class="flex items-center gap-2">
        <input
          type="checkbox"
          v-model="localSettings.deleteOldPRs"
          id="deleteOldPRs"
          class="w-3.5 h-3.5 rounded border-slate-700 bg-slate-900/60 accent-slate-400"
        />
        <label for="deleteOldPRs" class="text-slate-200 text-sm cursor-pointer">
          Delete closed/merged PRs (disable to keep history)
        </label>
      </div>

      <div
        v-if="localSettings.lastRefresh"
        class="flex items-center gap-2 border-l-2 border-slate-700 pl-3 py-1"
      >
        <span class="text-[11px] uppercase tracking-wider text-slate-500">Last Refresh</span>
        <span class="text-sm text-slate-200 font-mono tabular-nums">
          {{ formatDate(localSettings.lastRefresh) }}
        </span>
      </div>

      <div
        v-if="error"
        class="inline-flex items-center gap-2 px-3 py-2 rounded border border-red-900/40 bg-red-950/20 text-red-400 text-sm"
      >
        <span class="font-mono">✕</span>
        {{ error }}
      </div>

      <div class="flex gap-3 pt-2">
        <button
          type="submit"
          :disabled="loading"
          class="inline-flex items-center gap-2 px-3 py-1.5 rounded bg-slate-200 hover:bg-white text-slate-900 text-sm transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <svg
            v-if="loading"
            class="w-3.5 h-3.5 animate-spin"
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
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useSettings } from '../../composables/useSettings';

const { settings, loading, error, fetchSettings, saveSettings } = useSettings();

const localSettings = ref({
  organization: '',
  refreshIntervalMinutes: 5,
  lastRefresh: '',
  appId: '',
  privateKey: '',
  installationId: '',
  deleteOldPRs: false,
});

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleString();
};

const handleSave = async () => {
  const success = await saveSettings({
    organization: localSettings.value.organization,
    refreshIntervalMinutes: localSettings.value.refreshIntervalMinutes,
    appId: localSettings.value.appId,
    privateKey: localSettings.value.privateKey,
    installationId: localSettings.value.installationId,
    useGitHubApp: true,
    deleteOldPRs: localSettings.value.deleteOldPRs,
  });

  if (success) {
    await fetchSettings();
  }
};

onMounted(async () => {
  await fetchSettings();
  if (settings.value) {
    localSettings.value = {
      organization: settings.value.organization || '',
      refreshIntervalMinutes: settings.value.refreshIntervalMinutes || 5,
      lastRefresh: settings.value.lastRefresh || '',
      appId: settings.value.appId || '',
      privateKey: settings.value.privateKey || '',
      installationId: settings.value.installationId || '',
      deleteOldPRs: settings.value.deleteOldPRs || false,
    };
  }
});
</script>
