<template>
  <img
    v-if="imageData"
    :src="imageData"
    :alt="alt"
    :width="width"
    :height="height"
    :class="className"
    v-bind="$attrs"
  />
  <div v-else class="flex items-center justify-center bg-slate-800/50 rounded-lg p-8">
    <svg class="animate-spin h-8 w-8 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0-3.042 1.135 5.824 3 7.938l3-2.647z"></path>
    </svg>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import apiClient from '../utils/api';

const props = defineProps<{
  src: string;
  alt?: string;
  width?: string | number;
  height?: string | number;
  className?: string;
}>();

const imageData = ref<string | null>(null);
let objectUrl: string | null = null;

const fetchAuthenticatedImage = async () => {
  try {
    const response = await apiClient.get(props.src, {
      responseType: 'blob',
    });

    const blob = response.data;
    objectUrl = URL.createObjectURL(blob);
    imageData.value = objectUrl;
  } catch (error) {
    console.error('Failed to fetch authenticated image:', error);
    // Fallback to original src
    imageData.value = props.src;
  }
};

onMounted(() => {
  fetchAuthenticatedImage();
});

onUnmounted(() => {
  if (objectUrl) {
    URL.revokeObjectURL(objectUrl);
  }
});
</script>
