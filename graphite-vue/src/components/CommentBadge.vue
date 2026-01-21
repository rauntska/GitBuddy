<template>
  <div
    v-if="count > 0"
    class="flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium"
    :class="badgeClass"
    :title="`${count} comment${count !== 1 ? 's' : ''}`"
  >
    <svg class="w-3.5 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
    </svg>
    {{ count }}
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  count: number;
}>();

const badgeClass = computed(() => {
  if (props.count > 5) return 'bg-red-500/20 text-red-400';
  if (props.count > 2) return 'bg-orange-500/20 text-orange-400';
  return 'bg-blue-500/20 text-blue-400';
});
</script>
