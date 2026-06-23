<template>
  <div class="flex items-center gap-2 mb-3">
    <component :is="icon" :class="['w-4 h-4 shrink-0', iconText]" />
    <div class="flex-1 min-w-0">
      <h3 class="text-sm font-semibold uppercase tracking-wider text-slate-300">{{ title }}</h3>
      <p v-if="subtitle" class="text-[11px] text-slate-500 mt-0.5 truncate">{{ subtitle }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { Component } from 'vue';

type Accent = 'blue' | 'emerald' | 'purple' | 'amber' | 'red' | 'orange' | 'slate';

const props = withDefaults(defineProps<{
  title: string;
  subtitle?: string;
  icon: Component;
  accent?: Accent;
}>(), {
  accent: 'slate',
});

const iconColorMap: Record<Accent, string> = {
  blue: 'text-blue-400',
  emerald: 'text-emerald-400',
  purple: 'text-violet-400',
  amber: 'text-amber-400',
  red: 'text-red-400',
  orange: 'text-orange-400',
  slate: 'text-slate-400',
};

const iconText = computed(() => iconColorMap[props.accent]);
</script>
