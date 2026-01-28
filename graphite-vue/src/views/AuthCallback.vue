<script setup lang="ts">
import { onMounted } from 'vue';
import { useAuth } from '../composables/useAuth';

const { handleCallback } = useAuth();

onMounted(() => {
  handleCallback();
});
</script>

<template>
  <div class="min-h-screen flex items-center justify-center">
    <div class="text-center">
      <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-gray-900 mx-auto"></div>
      <p class="mt-4 text-gray-600">Logging in...</p>
    </div>
  </div>
</template>
