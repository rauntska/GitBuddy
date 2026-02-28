<template>
  <div class="space-y-6">
    <div>
      <h3 class="text-lg font-semibold text-white mb-4">Personal Access Token</h3>
      <p class="text-sm text-slate-400 mb-4">
        Your Personal Access Token is used for user-specific operations like submitting reviews,
        marking files as viewed, and posting comments.
      </p>
      <form @submit.prevent="savePAT" class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-slate-300 mb-2">
            Personal Access Token
          </label>
          <div class="flex gap-2">
            <input
              v-model="localPAT"
              :type="showPAT ? 'text' : 'password'"
              :placeholder="hasExistingPAT ? '••••••••••••••••••••' : 'ghp_xxxxxxxxxxxx'"
              class="flex-1 px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
            />
            <button
              type="button"
              @click="showPAT = !showPAT"
              class="px-3 py-2 rounded-lg bg-slate-800 border border-slate-700 text-slate-400 hover:text-slate-200 transition-colors"
            >
              <EyeIcon v-if="!showPAT" class="w-5 h-5" />
              <EyeSlashIcon v-else class="w-5 h-5" />
            </button>
          </div>
          <p class="mt-2 text-xs text-slate-500">
            Token requires 'repo' and 'read:org' scopes.
            <a
              href="https://github.com/settings/tokens/new"
              target="_blank"
              rel="noopener noreferrer"
              class="text-blue-400 hover:text-blue-300"
            >
              Create new token
            </a>
          </p>
        </div>
        <div class="flex gap-3">
          <button
            type="submit"
            :disabled="savingPAT"
            class="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium transition-colors disabled:opacity-50"
          >
            {{ savingPAT ? 'Saving...' : 'Save Token' }}
          </button>
          <button
            v-if="hasExistingPAT"
            type="button"
            @click="clearPAT"
            class="px-4 py-2 rounded-lg bg-red-600/20 hover:bg-red-600/30 text-red-400 text-sm font-medium transition-colors"
          >
            Clear Token
          </button>
        </div>
      </form>
      <div
        v-if="patMessage"
        :class="[
          'mt-3 p-3 rounded-lg text-sm',
          patMessageType === 'success' ? 'bg-emerald-500/10 text-emerald-400' : 'bg-red-500/10 text-red-400'
        ]"
      >
        {{ patMessage }}
      </div>
    </div>

    <hr class="border-slate-800" />

    <div>
      <h3 class="text-lg font-semibold text-white mb-4">Preferences</h3>
      
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-slate-300 mb-2">
            Keyboard Shortcuts
          </label>
          <div class="space-y-2">
            <div class="flex items-center gap-2">
              <span class="w-32 text-xs text-slate-400">Toggle Comments</span>
              <input
                :value="keyboardShortcuts?.toggleComments || 'c'"
                @input="(e: any) => updateShortcut('toggleComments', e.target.value)"
                type="text"
                maxlength="1"
                class="w-16 px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-sm text-center uppercase focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div class="flex items-center gap-2">
              <span class="w-32 text-xs text-slate-400">Toggle File Tree</span>
              <input
                :value="keyboardShortcuts?.toggleFileTree || 'f'"
                @input="(e: any) => updateShortcut('toggleFileTree', e.target.value)"
                type="text"
                maxlength="1"
                class="w-16 px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-sm text-center uppercase focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div class="flex items-center gap-2">
              <span class="w-32 text-xs text-slate-400">Next File</span>
              <input
                :value="keyboardShortcuts?.nextFile || 'j'"
                @input="(e: any) => updateShortcut('nextFile', e.target.value)"
                type="text"
                maxlength="1"
                class="w-16 px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-sm text-center uppercase focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div class="flex items-center gap-2">
              <span class="w-32 text-xs text-slate-400">Previous File</span>
              <input
                :value="keyboardShortcuts?.previousFile || 'k'"
                @input="(e: any) => updateShortcut('previousFile', e.target.value)"
                type="text"
                maxlength="1"
                class="w-16 px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-sm text-center uppercase focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div class="flex items-center gap-2">
              <span class="w-32 text-xs text-slate-400">Next Comment</span>
              <input
                :value="keyboardShortcuts?.nextComment || 'n'"
                @input="(e: any) => updateShortcut('nextComment', e.target.value)"
                type="text"
                maxlength="1"
                class="w-16 px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-sm text-center uppercase focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div class="flex items-center gap-2">
              <span class="w-32 text-xs text-slate-400">Previous Comment</span>
              <input
                :value="keyboardShortcuts?.previousComment || 'p'"
                @input="(e: any) => updateShortcut('previousComment', e.target.value)"
                type="text"
                maxlength="1"
                class="w-16 px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-sm text-center uppercase focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { EyeIcon, EyeSlashIcon } from '@heroicons/vue/24/outline';
import { apiService } from '../../services/api';
import { useUserPreferences } from '../../composables/useUserPreferences';
import type { KeyboardShortcuts } from '../../types';

const { preferences, loadPreferences, setKeyboardShortcut } = useUserPreferences();

const localPAT = ref('');
const showPAT = ref(false);
const savingPAT = ref(false);
const patMessage = ref('');
const patMessageType = ref<'success' | 'error'>('success');
const hasExistingPAT = ref(false);

const keyboardShortcuts = computed(() => preferences.value.keyboardShortcuts);

const loadUserSettings = async () => {
  try {
    const settings = await apiService.getUserSettings();
    hasExistingPAT.value = settings.hasPersonalAccessToken;
  } catch (error) {
    console.error('Failed to load user settings:', error);
  }
};

const savePAT = async () => {
  savingPAT.value = true;
  patMessage.value = '';
  
  try {
    await apiService.updateUserSettings({ personalAccessToken: localPAT.value || null });
    hasExistingPAT.value = !!localPAT.value;
    localPAT.value = '';
    patMessage.value = 'Personal access token saved successfully';
    patMessageType.value = 'success';
  } catch (error) {
    patMessage.value = 'Failed to save personal access token';
    patMessageType.value = 'error';
  } finally {
    savingPAT.value = false;
  }
};

const clearPAT = async () => {
  savingPAT.value = true;
  patMessage.value = '';
  
  try {
    await apiService.updateUserSettings({ personalAccessToken: null });
    hasExistingPAT.value = false;
    localPAT.value = '';
    patMessage.value = 'Personal access token cleared';
    patMessageType.value = 'success';
  } catch (error) {
    patMessage.value = 'Failed to clear personal access token';
    patMessageType.value = 'error';
  } finally {
    savingPAT.value = false;
  }
};

const updateShortcut = async (key: keyof KeyboardShortcuts, value: string) => {
  const lowerValue = value.toLowerCase();
  if (lowerValue && lowerValue.length === 1) {
    try {
      await setKeyboardShortcut(key, lowerValue);
    } catch (error) {
      console.error('Failed to update keyboard shortcut:', error);
    }
  }
};

onMounted(() => {
  loadUserSettings();
  loadPreferences();
});
</script>
