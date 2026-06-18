<template>
  <section class="relative overflow-hidden rounded-2xl border border-slate-700/60 bg-slate-900/40 p-5 backdrop-blur-sm">
    <SectionHeader
      title="Health & Bottlenecks"
      subtitle="Current state of open work and where it's stuck"
      :icon="HeartIcon"
      accent="red"
    />

    <div class="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
      <KpiCard label="Total Open" :value="data?.totalOpenPRs ?? 0" :icon="DocumentTextIcon" accent="slate" value-color="text-slate-100" />
      <KpiCard label="Stuck in Review" :value="data?.stuckInReviewCount ?? 0" :icon="ExclamationTriangleIcon" accent="amber" value-color="text-amber-300" />
      <KpiCard label="Failing Checks" :value="data?.failingChecksCount ?? 0" :icon="XCircleIcon" accent="red" value-color="text-red-300" />
      <KpiCard label="Unresolved Threads" :value="data?.unresolvedThreadsCount ?? 0" :icon="ChatBubbleLeftIcon" accent="orange" value-color="text-orange-300" />
    </div>

    <div class="flex items-center justify-between mb-3">
      <h4 class="text-sm font-semibold text-slate-300 flex items-center gap-2">
        <ClockIcon class="w-4 h-4 text-amber-400" />
        Stale PRs
        <span class="text-xs font-normal text-slate-500">(open, no update for 14+ days)</span>
      </h4>
    </div>

    <div v-if="!data || data.stalePRs.length === 0" class="text-center py-8 text-sm bg-slate-950/30 border border-dashed border-slate-700/60 rounded-xl">
      <CheckCircleIcon class="w-8 h-8 text-emerald-400 mx-auto mb-2" />
      <span class="text-slate-400">No stale PRs. Nothing piling up.</span>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-2">
      <router-link
        v-for="pr in data.stalePRs"
        :key="pr.id"
        :to="{ name: 'pr-detail', params: { id: pr.id } }"
        class="group flex items-center gap-3 rounded-lg border border-slate-700/60 bg-slate-800/40 hover:bg-slate-800/80 hover:border-amber-500/40 px-3 py-2.5 transition-all"
      >
        <div class="shrink-0 rounded-md bg-amber-500/15 ring-1 ring-inset ring-amber-500/30 px-2 py-1 text-center min-w-[3rem]">
          <div class="text-sm font-bold text-amber-300 tabular-nums">{{ pr.daysStale }}</div>
          <div class="text-[9px] uppercase tracking-wider text-amber-500/70">days</div>
        </div>
        <div class="flex-1 min-w-0">
          <div class="text-sm text-slate-200 truncate group-hover:text-white transition-colors">{{ pr.title }}</div>
          <div class="text-xs text-slate-500 mt-0.5">Updated {{ formatDate(pr.updatedAt) }}</div>
        </div>
        <ChevronRightIcon class="w-4 h-4 text-slate-600 group-hover:text-amber-400 group-hover:translate-x-0.5 transition-all shrink-0" />
      </router-link>
    </div>
  </section>
</template>

<script setup lang="ts">
import {
  HeartIcon,
  DocumentTextIcon,
  ExclamationTriangleIcon,
  XCircleIcon,
  ChatBubbleLeftIcon,
  ClockIcon,
  CheckCircleIcon,
  ChevronRightIcon,
} from '@heroicons/vue/24/outline';
import type { HealthAnalytics } from '../../types';
import KpiCard from './KpiCard.vue';
import SectionHeader from './SectionHeader.vue';

defineProps<{ data: HealthAnalytics | null }>();

function formatDate(s: string): string {
  return new Date(s).toLocaleDateString();
}
</script>
