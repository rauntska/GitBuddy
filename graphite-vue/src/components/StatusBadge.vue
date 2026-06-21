<template>
  <span
    :class="[
      'inline-flex items-center gap-1.5 px-2 py-0.5 rounded text-xs font-medium',
      'bg-slate-800/60 border border-slate-700/60',
    ]"
  >
    <span :class="['font-mono leading-none', glyph.color]">{{ glyph.char }}</span>
    <span :class="glyph.color">{{ label }}</span>
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { getStatusGlyph } from '../utils/prHelpers';

const props = defineProps<{
  status: string;
}>();

const labels: Record<string, string> = {
  AwaitingReview: 'Awaiting Review',
  Approved: 'Approved',
  Reviewed: 'Reviewed',
  ChangesRequested: 'Changes Requested',
  Draft: 'Draft',
  Merged: 'Merged',
  Closed: 'Closed',
};

const label = computed(() => labels[props.status] || props.status);
const glyph = computed(() => getStatusGlyph(props.status));
</script>
