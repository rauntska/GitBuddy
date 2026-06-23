<template>
  <div class="flex flex-wrap items-center gap-3 border border-slate-800 rounded p-2">
    <div class="inline-flex gap-1 bg-slate-900/60 border border-slate-800 rounded p-0.5 w-full sm:w-auto">
      <button
        v-for="p in presets"
        :key="p.value"
        @click="selectPreset(p.value)"
        :class="[
          'flex-1 sm:flex-initial px-2.5 py-1 rounded text-xs transition-all duration-150 ease-out border font-mono tabular-nums',
          modelPreset === p.value
            ? 'bg-slate-800/60 border-slate-700 text-slate-200'
            : 'border-transparent text-slate-300 hover:bg-slate-800/40 hover:border-slate-700'
        ]"
      >
        {{ p.label }}
      </button>
    </div>

    <div class="h-px w-full sm:h-4 sm:w-px bg-slate-800"></div>

    <div class="flex flex-wrap items-center gap-2 text-sm w-full sm:w-auto">
      <span class="text-[11px] uppercase tracking-wider text-slate-500">From</span>
      <input
        :value="customFrom"
        @input="emit('update:customFrom', ($event.target as HTMLInputElement).value)"
        type="date"
        class="flex-1 sm:flex-initial min-w-[120px] px-2 py-1 bg-slate-900/60 border border-slate-700 rounded text-slate-200 text-sm focus:outline-none focus:border-slate-600 transition-colors font-mono tabular-nums"
      />
      <ArrowRightIcon class="w-3 h-3 text-slate-600 hidden sm:block" />
      <span class="text-[11px] uppercase tracking-wider text-slate-500 sm:ml-1">To</span>
      <input
        :value="customTo"
        @input="emit('update:customTo', ($event.target as HTMLInputElement).value)"
        type="date"
        class="flex-1 sm:flex-initial min-w-[120px] px-2 py-1 bg-slate-900/60 border border-slate-700 rounded text-slate-200 text-sm focus:outline-none focus:border-slate-600 transition-colors font-mono tabular-nums"
      />
      <button
        @click="emit('applyCustom')"
        :disabled="!customFrom || !customTo"
        class="px-2.5 py-1 rounded bg-slate-200 hover:bg-white text-slate-900 text-xs transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
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
