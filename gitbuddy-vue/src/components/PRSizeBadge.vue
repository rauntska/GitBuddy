<template>
  <span
    :class="[badgeClass, compact ? 'text-[10px]' : 'text-xs', 'font-mono font-semibold tracking-wide']"
    :title="tooltipText"
  >{{ size }}</span>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { getPRSize, getSizeBadgeClass } from '../utils/prHelpers';

const props = defineProps<{
  additions: number;
  deletions: number;
  compact?: boolean;
}>();

const totalLines = computed(() => props.additions + props.deletions);
const size = computed(() => getPRSize(totalLines.value));
const badgeClass = computed(() => getSizeBadgeClass(totalLines.value));
const tooltipText = computed(() =>
  `${totalLines.value} total lines changed (${size.value})`
);
</script>
