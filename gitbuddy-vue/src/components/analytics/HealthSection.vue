<template>
  <section class="border-t border-slate-800 pt-4">
    <SectionHeader
      title="Health & Bottlenecks"
      subtitle="Current state of open work and where it's stuck"
      :icon="HeartIcon"
      accent="red"
    />

    <div class="grid grid-cols-2 md:grid-cols-4 gap-x-6 gap-y-3 mb-5">
      <KpiCard label="Total Open" :value="data?.totalOpenPRs ?? 0" :icon="DocumentTextIcon" accent="slate" value-color="text-slate-100" />
      <KpiCard label="Stuck in Review" :value="data?.stuckInReviewCount ?? 0" :icon="ExclamationTriangleIcon" accent="amber" value-color="text-amber-400" />
      <KpiCard label="Failing Checks" :value="data?.failingChecksCount ?? 0" :icon="XCircleIcon" accent="red" value-color="text-red-400" />
      <KpiCard label="Unresolved Threads" :value="data?.unresolvedThreadsCount ?? 0" :icon="ChatBubbleLeftIcon" accent="orange" value-color="text-orange-400" />
    </div>

    <div class="flex items-center gap-2 mb-2">
      <ClockIcon class="w-3.5 h-3.5 text-amber-400" />
      <h4 class="text-sm font-semibold uppercase tracking-wider text-slate-300">Stale PRs</h4>
      <span class="text-[11px] text-slate-500">(open, no update for 14+ days)</span>
    </div>

    <div v-if="!data || data.stalePRs.length === 0" class="inline-flex items-center gap-2 py-2 text-sm text-slate-200/60">
      <CheckCircleIcon class="w-3.5 h-3.5 text-emerald-400" />
      <span>No stale PRs. Nothing piling up.</span>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-1">
      <router-link
        v-for="pr in data.stalePRs"
        :key="pr.id"
        :to="{ name: 'pr-detail', params: { id: pr.id } }"
        class="group flex items-center gap-3 border border-slate-800 rounded px-2 py-1.5 hover:bg-slate-800/40 hover:border-slate-700 transition-all duration-150 ease-out"
      >
        <div class="shrink-0 font-mono tabular-nums text-amber-400 text-sm w-10 text-center">
          {{ pr.daysStale }}<span class="text-[9px] uppercase tracking-wider text-slate-500 ml-0.5">d</span>
        </div>
        <div class="flex-1 min-w-0">
          <div class="text-sm text-slate-200 truncate group-hover:text-white transition-colors">{{ pr.title }}</div>
          <div class="text-[11px] text-slate-500 mt-0.5 font-mono">Updated {{ formatDate(pr.updatedAt) }}</div>
        </div>
        <ChevronRightIcon class="w-3.5 h-3.5 text-slate-600 group-hover:text-slate-300 group-hover:translate-x-0.5 transition-all shrink-0" />
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
