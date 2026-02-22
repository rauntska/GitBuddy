<template>
  <span 
    :class="[badgeClass, compact ? 'px-1 py-0.5 text-[9px]' : 'px-1.5 py-0.5 text-[10px]']"
    :title="tooltipText"
    class="rounded font-bold uppercase tracking-wide">
    {{ size }}
  </span>
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
