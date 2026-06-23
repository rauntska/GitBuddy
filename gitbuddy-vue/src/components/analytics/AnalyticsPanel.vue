<template>
  <div class="space-y-4">
    <div class="border-b border-slate-800 pb-3">
      <div class="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <div class="flex items-center gap-1.5 text-[11px] uppercase tracking-wider text-slate-500">
            <ChartBarIcon class="w-3.5 h-3.5" />
            <span>Insights</span>
          </div>
          <h2 class="mt-1 text-base font-semibold text-slate-100">PR Analytics</h2>
          <p class="mt-1 text-sm text-slate-200/60 max-w-xl">
            Throughput, reviewer participation, and bottlenecks across your organization.
          </p>
        </div>
        <button
          @click="refresh"
          :disabled="loading"
          class="inline-flex items-center gap-2 border border-slate-800 hover:bg-slate-800 px-2.5 py-1.5 text-sm text-slate-300 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <ArrowPathIcon :class="['w-3.5 h-3.5', loading && 'animate-spin']" />
          Refresh
        </button>
      </div>
    </div>

    <AnalyticsTab />
  </div>
</template>

<script setup lang="ts">
import { ChartBarIcon, ArrowPathIcon } from '@heroicons/vue/24/outline';
import AnalyticsTab from './AnalyticsTab.vue';
import { useAnalytics } from '../../composables/useAnalytics';

const { refresh, loading } = useAnalytics();
</script>
