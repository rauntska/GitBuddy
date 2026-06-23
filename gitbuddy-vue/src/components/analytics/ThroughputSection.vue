<template>
  <section class="border-t border-slate-800 pt-4">
    <SectionHeader
      title="Throughput & Cycle Time"
      subtitle="PR flow and median velocity across the selected window"
      :icon="ArrowTrendingUpIcon"
      accent="blue"
    />

    <div class="grid grid-cols-2 md:grid-cols-4 gap-x-6 gap-y-3 mb-5">
      <KpiCard label="PRs Opened" :value="data?.totalOpened ?? 0" :icon="DocumentPlusIcon" accent="blue" value-color="text-blue-400" />
      <KpiCard label="PRs Merged" :value="data?.totalMerged ?? 0" :icon="CheckCircleIcon" accent="emerald" value-color="text-emerald-400" />
      <KpiCard label="Median Time-to-Merge" :value="formatHours(data?.medianTimeToMergeHours)" :icon="ClockIcon" accent="purple" value-color="text-violet-400" />
      <KpiCard label="Median First Review" :value="formatHours(data?.medianTimeToFirstReviewHours)" :icon="ChatBubbleLeftRightIcon" accent="amber" value-color="text-amber-400" />
    </div>

    <div class="border-t border-slate-800 pt-3">
      <div class="flex items-center gap-4 mb-3 text-xs">
        <div class="flex items-center gap-1.5">
          <span class="h-2 w-2 rounded-full bg-blue-400"></span>
          <span class="text-slate-300">Opened</span>
        </div>
        <div class="flex items-center gap-1.5">
          <span class="h-2 w-2 rounded-full bg-emerald-400"></span>
          <span class="text-slate-300">Merged</span>
        </div>
      </div>
      <div v-if="!hasChartData" class="text-center text-slate-200/60 py-10 text-sm">
        No PRs in this window.
      </div>
      <div v-else class="h-72">
        <Line :data="chartData" :options="chartOptions" />
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Line } from 'vue-chartjs';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
} from 'chart.js';
import {
  ArrowTrendingUpIcon,
  DocumentPlusIcon,
  CheckCircleIcon,
  ClockIcon,
  ChatBubbleLeftRightIcon,
} from '@heroicons/vue/24/outline';
import type { ThroughputAnalytics } from '../../types';
import KpiCard from './KpiCard.vue';
import SectionHeader from './SectionHeader.vue';

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend, Filler);

const props = defineProps<{ data: ThroughputAnalytics | null }>();

const hasChartData = computed(() => {
  const d = props.data;
  if (!d) return false;
  return d.openedDaily.length > 0 || d.mergedDaily.length > 0;
});

function makeGradient(ctx: CanvasRenderingContext2D, area: { top: number; bottom: number }, color: string) {
  const g = ctx.createLinearGradient(0, area.top, 0, area.bottom);
  g.addColorStop(0, color);
  g.addColorStop(1, 'rgba(15, 23, 42, 0)');
  return g;
}

const chartData = computed(() => {
  const d = props.data;
  if (!d) return { labels: [], datasets: [] };
  const dateSet = new Set<string>();
  d.openedDaily.forEach((x) => dateSet.add(x.date));
  d.mergedDaily.forEach((x) => dateSet.add(x.date));
  const labels = Array.from(dateSet).sort();
  const openedMap = new Map(d.openedDaily.map((x) => [x.date, x.count]));
  const mergedMap = new Map(d.mergedDaily.map((x) => [x.date, x.count]));
  return {
    labels,
    datasets: [
      {
        label: 'Opened',
        data: labels.map((l) => openedMap.get(l) ?? 0),
        borderColor: '#60a5fa',
        backgroundColor: (ctx: { chart: { ctx: CanvasRenderingContext2D; chartArea?: { top: number; bottom: number } } }) => {
          const { ctx: canvasCtx, chartArea } = ctx.chart;
          if (!chartArea) return 'rgba(96, 165, 250, 0.2)';
          return makeGradient(canvasCtx, chartArea, 'rgba(96, 165, 250, 0.4)');
        },
        fill: true,
        tension: 0.35,
        borderWidth: 2,
        pointRadius: 0,
        pointHoverRadius: 5,
        pointHoverBackgroundColor: '#60a5fa',
        pointHoverBorderColor: '#fff',
      },
      {
        label: 'Merged',
        data: labels.map((l) => mergedMap.get(l) ?? 0),
        borderColor: '#34d399',
        backgroundColor: (ctx: { chart: { ctx: CanvasRenderingContext2D; chartArea?: { top: number; bottom: number } } }) => {
          const { ctx: canvasCtx, chartArea } = ctx.chart;
          if (!chartArea) return 'rgba(52, 211, 153, 0.2)';
          return makeGradient(canvasCtx, chartArea, 'rgba(52, 211, 153, 0.4)');
        },
        fill: true,
        tension: 0.35,
        borderWidth: 2,
        pointRadius: 0,
        pointHoverRadius: 5,
        pointHoverBackgroundColor: '#34d399',
        pointHoverBorderColor: '#fff',
      },
    ],
  };
});

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index' as const, intersect: false },
  plugins: {
    legend: { display: false },
    tooltip: {
      backgroundColor: 'rgba(15, 23, 42, 0.95)',
      titleColor: '#e2e8f0',
      bodyColor: '#cbd5e1',
      borderColor: 'rgba(71, 85, 105, 0.4)',
      borderWidth: 1,
      padding: 10,
      cornerRadius: 8,
      boxPadding: 4,
    },
  },
  scales: {
    x: {
      ticks: { color: '#64748b', maxRotation: 0, autoSkipPadding: 16 },
      grid: { color: 'rgba(148, 163, 184, 0.06)' },
      border: { display: false },
    },
    y: {
      ticks: { color: '#64748b', precision: 0 },
      grid: { color: 'rgba(148, 163, 184, 0.06)' },
      border: { display: false },
      beginAtZero: true,
    },
  },
};

function formatHours(h: number | null | undefined): string {
  if (h === null || h === undefined) return '—';
  if (h < 1) return `${Math.round(h * 60)}m`;
  if (h < 24) return `${h.toFixed(1)}h`;
  return `${(h / 24).toFixed(1)}d`;
}
</script>
