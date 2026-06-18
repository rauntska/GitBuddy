<template>
  <div
    :class="[
      'group relative overflow-hidden rounded-xl border bg-gradient-to-br p-4 transition-all duration-300 hover:-translate-y-0.5',
      accentClasses.bg,
      accentClasses.border,
    ]"
  >
    <div :class="['absolute -top-8 -right-8 h-24 w-24 rounded-full blur-2xl opacity-50 group-hover:opacity-80 transition-opacity', accentClasses.glow]"></div>

    <div class="relative flex items-start justify-between gap-3">
      <div class="min-w-0">
        <div class="text-xs font-medium uppercase tracking-wider text-slate-400">{{ label }}</div>
        <div :class="['mt-2 text-2xl font-bold tabular-nums truncate', valueColor]">{{ value }}</div>
        <div v-if="sub" class="mt-1 text-xs text-slate-500 truncate">{{ sub }}</div>
      </div>
      <div v-if="icon" :class="['shrink-0 rounded-lg p-2 ring-1 ring-inset', accentClasses.iconBg, accentClasses.iconRing]">
        <component :is="icon" :class="['w-5 h-5', accentClasses.iconText]" />
      </div>
    </div>
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

const accentClasses = computed(() => {
  const map: Record<Accent, { bg: string; border: string; glow: string; iconBg: string; iconRing: string; iconText: string }> = {
    blue: {
      bg: 'from-blue-950/40 to-slate-900/20',
      border: 'border-blue-500/30 hover:border-blue-400/50',
      glow: 'bg-blue-500/30',
      iconBg: 'bg-blue-500/15',
      iconRing: 'ring-blue-500/30',
      iconText: 'text-blue-300',
    },
    emerald: {
      bg: 'from-emerald-950/40 to-slate-900/20',
      border: 'border-emerald-500/30 hover:border-emerald-400/50',
      glow: 'bg-emerald-500/30',
      iconBg: 'bg-emerald-500/15',
      iconRing: 'ring-emerald-500/30',
      iconText: 'text-emerald-300',
    },
    purple: {
      bg: 'from-purple-950/40 to-slate-900/20',
      border: 'border-purple-500/30 hover:border-purple-400/50',
      glow: 'bg-purple-500/30',
      iconBg: 'bg-purple-500/15',
      iconRing: 'ring-purple-500/30',
      iconText: 'text-purple-300',
    },
    amber: {
      bg: 'from-amber-950/40 to-slate-900/20',
      border: 'border-amber-500/30 hover:border-amber-400/50',
      glow: 'bg-amber-500/30',
      iconBg: 'bg-amber-500/15',
      iconRing: 'ring-amber-500/30',
      iconText: 'text-amber-300',
    },
    red: {
      bg: 'from-red-950/40 to-slate-900/20',
      border: 'border-red-500/30 hover:border-red-400/50',
      glow: 'bg-red-500/30',
      iconBg: 'bg-red-500/15',
      iconRing: 'ring-red-500/30',
      iconText: 'text-red-300',
    },
    orange: {
      bg: 'from-orange-950/40 to-slate-900/20',
      border: 'border-orange-500/30 hover:border-orange-400/50',
      glow: 'bg-orange-500/30',
      iconBg: 'bg-orange-500/15',
      iconRing: 'ring-orange-500/30',
      iconText: 'text-orange-300',
    },
    slate: {
      bg: 'from-slate-800/60 to-slate-900/20',
      border: 'border-slate-700/60 hover:border-slate-600',
      glow: 'bg-slate-500/20',
      iconBg: 'bg-slate-700/40',
      iconRing: 'ring-slate-600/40',
      iconText: 'text-slate-300',
    },
  };
  return map[props.accent];
});
</script>
