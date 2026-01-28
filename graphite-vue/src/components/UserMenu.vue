<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';

const authStore = useAuthStore();
const router = useRouter();
const isOpen = ref(false);

const handleLogout = async () => {
  authStore.clearAuth();
  isOpen.value = false;
  await router.push('/');
};
</script>

<template>
  <div class="relative">
    <button
      @click="isOpen = !isOpen"
      class="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-gray-100 transition-colors"
    >
      <img
        v-if="authStore.avatarUrl"
        :src="authStore.avatarUrl"
        :alt="authStore.username"
        class="w-8 h-8 rounded-full"
      />
      <div
        v-else
        class="w-8 h-8 rounded-full bg-gray-300 flex items-center justify-center"
      >
        <span class="text-sm font-medium text-gray-600">
          {{ authStore.username.charAt(0).toUpperCase() }}
        </span>
      </div>
      <span class="text-sm font-medium text-gray-700">{{ authStore.username }}</span>
      <svg class="w-4 h-4 text-gray-500" :class="{ 'rotate-180': isOpen }">
        <path fill="currentColor" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z"/>
      </svg>
    </button>

    <div
      v-if="isOpen"
      class="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-50"
      @click="isOpen = false"
    >
      <button
        @click="handleLogout"
        class="w-full px-4 py-2 text-left text-sm text-red-600 hover:bg-red-50 transition-colors"
      >
        Logout
      </button>
    </div>
  </div>
</template>
