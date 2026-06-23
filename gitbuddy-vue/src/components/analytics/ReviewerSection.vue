<template>
  <section class="border-t border-slate-800 pt-4">
    <SectionHeader
      title="User Stats"
      subtitle="PRs authored and reviewing activity per user"
      :icon="UsersIcon"
      accent="emerald"
    />

    <div v-if="!data || data.reviewers.length === 0" class="text-center text-slate-200/60 py-10 text-sm">
      No user activity in this window.
    </div>

    <div v-else class="overflow-x-auto">
      <table class="w-full min-w-[760px]">
        <thead>
          <tr class="border-b border-slate-800">
            <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">User</th>
            <th class="px-3 py-2 text-right text-[11px] font-normal uppercase tracking-wider text-slate-500">PRs Authored</th>
            <th class="px-3 py-2 text-right text-[11px] font-normal uppercase tracking-wider text-slate-500">Reviews</th>
            <th class="px-3 py-2 text-right text-[11px] font-normal uppercase tracking-wider text-slate-500">Approved</th>
            <th class="px-3 py-2 text-right text-[11px] font-normal uppercase tracking-wider text-slate-500">Changes</th>
            <th class="px-3 py-2 text-right text-[11px] font-normal uppercase tracking-wider text-slate-500 hidden md:table-cell">Comments</th>
            <th class="px-3 py-2 text-right text-[11px] font-normal uppercase tracking-wider text-slate-500 hidden lg:table-cell">Median Latency</th>
            <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500 w-32 hidden sm:table-cell">Approval Rate</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="r in data.reviewers"
            :key="r.username"
            class="border-b border-slate-800 last:border-0 hover:bg-slate-800/40 hover:border-slate-700 transition-all duration-150 ease-out"
          >
            <td class="px-3 py-2 max-w-[200px]">
              <div class="flex items-center gap-2 min-w-0">
                <img v-if="r.avatarUrl" :src="r.avatarUrl" :alt="r.username" class="w-6 h-6 rounded-full shrink-0" />
                <div v-else class="w-6 h-6 rounded-full bg-slate-800 border border-slate-700 flex items-center justify-center text-slate-400 text-[11px] shrink-0">
                  {{ r.username.charAt(0).toUpperCase() }}
                </div>
                <span class="text-sm text-slate-200 truncate font-mono">{{ r.username }}</span>
              </div>
            </td>
            <td class="px-3 py-2 text-right">
              <span class="text-sm text-blue-400 tabular-nums font-mono">{{ r.totalPRsAuthored }}</span>
            </td>
            <td class="px-3 py-2 text-right">
              <span class="text-sm text-slate-200 tabular-nums font-mono">{{ r.totalReviews }}</span>
            </td>
            <td class="px-3 py-2 text-right">
              <span class="text-sm text-emerald-400 tabular-nums font-mono">{{ r.approvals }}</span>
            </td>
            <td class="px-3 py-2 text-right">
              <span class="text-sm text-amber-400 tabular-nums font-mono">{{ r.changesRequested }}</span>
            </td>
            <td class="px-3 py-2 text-right text-sm text-slate-200/60 tabular-nums font-mono hidden md:table-cell">{{ r.comments }}</td>
            <td class="px-3 py-2 text-right text-sm text-slate-200/60 tabular-nums font-mono hidden lg:table-cell">{{ formatHours(r.medianReviewLatencyHours) }}</td>
            <td class="px-3 py-2 hidden sm:table-cell">
              <div class="flex items-center gap-2">
                <div class="flex-1 h-1 rounded-full bg-slate-800 overflow-hidden min-w-[60px]">
                  <div
                    class="h-full rounded-full bg-emerald-400"
                    :style="{ width: `${approvalRate(r)}%` }"
                  ></div>
                </div>
                <span class="text-[11px] text-slate-400 tabular-nums font-mono w-9 text-right">{{ approvalRate(r) }}%</span>
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
