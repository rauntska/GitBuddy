<template>
  <div 
    class="flex items-center gap-1.5 cursor-default"
    :title="tooltipText"
  >
    <span 
      :class="dotClass"
      class="w-2 h-2 rounded-full transition-colors duration-300"
    ></span>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { ConnectionState } from '../composables/useSignalR';

const props = defineProps<{
  state: ConnectionState;
}>();

const dotClass = computed(() => {
  switch (props.state) {
    case 'connected':
      return 'bg-emerald-500';
    case 'reconnecting':
      return 'bg-amber-500 animate-pulse';
    case 'disconnected':
    default:
      return 'bg-slate-500';
  }
});

const tooltipText = computed(() => {
  switch (props.state) {
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
