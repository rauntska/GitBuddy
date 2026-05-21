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
      <h3 class="text-lg font-semibold text-white mb-4">Desktop Notifications</h3>
      <p class="text-sm text-slate-400 mb-4">
        Receive browser notifications for PR activity when the tab is in the background.
      </p>

      <!-- Permission status -->
      <div class="flex items-center gap-3 mb-4">
        <div class="flex items-center gap-2">
          <span
            class="w-2.5 h-2.5 rounded-full"
            :class="{
              'bg-green-500': notificationPermission === 'granted',
              'bg-yellow-500': notificationPermission === 'default',
              'bg-red-500': notificationPermission === 'denied'
            }"
          ></span>
          <span class="text-sm text-slate-300">
            <template v-if="notificationPermission === 'granted'">Notifications enabled</template>
            <template v-else-if="notificationPermission === 'default'">Not yet enabled</template>
            <template v-else>Blocked by browser</template>
          </span>
        </div>
        <button
          v-if="notificationPermission === 'default'"
          @click="enableNotifications"
          class="px-3 py-1.5 text-xs font-medium text-white bg-blue-600 hover:bg-blue-500 rounded-lg transition-colors"
        >
          Enable
        </button>
        <span v-else-if="notificationPermission === 'denied'" class="text-xs text-slate-500">
          Enable in your browser settings
        </span>
      </div>

      <!-- Master toggle -->
      <div class="space-y-4">
        <label class="flex items-center justify-between">
          <span class="text-sm font-medium text-slate-300">Enable desktop notifications</span>
          <button
            type="button"
            role="switch"
            :aria-checked="notifPrefs.enabled"
            @click="updateNotifPref('enabled', !notifPrefs.enabled)"
            class="relative inline-flex h-6 w-11 items-center rounded-full transition-colors"
            :class="notifPrefs.enabled ? 'bg-blue-600' : 'bg-slate-700'"
          >
            <span
              class="inline-block h-4 w-4 transform rounded-full bg-white transition-transform"
              :class="notifPrefs.enabled ? 'translate-x-6' : 'translate-x-1'"
            ></span>
          </button>
        </label>

        <!-- Event toggles -->
        <div v-if="notifPrefs.enabled" class="pl-2 border-l-2 border-slate-700 space-y-3">
          <p class="text-xs text-slate-500 uppercase tracking-wider font-medium">Notify me when...</p>
          <label v-for="(label, key) in eventLabels" :key="key" class="flex items-center justify-between">
            <span class="text-sm text-slate-400">{{ label }}</span>
            <button
              type="button"
              role="switch"
              :aria-checked="notifPrefs.events[key as keyof typeof notifPrefs.events]"
              @click="toggleEvent(key as keyof typeof notifPrefs.events)"
              class="relative inline-flex h-5 w-9 items-center rounded-full transition-colors"
              :class="notifPrefs.events[key as keyof typeof notifPrefs.events] ? 'bg-blue-600' : 'bg-slate-700'"
            >
              <span
                class="inline-block h-3 w-3 transform rounded-full bg-white transition-transform"
                :class="notifPrefs.events[key as keyof typeof notifPrefs.events] ? 'translate-x-5' : 'translate-x-1'"
              ></span>
            </button>
          </label>
        </div>

        <!-- Quiet hours -->
        <div v-if="notifPrefs.enabled" class="pl-2 border-l-2 border-slate-700 space-y-3">
          <label class="flex items-center justify-between">
            <span class="text-sm text-slate-400">Quiet hours</span>
            <button
              type="button"
              role="switch"
              :aria-checked="notifPrefs.quietHours.enabled"
              @click="updateQuietHours('enabled', !notifPrefs.quietHours.enabled)"
              class="relative inline-flex h-5 w-9 items-center rounded-full transition-colors"
              :class="notifPrefs.quietHours.enabled ? 'bg-blue-600' : 'bg-slate-700'"
            >
              <span
                class="inline-block h-3 w-3 transform rounded-full bg-white transition-transform"
                :class="notifPrefs.quietHours.enabled ? 'translate-x-5' : 'translate-x-1'"
              ></span>
            </button>
          </label>
          <div v-if="notifPrefs.quietHours.enabled" class="flex items-center gap-2">
            <input
              type="time"
              :value="notifPrefs.quietHours.start"
              @change="(e: any) => updateQuietHours('start', e.target.value)"
              class="px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <span class="text-xs text-slate-500">to</span>
            <input
              type="time"
              :value="notifPrefs.quietHours.end"
              @change="(e: any) => updateQuietHours('end', e.target.value)"
              class="px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>

        <!-- Test button -->
        <button
          v-if="notifPrefs.enabled && notificationPermission === 'granted'"
          @click="testNotification"
          class="px-3 py-2 text-sm font-medium text-slate-300 bg-slate-800 hover:bg-slate-700 rounded-lg transition-colors"
        >
          Send test notification
        </button>
      </div>
    </div>

    <hr class="border-slate-800" />

    <div>
      <h3 class="text-lg font-semibold text-white mb-4">Preferences</h3>

      <div class="space-y-4">
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { EyeIcon, EyeSlashIcon } from '@heroicons/vue/24/outline';
import { apiService } from '../../services/api';
import { useUserPreferences } from '../../composables/useUserPreferences';
import { useBrowserNotifications } from '../../composables/useBrowserNotifications';
import type { NotificationPreferences } from '../../types';

const { preferences, loadPreferences, updatePreferences } = useUserPreferences();
const browserNotifications = useBrowserNotifications();

const localPAT = ref('');
const showPAT = ref(false);
const savingPAT = ref(false);
const patMessage = ref('');
const patMessageType = ref<'success' | 'error'>('success');
const hasExistingPAT = ref(false);


const notificationPermission = computed(() => browserNotifications.permissionStatus.value);

const notifPrefs = computed(() => browserNotifications.preferences.value);

const eventLabels: Record<string, string> = {
  prCreated: 'A new PR is created',
  reviewAdded: 'A review is submitted on my PR',
  commentAdded: 'A comment is added',
  threadResolved: 'A review thread is resolved',
  prMerged: 'My PR is merged',
  checkFailed: 'CI checks fail on my PR',
};

async function enableNotifications() {
  await browserNotifications.requestPermission();
}

async function updateNotifPref(key: string, value: boolean) {
  const updated = { ...notifPrefs.value, [key]: value };
  browserNotifications.loadPreferences(updated);
  await saveNotifPrefs(updated);
}

async function toggleEvent(key: keyof NotificationPreferences['events']) {
  const updated: NotificationPreferences = {
    ...notifPrefs.value,
    events: { ...notifPrefs.value.events, [key]: !notifPrefs.value.events[key] },
  };
  browserNotifications.loadPreferences(updated);
  await saveNotifPrefs(updated);
}

async function updateQuietHours(key: string, value: string | boolean) {
  const updated: NotificationPreferences = {
    ...notifPrefs.value,
    quietHours: { ...notifPrefs.value.quietHours, [key]: value },
  };
  browserNotifications.loadPreferences(updated);
  await saveNotifPrefs(updated);
}

async function saveNotifPrefs(notifPrefs: NotificationPreferences) {
  try {
    await updatePreferences({ notificationPreferences: notifPrefs });
  } catch (error) {
    console.error('Failed to save notification preferences:', error);
  }
}

function testNotification() {
  browserNotifications.sendTestNotification();
}

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

onMounted(async () => {
  loadUserSettings();
  await loadPreferences();
  browserNotifications.loadPreferences(preferences.value.notificationPreferences);
});
</script>
