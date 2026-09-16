<template>
  <div class="py-4 border-b border-slate-800 last:border-b-0">
    <div class="flex items-center gap-2.5 mb-1.5">
      <span
        :class="[
          'inline-flex items-center gap-1.5 px-2 py-0.5 rounded text-xs font-medium',
          'bg-slate-800/60 border border-slate-700/60',
          categoryStyle.color,
        ]"
      >
        <span class="font-mono leading-none">{{ categoryStyle.glyph }}</span>
        <span>{{ entry.category }}</span>
      </span>
      <span class="font-mono text-xs text-slate-500">{{ formattedDate }}</span>
    </div>
    <h3 class="text-[15px] text-slate-200 mb-1.5">{{ entry.title }}</h3>
    <div class="prose prose-invert prose-sm max-w-none text-sm text-slate-300 leading-relaxed markdown-content" v-html="html" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { ChangelogEntry } from '../types';
import { useProxiedHtml } from '../composables/useProxiedHtml';

const props = defineProps<{
  entry: ChangelogEntry;
}>();

const { html } = useProxiedHtml(() => props.entry.body, { isMarkdown: true });

const categoryStyles: Record<ChangelogEntry['category'], { glyph: string; color: string }> = {
  Feature: { glyph: '●', color: 'text-blue-400' },
  Improvement: { glyph: '◐', color: 'text-violet-400' },
  Fix: { glyph: '✓', color: 'text-emerald-400' },
};

const categoryStyle = computed(() => categoryStyles[props.entry.category] ?? categoryStyles.Improvement);

const formattedDate = computed(() => {
  const date = new Date(`${props.entry.publishedOn}T00:00:00Z`);
  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric', timeZone: 'UTC' });
});
</script>
