<template>
  <Transition name="slide-down">
    <div
      v-if="show"
      class="fixed top-0 left-0 right-0 z-50 bg-slate-800 border-b border-blue-500/30 shadow-lg shadow-blue-500/5"
    >
      <div class="max-w-5xl mx-auto px-4 py-3 flex items-center justify-between gap-4">
        <div class="flex items-center gap-3 min-w-0">
          <div class="flex-shrink-0 w-8 h-8 rounded-full bg-blue-500/20 flex items-center justify-center">
            <svg class="w-4 h-4 text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
            </svg>
          </div>
          <p class="text-sm text-slate-300 truncate">
            Enable desktop notifications to stay updated on PR activity?
          </p>
        </div>
        <div class="flex items-center gap-2 flex-shrink-0">
          <button
            @click="enable"
            class="px-3 py-1.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-500 rounded-lg transition-colors"
          >
            Enable
          </button>
          <button
            @click="dismiss"
            class="px-3 py-1.5 text-sm font-medium text-slate-400 hover:text-slate-200 transition-colors"
          >
            Not now
          </button>
          <button
            @click="dismissPermanently"
            class="px-3 py-1.5 text-sm font-medium text-slate-500 hover:text-slate-300 transition-colors"
            title="Don't show again"
          >
            Dismiss
          </button>
        </div>
      </div>
    </div>
  </Transition>
</template>

<script setup lang="ts">
import { useBrowserNotifications } from '../composables/useBrowserNotifications';

const { showPermissionBanner, requestPermission, dismissBanner } = useBrowserNotifications();

const show = showPermissionBanner;

async function enable() {
  await requestPermission();
  dismissBanner();
}

function dismiss() {
  dismissBanner();
}

function dismissPermanently() {
  dismissBanner();
}
</script>

<style scoped>
.slide-down-enter-active,
.slide-down-leave-active {
  transition: all 0.3s ease;
}
.slide-down-enter-from,
.slide-down-leave-to {
  transform: translateY(-100%);
  opacity: 0;
}
</style>
