<template>
  <div class="flex flex-wrap items-end gap-x-6 gap-y-3 mb-4 sm:mb-6">
    <div
      v-for="(stat, key) in statsConfig"
      :key="key"
      class="flex flex-col"
    >
      <div class="flex items-center gap-1.5">
        <span :class="['font-mono text-sm leading-none', stat.glyphColor]">{{ stat.glyph }}</span>
        <span class="text-xl sm:text-2xl font-semibold text-slate-100 tabular-nums">{{ stats[key] }}</span>
      </div>
      <div class="text-[11px] uppercase tracking-wider text-slate-500 mt-1">{{ stat.label }}</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { PRStats } from '../types';

const props = defineProps<{
  stats: PRStats;
}>();

const statsConfig = computed(() => ({
  totalOpen:        { label: 'Open',     glyph: '●', glyphColor: 'text-blue-400' },
  awaitingReview:   { label: 'Review',   glyph: '⋯', glyphColor: 'text-amber-400' },
  approved:         { label: 'Approved', glyph: '✓', glyphColor: 'text-emerald-400' },
  draft:            { label: 'Drafts',   glyph: '○', glyphColor: 'text-slate-500' },
  totalComments:    { label: 'Comments', glyph: '◐', glyphColor: 'text-violet-400' },
  resolvedComments: { label: 'Resolved', glyph: '✓', glyphColor: 'text-cyan-400' },
  pendingComments:  { label: 'Pending',  glyph: '!', glyphColor: 'text-orange-400' },
}));
</script>
