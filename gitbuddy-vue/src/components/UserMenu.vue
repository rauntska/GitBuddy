<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { useSignalR } from '../composables/useSignalR';

const authStore = useAuthStore();
const router = useRouter();
const isOpen = ref(false);
const { connectionState } = useSignalR();

const handleLogout = async () => {
  authStore.clearAuth();
  isOpen.value = false;
  await router.push('/');
};

const ringClass = computed(() => {
  const base = 'ring-2 ring-offset-1 ring-offset-slate-900';
  switch (connectionState.value) {
    case 'connected':
      return `${base} ring-emerald-500`;
    case 'reconnecting':
      return `${base} ring-amber-500 animate-pulse`;
    case 'disconnected':
    default:
      return `${base} ring-slate-500`;
  }
});

const connectionTooltip = computed(() => {
  switch (connectionState.value) {
    case 'connected':
      return 'Real-time updates connected';
    case 'reconnecting':
      return 'Reconnecting...';
    case 'disconnected':
    default:
      return 'Real-time updates disconnected';
  }
});
</script>

<template>
  <div class="relative">
    <button
      @click="isOpen = !isOpen"
      class="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-slate-800 transition-colors"
    >
      <img
        v-if="authStore.avatarUrl"
        :src="authStore.avatarUrl"
        :alt="authStore.username"
        :class="ringClass"
        :title="connectionTooltip"
        class="w-8 h-8 rounded-full"
      />
      <div
        v-else
        :class="ringClass"
        :title="connectionTooltip"
        class="w-8 h-8 rounded-full bg-slate-700 flex items-center justify-center"
      >
        <span class="text-sm font-medium text-slate-300">
          {{ authStore.username.charAt(0).toUpperCase() }}
        </span>
      </div>
      <span class="text-sm font-medium text-slate-300">{{ authStore.username }}</span>
      <svg class="w-4 h-4 text-slate-400" :class="{ 'rotate-180': isOpen }">
        <path fill="currentColor" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z"/>
      </svg>
    </button>

    <div
      v-if="isOpen"
      class="absolute right-0 mt-2 w-48 bg-slate-800 rounded-lg shadow-lg border border-slate-700 py-1 z-50"
      @click="isOpen = false"
    >
      <div class="px-4 py-2 border-b border-slate-700">
        <div class="flex items-center gap-2">
          <span class="text-xs px-2 py-0.5 rounded-full" 
                :class="authStore.isAdmin ? 'bg-purple-500/20 text-purple-400' : 'bg-slate-700 text-slate-400'">
            {{ authStore.isAdmin ? 'Admin' : 'Developer' }}
          </span>
        </div>
      </div>
      <button
        @click="handleLogout"
        class="w-full px-4 py-2 text-left text-sm text-red-400 hover:bg-slate-700 transition-colors"
      >
        Logout
      </button>
    </div>
  </div>
</template>
