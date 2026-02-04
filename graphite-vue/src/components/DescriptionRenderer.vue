<template>
  <div ref="container" class="prose prose-invert prose-sm max-w-none text-sm text-slate-300 leading-relaxed max-h-96 overflow-y-auto markdown-content">
    <div v-if="loading" class="flex items-center justify-center py-8">
      <svg class="animate-spin h-8 w-8 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0-3.042 1.135 5.824 3 7.938l3-2.647z"></path>
      </svg>
      <span class="ml-2 text-sm text-slate-400">Loading images...</span>
    </div>
    <div v-else v-html="processedHtml" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';
import apiClient from '../utils/api';

const props = defineProps<{
  content: string;
}>();

const container = ref<HTMLDivElement | null>(null);
const processedHtml = ref('');
const processedImages = ref<Map<string, string>>(new Map());
const loading = ref(false);

const processImages = async () => {
  if (!container.value) return;

  const content = props.content || '';
  if (!content) {
    processedHtml.value = '';
    return;
  }

  loading.value = true;

  try {
    console.log('Processing description with images');

    const envUrl = import.meta.env.VITE_API_BASE_URL;
    const apiBaseUrl = typeof envUrl === 'string' ? envUrl : 'http://localhost:5247/api';
    const proxyUrl = `${apiBaseUrl}/images/proxy`;

    const html = content.replace(/\r\n/g, '\n');

    // Find all GitHub user-attachment images
    const imgRegex = /<img[^>]*src="(https:\/\/github\.com\/user-attachments\/[^"]*)"[^>]*>/g;
    const imageUrls: string[] = [];
    let match: RegExpExecArray | null;

    while ((match = imgRegex.exec(html)) !== null) {
      if (match[1]) {
        imageUrls.push(match[1]);
      }
    }

    console.log('Found GitHub images:', imageUrls.length);

    // Fetch all images with authentication
    for (const imageUrl of imageUrls) {
      try {
        const response = await apiClient.get(`${proxyUrl}?url=${encodeURIComponent(imageUrl)}`, {
          responseType: 'blob',
        });

        const blob = response.data;
        const objectUrl = URL.createObjectURL(blob);
        processedImages.value.set(imageUrl, objectUrl);
        console.log('Successfully loaded image:', imageUrl);
      } catch (error: any) {
        console.error('Failed to load image:', imageUrl, error);
        // Keep original URL on error (will show 401 but at least doesn't crash)
      }
    }

    // Replace all GitHub image URLs with their blob URLs
    let newHtml = html;
    processedImages.value.forEach((blobUrl, originalUrl) => {
      newHtml = newHtml.replace(originalUrl, blobUrl);
    });

    processedHtml.value = newHtml;
    console.log('Processed HTML with authenticated images');
  } catch (error) {
    console.error('Error processing images:', error);
    processedHtml.value = content;
  } finally {
    loading.value = false;
  }
};

const revokeObjectUrls = () => {
  processedImages.value.forEach((url) => URL.revokeObjectURL(url));
  processedImages.value.clear();
};

watch(() => props.content, () => {
  processImages();
});

onMounted(() => {
  processImages();
});

onUnmounted(() => {
  revokeObjectUrls();
});
</script>

