<template>
  <div class="flex flex-wrap items-center gap-3 rounded-xl border border-slate-700/60 bg-slate-900/40 p-3 backdrop-blur-sm">
    <div class="inline-flex gap-1 bg-slate-950/40 rounded-lg p-1 border border-slate-800 w-full sm:w-auto">
      <button
        v-for="p in presets"
        :key="p.value"
        @click="selectPreset(p.value)"
        :class="[
          'flex-1 sm:flex-initial px-3 py-1.5 rounded-md text-xs font-semibold transition-all duration-200',
          modelPreset === p.value
            ? 'bg-gradient-to-r from-blue-600 to-blue-500 text-white shadow-md shadow-blue-500/30'
            : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
        ]"
      >
        {{ p.label }}
      </button>
    </div>

    <div class="h-px w-full sm:h-6 sm:w-px bg-slate-700/60"></div>

    <div class="flex flex-wrap items-center gap-2 text-sm w-full sm:w-auto">
      <span class="text-slate-500 text-xs uppercase tracking-wider">From</span>
      <input
        :value="customFrom"
        @input="emit('update:customFrom', ($event.target as HTMLInputElement).value)"
        type="date"
        class="flex-1 sm:flex-initial min-w-[120px] px-2.5 py-1.5 rounded-lg bg-slate-800/80 border border-slate-700 text-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500/50 transition-all"
      />
      <ArrowRightIcon class="w-3.5 h-3.5 text-slate-600 hidden sm:block" />
      <span class="text-slate-500 text-xs uppercase tracking-wider sm:ml-2">To</span>
      <input
        :value="customTo"
        @input="emit('update:customTo', ($event.target as HTMLInputElement).value)"
        type="date"
        class="flex-1 sm:flex-initial min-w-[120px] px-2.5 py-1.5 rounded-lg bg-slate-800/80 border border-slate-700 text-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500/50 transition-all"
      />
      <button
        @click="emit('applyCustom')"
        :disabled="!customFrom || !customTo"
        class="w-full sm:w-auto ml-0 sm:ml-1 px-3 py-1.5 rounded-lg bg-gradient-to-r from-blue-600 to-blue-500 hover:from-blue-500 hover:to-blue-400 text-white text-xs font-semibold transition-all shadow-md shadow-blue-500/20 disabled:opacity-30 disabled:cursor-not-allowed disabled:shadow-none"
      >
        Apply
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ArrowRightIcon } from '@heroicons/vue/20/solid';
import type { AnalyticsPreset } from '../../types';

defineProps<{
  modelPreset: AnalyticsPreset | 'custom';
  customFrom: string;
  customTo: string;
}>();

const emit = defineEmits<{
  (e: 'selectPreset', preset: AnalyticsPreset): void;
  (e: 'update:customFrom', value: string): void;
  (e: 'update:customTo', value: string): void;
  (e: 'applyCustom'): void;
}>();

const presets: { value: AnalyticsPreset; label: string }[] = [
  { value: '7d', label: '7 days' },
  { value: '30d', label: '30 days' },
  { value: '90d', label: '90 days' },
  { value: 'all', label: 'All time' },
];

function selectPreset(p: AnalyticsPreset) {
  emit('selectPreset', p);
}
</script>
