<template>
  <section class="relative overflow-hidden rounded-2xl border border-slate-700/60 bg-slate-900/40 p-5 backdrop-blur-sm">
    <SectionHeader
      title="User Stats"
      subtitle="PRs authored and reviewing activity per user"
      :icon="UsersIcon"
      accent="emerald"
    />

    <div v-if="!data || data.reviewers.length === 0" class="text-center text-slate-500 py-10 text-sm bg-slate-950/30 border border-slate-800/60 rounded-xl">
      No user activity in this window.
    </div>

    <div v-else class="overflow-x-auto rounded-xl border border-slate-800/80 bg-slate-950/30">
      <table class="w-full min-w-[760px]">
        <thead>
          <tr class="border-b border-slate-800 bg-slate-900/60">
            <th class="px-4 py-3 text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500">User</th>
            <th class="px-4 py-3 text-right text-[11px] font-semibold uppercase tracking-wider text-sky-500/80">PRs Authored</th>
            <th class="px-4 py-3 text-right text-[11px] font-semibold uppercase tracking-wider text-slate-500">Reviews</th>
            <th class="px-4 py-3 text-right text-[11px] font-semibold uppercase tracking-wider text-emerald-500/80">Approved</th>
            <th class="px-4 py-3 text-right text-[11px] font-semibold uppercase tracking-wider text-amber-500/80">Changes</th>
            <th class="px-4 py-3 text-right text-[11px] font-semibold uppercase tracking-wider text-slate-500 hidden md:table-cell">Comments</th>
            <th class="px-4 py-3 text-right text-[11px] font-semibold uppercase tracking-wider text-slate-500 hidden lg:table-cell">Median Latency</th>
            <th class="px-4 py-3 text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 w-32 hidden sm:table-cell">Approval Rate</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="r in data.reviewers"
            :key="r.username"
            class="group border-b border-slate-800/60 last:border-0 hover:bg-slate-800/40 transition-colors"
          >
            <td class="px-4 py-3 max-w-[200px]">
              <div class="flex items-center gap-3 min-w-0">
                <img v-if="r.avatarUrl" :src="r.avatarUrl" :alt="r.username" class="w-8 h-8 rounded-full ring-2 ring-slate-700 group-hover:ring-emerald-500/40 transition-all shrink-0" />
                <div v-else class="w-8 h-8 rounded-full bg-slate-700 flex items-center justify-center text-slate-300 text-xs font-semibold shrink-0">
                  {{ r.username.charAt(0).toUpperCase() }}
                </div>
                <span class="text-sm font-medium text-slate-200 truncate">{{ r.username }}</span>
              </div>
            </td>
            <td class="px-4 py-3 text-right">
              <span class="text-sm font-semibold text-sky-300 tabular-nums">{{ r.totalPRsAuthored }}</span>
            </td>
            <td class="px-4 py-3 text-right">
              <span class="text-sm font-semibold text-slate-100 tabular-nums">{{ r.totalReviews }}</span>
            </td>
            <td class="px-4 py-3 text-right">
              <span class="inline-flex items-center justify-end gap-1.5 text-sm font-semibold text-emerald-300 tabular-nums">
                {{ r.approvals }}
              </span>
            </td>
            <td class="px-4 py-3 text-right">
              <span class="text-sm font-semibold text-amber-300 tabular-nums">{{ r.changesRequested }}</span>
            </td>
            <td class="px-4 py-3 text-right text-sm text-slate-300 tabular-nums hidden md:table-cell">{{ r.comments }}</td>
            <td class="px-4 py-3 text-right text-sm text-slate-300 tabular-nums hidden lg:table-cell">{{ formatHours(r.medianReviewLatencyHours) }}</td>
            <td class="px-4 py-3 hidden sm:table-cell">
              <div class="flex items-center gap-2">
                <div class="flex-1 h-1.5 rounded-full bg-slate-800 overflow-hidden min-w-[60px]">
                  <div
                    class="h-full rounded-full bg-gradient-to-r from-emerald-500 to-emerald-400 transition-all"
                    :style="{ width: `${approvalRate(r)}%` }"
                  ></div>
                </div>
                <span class="text-xs text-slate-400 tabular-nums w-9 text-right">{{ approvalRate(r) }}%</span>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>

<script setup lang="ts">
import { UsersIcon } from '@heroicons/vue/24/outline';
import type { ReviewerAnalytics, ReviewerStat } from '../../types';
import SectionHeader from './SectionHeader.vue';

defineProps<{ data: ReviewerAnalytics | null }>();

function formatHours(h: number | null | undefined): string {
  if (h === null || h === undefined) return '—';
  if (h < 1) return `${Math.round(h * 60)}m`;
  if (h < 24) return `${h.toFixed(1)}h`;
  return `${(h / 24).toFixed(1)}d`;
}

function approvalRate(r: ReviewerStat): number {
  const decided = r.approvals + r.changesRequested;
  if (decided === 0) return 0;
  return Math.round((r.approvals / decided) * 100);
}
</script>
