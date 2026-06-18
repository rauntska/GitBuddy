<template>
  <div class="space-y-4">
    <DateRangeSelector
      :model-preset="preset"
      :custom-from="customFrom"
      :custom-to="customTo"
      @select-preset="setPreset"
      @update:custom-from="(v) => (customFrom = v)"
      @update:custom-to="(v) => (customTo = v)"
      @apply-custom="() => { if (customFrom && customTo) setCustomRange(customFrom, customTo) }"
    />

    <div v-if="loading && !throughput" class="flex flex-col items-center justify-center py-16 text-slate-400">
      <ArrowPathIcon class="w-8 h-8 animate-spin text-blue-400 mb-3" />
      <span class="text-sm">Loading analytics…</span>
    </div>
    <div v-else-if="error" class="text-center text-red-400 py-12 rounded-xl border border-red-500/20 bg-red-500/5">
      {{ error }}
    </div>

    <transition
      enter-active-class="transition duration-300 ease-out"
      enter-from-class="opacity-0 translate-y-2"
      enter-to-class="opacity-100 translate-y-0"
    >
      <div v-if="!loading || throughput" class="space-y-4">
        <ThroughputSection :data="throughput" />
        <ReviewerSection :data="reviewers" />
        <HealthSection :data="health" />
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { ArrowPathIcon } from '@heroicons/vue/24/outline';
import { useAnalytics } from '../../composables/useAnalytics';
import DateRangeSelector from './DateRangeSelector.vue';
import ThroughputSection from './ThroughputSection.vue';
import ReviewerSection from './ReviewerSection.vue';
import HealthSection from './HealthSection.vue';

const {
  preset,
  customFrom,
  customTo,
  throughput,
  reviewers,
  health,
  loading,
  error,
  setPreset,
  setCustomRange,
} = useAnalytics();
</script>
