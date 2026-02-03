<template>
  <div
    class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
    @click.self="$emit('close')"
  >
    <div class="w-full max-w-md bg-slate-900 border border-slate-700 rounded-xl shadow-2xl">
      <div class="flex items-center justify-between p-5 border-b border-slate-800">
        <div class="flex items-center gap-4">
          <button
            type="button"
            @click="activeTab = 'github'"
            :class="[
              'text-base font-semibold transition-colors',
              activeTab === 'github' ? 'text-white' : 'text-slate-400 hover:text-slate-300'
            ]"
          >
            GitHub Settings
          </button>
          <button
            type="button"
            @click="activeTab = 'preferences'"
            :class="[
              'text-base font-semibold transition-colors',
              activeTab === 'preferences' ? 'text-white' : 'text-slate-400 hover:text-slate-300'
            ]"
          >
            Preferences
          </button>
        </div>
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
        <!-- GitHub Settings Tab -->
        <div v-if="isGitHubTab" class="space-y-4">
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
              Found in URL when viewing app installation: 
              <code class="bg-slate-700 px-1 rounded">/settings/installations/[INSTALLATION_ID]</code>
            </p>
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
        </div>
        </div>

        <!-- Preferences Tab -->
        <div v-if="isPreferencesTab" class="space-y-4">
          <!-- Keyboard Shortcuts -->
          <div>
            <label class="block text-sm font-medium text-slate-300 mb-2">
              Keyboard Shortcuts
            </label>
            <div class="space-y-2">
              <div class="flex items-center gap-2">
                <span class="w-32 text-xs text-slate-400">Toggle Comments</span>
                <input
                  :value="localSettings.keyboardShortcuts?.toggleComments || 'c'"
                  @input="(e: any) => updateShortcut('toggleComments', e.target.value)"
                  type="text"
                  maxlength="1"
                  class="w-16 px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-sm text-center uppercase focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div class="flex items-center gap-2">
                <span class="w-32 text-xs text-slate-400">Toggle File Tree</span>
                <input
                  :value="localSettings.keyboardShortcuts?.toggleFileTree || 'f'"
                  @input="(e: any) => updateShortcut('toggleFileTree', e.target.value)"
                  type="text"
                  maxlength="1"
                  class="w-16 px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-sm text-center uppercase focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div class="flex items-center gap-2">
                <span class="w-32 text-xs text-slate-400">Next File</span>
                <input
                  :value="localSettings.keyboardShortcuts?.nextFile || 'j'"
                  @input="(e: any) => updateShortcut('nextFile', e.target.value)"
                  type="text"
                  maxlength="1"
                  class="w-16 px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-sm text-center uppercase focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div class="flex items-center gap-2">
                <span class="w-32 text-xs text-slate-400">Previous File</span>
                <input
                  :value="localSettings.keyboardShortcuts?.previousFile || 'k'"
                  @input="(e: any) => updateShortcut('previousFile', e.target.value)"
                  type="text"
                  maxlength="1"
                  class="w-16 px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-sm text-center uppercase focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div class="flex items-center gap-2">
                <span class="w-32 text-xs text-slate-400">Next Comment</span>
                <input
                  :value="localSettings.keyboardShortcuts?.nextComment || 'n'"
                  @input="(e: any) => updateShortcut('nextComment', e.target.value)"
                  type="text"
                  maxlength="1"
                  class="w-16 px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-sm text-center uppercase focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div class="flex items-center gap-2">
                <span class="w-32 text-xs text-slate-400">Previous Comment</span>
                <input
                  :value="localSettings.keyboardShortcuts?.previousComment || 'p'"
                  @input="(e: any) => updateShortcut('previousComment', e.target.value)"
                  type="text"
                  maxlength="1"
                  class="w-16 px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-sm text-center uppercase focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
            </div>
          </div>
        </div>

        <div
          v-if="isGitHubTab && localSettings.lastRefresh"
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
import { ref, watch, onMounted, computed } from 'vue';
import { useSettings } from '../composables/useSettings';
import { useUserPreferences } from '../composables/useUserPreferences';
import type { KeyboardShortcuts } from '../types';

const emit = defineEmits<{
  close: [];
  saved: [];
}>();

const { settings, loading, error, fetchSettings, saveSettings: saveSettingsAction } = useSettings();
const { setKeyboardShortcut } = useUserPreferences();

const activeTab = ref<'github' | 'preferences'>('github');

const isGitHubTab = computed(() => activeTab.value === 'github');
const isPreferencesTab = computed(() => activeTab.value === 'preferences');

const localSettings = ref({
  organization: '',
  personalAccessToken: '',
  refreshIntervalMinutes: 5,
  lastRefresh: '',
  appId: '',
  privateKey: '',
  installationId: '',
  useGitHubApp: false,
  keyboardShortcuts: {
    toggleComments: 'c',
    toggleFileTree: 'f',
    nextFile: 'j',
    previousFile: 'k',
    nextComment: 'n',
    previousComment: 'p',
  } as KeyboardShortcuts,
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
      keyboardShortcuts: newSettings.keyboardShortcuts || localSettings.value.keyboardShortcuts,
    };
  },
  { deep: true, immediate: true }
);

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleString();
};

const updateShortcut = async (key: keyof KeyboardShortcuts, value: string) => {
  const lowerValue = value.toLowerCase();
  if (lowerValue && lowerValue.length === 1) {
    localSettings.value.keyboardShortcuts[key] = lowerValue;
    try {
      await setKeyboardShortcut(key, lowerValue);
    } catch (error) {
      console.error('Failed to update keyboard shortcut:', error);
    }
  }
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
    keyboardShortcuts: localSettings.value.keyboardShortcuts,
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
