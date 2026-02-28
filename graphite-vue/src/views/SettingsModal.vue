<template>
  <div
    class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
    @click.self="$emit('close')"
  >
    <div class="w-full max-w-md bg-slate-900 border border-slate-700 rounded-xl shadow-2xl">
      <div class="flex items-center justify-between p-5 border-b border-slate-800">
        <h2 class="text-lg font-semibold text-white">Quick Settings</h2>
        <button
          @click="$emit('close')"
          class="text-slate-500 hover:text-slate-300 transition-colors p-1"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <div class="p-5 space-y-4">
        <p class="text-sm text-slate-400">
          For full settings including GitHub App configuration and Administration, visit the 
          <router-link to="/settings" @click="$emit('close')" class="text-blue-400 hover:text-blue-300">
            Settings page
          </router-link>.
        </p>

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

        <div
          v-if="message"
          :class="[
            'p-3 rounded-lg text-sm',
            messageType === 'success' ? 'bg-emerald-500/10 text-emerald-400' : 'bg-red-500/10 text-red-400'
          ]"
        >
          {{ message }}
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
            @click="savePAT"
            :disabled="saving"
            class="flex-1 px-4 py-2.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            <svg
              v-if="saving"
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
            {{ saving ? 'Saving...' : 'Save' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { EyeIcon, EyeSlashIcon } from '@heroicons/vue/24/outline';
import { apiService } from '../services/api';

const emit = defineEmits<{
  close: [];
  saved: [];
}>();

const localPAT = ref('');
const showPAT = ref(false);
const saving = ref(false);
const message = ref('');
const messageType = ref<'success' | 'error'>('success');
const hasExistingPAT = ref(false);

const loadUserSettings = async () => {
  try {
    const settings = await apiService.getUserSettings();
    hasExistingPAT.value = settings.hasPersonalAccessToken;
  } catch (error) {
    console.error('Failed to load user settings:', error);
  }
};

const savePAT = async () => {
  saving.value = true;
  message.value = '';
  
  try {
    await apiService.updateUserSettings({ 
      personalAccessToken: localPAT.value || null 
    });
    hasExistingPAT.value = !!localPAT.value;
    localPAT.value = '';
    message.value = 'Settings saved successfully';
    messageType.value = 'success';
    emit('saved');
  } catch (error) {
    message.value = 'Failed to save settings';
    messageType.value = 'error';
  } finally {
    saving.value = false;
  }
};

onMounted(() => {
  loadUserSettings();
});
</script>
