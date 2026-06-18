<template>
  <div class="space-y-6">
    <div class="relative overflow-hidden rounded-2xl bg-gradient-to-br from-slate-800/80 via-slate-900/60 to-slate-900/40 border border-slate-700/60 p-6 shadow-xl shadow-slate-950/40">
      <div class="absolute -top-24 -right-24 h-64 w-64 rounded-full bg-blue-500/10 blur-3xl pointer-events-none"></div>
      <div class="absolute -bottom-32 -left-16 h-72 w-72 rounded-full bg-purple-500/10 blur-3xl pointer-events-none"></div>
      <div class="relative flex items-start justify-between gap-6 flex-wrap">
        <div>
          <div class="flex items-center gap-2 text-xs font-medium uppercase tracking-widest text-blue-300/80">
            <ChartBarIcon class="w-4 h-4" />
            <span>Insights</span>
          </div>
          <h2 class="mt-2 text-3xl font-bold bg-gradient-to-r from-white via-blue-100 to-purple-200 bg-clip-text text-transparent">
            PR Analytics
          </h2>
          <p class="mt-1 text-sm text-slate-400 max-w-xl">
            Throughput, reviewer participation, and bottlenecks across your organization.
          </p>
        </div>
        <button
          @click="refresh"
          :disabled="loading"
          class="inline-flex items-center gap-2 rounded-lg bg-slate-800/80 hover:bg-slate-700/80 border border-slate-600/60 px-3 py-2 text-sm font-medium text-slate-200 transition-all hover:shadow-lg hover:shadow-blue-500/10 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <ArrowPathIcon :class="['w-4 h-4', loading && 'animate-spin']" />
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
