<template>
  <div class="border-t border-slate-800 pt-3">
    <div class="flex items-start justify-between gap-2">
      <div class="text-[11px] uppercase tracking-wider text-slate-500">{{ label }}</div>
      <component
        v-if="icon"
        :is="icon"
        :class="['w-3.5 h-3.5 shrink-0 mt-0.5', iconText]"
      />
    </div>
    <div :class="['mt-1 text-xl sm:text-2xl font-semibold tabular-nums truncate font-mono', valueColor || 'text-slate-100']">{{ value }}</div>
    <div v-if="sub" class="mt-1 text-[11px] text-slate-500 truncate">{{ sub }}</div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { Component } from 'vue';

type Accent = 'blue' | 'emerald' | 'purple' | 'amber' | 'red' | 'orange' | 'slate';

const props = withDefaults(defineProps<{
  label: string;
  value: string | number;
  sub?: string;
  icon?: Component;
  accent?: Accent;
  valueColor?: string;
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
