<template>
  <div
    :class="['flex items-center font-mono leading-none', compact ? 'text-sm' : 'text-base']"
    :title="tooltip"
  >
    <span :class="['font-semibold', glyph.color]">{{ glyph.char }}</span>
    <span v-if="!compact && status" :class="['ml-1 text-xs font-medium uppercase tracking-wide', glyph.color]">{{ statusLabel }}</span>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { getCIGlyph } from '../utils/prHelpers';

const props = withDefaults(defineProps<{
  status?: 'SUCCESS' | 'FAILURE' | 'PENDING' | 'NEUTRAL' | null;
  totalCount?: number;
  showCount?: boolean;
  compact?: boolean;
}>(), {
  status: null,
  totalCount: 0,
  showCount: false,
  compact: false,
});

const glyph = computed(() => getCIGlyph(props.status));

const statusLabel = computed(() => {
  switch (props.status) {
    case 'SUCCESS': return 'pass';
    case 'FAILURE': return 'fail';
    case 'PENDING': return 'pend';
    default: return 'none';
  }
});

const tooltip = computed(() => {
  if (!props.status) return 'No CI/CD checks configured';
  const label = props.status === 'SUCCESS' ? 'passing'
    : props.status === 'FAILURE' ? 'failing'
    : props.status === 'PENDING' ? 'pending'
    : 'none';
  const count = props.totalCount > 1 ? ` (${props.totalCount} checks)` : '';
  return `${label}${count}`;
});
</script>
