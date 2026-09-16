<template>
  <div v-if="sections.length > 0" class="border-b border-slate-800 pb-1.5 mb-2.5">
    <div class="flex items-center gap-2 mb-1">
      <h2 class="text-sm font-semibold uppercase tracking-wider text-slate-300">Sections</h2>
      <span class="font-mono text-xs text-slate-500 tabular-nums">{{ summaryLine }}</span>
    </div>

    <div class="flex flex-wrap gap-1.5">
      <button
        v-for="section in sections"
        :key="section.domId"
        type="button"
        @click="$emit('select', section.domId)"
        :title="section.path.join(' › ')"
        class="flex items-center gap-1.5 px-2 py-1 rounded border border-slate-800 text-xs
               hover:bg-slate-800/40 hover:border-slate-700 hover:-translate-y-px
               transition-all duration-150 ease-out"
        :class="section.status === 'unchanged' ? 'text-slate-500' : 'text-slate-200'"
      >
        <span class="font-mono text-xs leading-none" :class="glyphClass(section.status)">
          {{ glyph(section.status) }}
        </span>
        <span class="truncate max-w-[16rem]">{{ section.title }}</span>
        <span class="font-mono text-[10px] text-slate-500 tabular-nums">
          {{ metaFor(section) }}
        </span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { PairStatus, SectionSummary } from '../utils/proseDiff/types';

const props = defineProps<{ sections: SectionSummary[] }>();

defineEmits<{ select: [domId: string] }>();

const summaryLine = computed(() => {
  const counts = { added: 0, removed: 0, changed: 0, unchanged: 0 };
  for (const s of props.sections) counts[s.status]++;

  const parts = [`${props.sections.length}`];
  if (counts.added) parts.push(`${counts.added} added`);
  if (counts.removed) parts.push(`${counts.removed} removed`);
  if (counts.changed) parts.push(`${counts.changed} changed`);
  if (counts.unchanged) parts.push(`${counts.unchanged} unchanged`);
  return parts.join(' · ');
});

function glyph(status: PairStatus): string {
  switch (status) {
    case 'added': return '+';
    case 'removed': return '−';
    case 'changed': return '◐';
    default: return '–';
  }
}

function glyphClass(status: PairStatus): string {
  switch (status) {
    case 'added': return 'text-emerald-400';
    case 'removed': return 'text-red-400';
    case 'changed': return 'text-amber-400';
    default: return 'text-slate-600';
  }
}

function metaFor(section: SectionSummary): string {
  if (section.status === 'added') return 'new';
  if (section.status === 'removed') return 'removed';
  if (section.status === 'unchanged') return 'unchanged';
  return section.changedCount === 1 ? '1 block' : `${section.changedCount} blocks`;
}
</script>
