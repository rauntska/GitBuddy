<template>
  <div class="flex items-center gap-3 mb-4">
    <div :class="['rounded-lg p-1.5 ring-1 ring-inset', iconBg, iconRing]">
      <component :is="icon" :class="['w-4 h-4', iconText]" />
    </div>
    <div class="flex-1 min-w-0">
      <h3 class="text-base font-semibold text-white">{{ title }}</h3>
      <p v-if="subtitle" class="text-xs text-slate-500 mt-0.5 truncate">{{ subtitle }}</p>
    </div>
    <div :class="['h-px flex-1 max-w-[200px] bg-gradient-to-r', accentLine]"></div>
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

const map: Record<Accent, { iconBg: string; iconRing: string; iconText: string; accentLine: string }> = {
  blue: { iconBg: 'bg-blue-500/15', iconRing: 'ring-blue-500/30', iconText: 'text-blue-300', accentLine: 'from-blue-500/60 to-transparent' },
  emerald: { iconBg: 'bg-emerald-500/15', iconRing: 'ring-emerald-500/30', iconText: 'text-emerald-300', accentLine: 'from-emerald-500/60 to-transparent' },
  purple: { iconBg: 'bg-purple-500/15', iconRing: 'ring-purple-500/30', iconText: 'text-purple-300', accentLine: 'from-purple-500/60 to-transparent' },
  amber: { iconBg: 'bg-amber-500/15', iconRing: 'ring-amber-500/30', iconText: 'text-amber-300', accentLine: 'from-amber-500/60 to-transparent' },
  red: { iconBg: 'bg-red-500/15', iconRing: 'ring-red-500/30', iconText: 'text-red-300', accentLine: 'from-red-500/60 to-transparent' },
  orange: { iconBg: 'bg-orange-500/15', iconRing: 'ring-orange-500/30', iconText: 'text-orange-300', accentLine: 'from-orange-500/60 to-transparent' },
  slate: { iconBg: 'bg-slate-700/40', iconRing: 'ring-slate-600/40', iconText: 'text-slate-300', accentLine: 'from-slate-600/60 to-transparent' },
};

const { iconBg, iconRing, iconText, accentLine } = computed(() => map[props.accent]).value;
</script>
