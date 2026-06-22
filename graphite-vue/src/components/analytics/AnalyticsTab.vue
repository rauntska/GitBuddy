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

    <UserSelector
      :available="availableAuthors"
      :selected="selectedAuthors"
      :loading="loading"
      @update:selected="setSelectedAuthors"
    />

    <div v-if="loading && !throughput" class="flex flex-col items-center justify-center py-12 text-slate-200/60">
      <ArrowPathIcon class="w-5 h-5 animate-spin text-slate-400 mb-2" />
      <span class="text-sm">Loading analytics…</span>
    </div>
    <div v-else-if="error" class="inline-flex items-center gap-2 px-3 py-2 rounded border border-red-900/40 bg-red-950/20 text-red-400 text-sm">
      <span class="font-mono">✕</span>
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
import UserSelector from './UserSelector.vue';
import ThroughputSection from './ThroughputSection.vue';
import ReviewerSection from './ReviewerSection.vue';
import HealthSection from './HealthSection.vue';

const {
  preset,
  customFrom,
  customTo,
  selectedAuthors,
  availableAuthors,
  throughput,
  reviewers,
  health,
  loading,
  error,
  setPreset,
  setCustomRange,
  setSelectedAuthors,
} = useAnalytics();
</script>
