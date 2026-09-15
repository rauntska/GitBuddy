<template>
  <div class="lg:w-96 flex-shrink-0 space-y-4">
    <!-- Priority -->
    <div
      id="pr-card-priority"
      :class="['p-4 border rounded transition-shadow duration-300', cardClass('pr-card-priority')]"
    >
      <div class="flex items-center justify-between gap-2">
        <div class="text-sm font-semibold text-slate-300 uppercase tracking-wider">Priority</div>
        <span
          v-if="prDetail.priorityOverridden"
          class="text-[10px] uppercase tracking-wider text-amber-400/80"
          title="Manually overridden (auto-derivation disabled)"
        >override</span>
        <span
          v-else
          class="text-[10px] uppercase tracking-wider text-slate-500"
          title="Priority auto-derived from PR signals"
        >auto</span>
      </div>
      <div class="flex items-center gap-2 mt-2">
        <span
          class="text-xs font-mono"
          :class="getPriorityColor(prDetail.priority ?? 1)"
        >{{ getPriorityGlyph(prDetail.priority ?? 1) }}</span>
        <select
          :value="prDetail.priorityOverridden ? prDetail.priority : 'auto'"
          :disabled="settingPriority"
          @change="emit('priority-change', ($event.target as HTMLSelectElement).value)"
          class="flex-1 bg-slate-900/60 border border-slate-700 rounded px-2 py-1.5 text-sm text-slate-200 focus:outline-none focus:border-slate-600 transition-colors disabled:opacity-50"
        >
          <option value="auto">Auto (derived)</option>
          <option :value="PRIORITY_LOW">Low</option>
          <option :value="PRIORITY_NORMAL">Normal</option>
          <option :value="PRIORITY_HIGH">High</option>
          <option :value="PRIORITY_URGENT">Urgent</option>
        </select>
      </div>
    </div>

    <!-- Reviewer Manager -->
    <div
      id="pr-card-reviewers"
      :class="['p-4 border rounded transition-shadow duration-300', cardClass('pr-card-reviewers')]"
    >
      <ReviewerManager
        :pull-request-id="prDetail.id"
        @error="(message: string) => emit('reviewer-error', message)"
      />
    </div>

    <!-- CI/CD Checks -->
    <div
      id="pr-card-checks"
      :class="['p-4 border rounded transition-shadow duration-300', cardClass('pr-card-checks')]"
    >
      <pr-checks
        :checks-status="prDetail.checksStatus"
        :check-runs="prDetail.checkRuns"
      />
    </div>

    <!-- Review Timeline -->
    <div class="p-4 border border-slate-800 rounded">
      <ReviewTimeline
        :pull-request-id="prDetail.id"
        @error="(message: string) => emit('timeline-error', message)"
      />
    </div>

    <!-- Watched by useCondensedRail: once this passes under the sticky chrome, the header
         swaps in the condensed strip. -->
    <div ref="sentinelRef" class="h-px" aria-hidden="true" />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import ReviewerManager from './ReviewerManager.vue';
import ReviewTimeline from './ReviewTimeline.vue';
import PrChecks from './pr-checks.vue';
import type { PRDetail } from '../types';
import {
  getPriorityColor,
  getPriorityGlyph,
  PRIORITY_LOW,
  PRIORITY_NORMAL,
  PRIORITY_HIGH,
  PRIORITY_URGENT,
} from '../utils/prHelpers';

defineProps<{
  prDetail: PRDetail;
  settingPriority: boolean;
}>();

const emit = defineEmits<{
  (e: 'priority-change', value: string): void;
  (e: 'reviewer-error', message: string): void;
  (e: 'timeline-error', message: string): void;
}>();

const sentinelRef = ref<HTMLElement | null>(null);
const highlighted = ref<string | null>(null);
let highlightTimer: ReturnType<typeof setTimeout> | null = null;

const cardClass = (cardId: string) =>
  highlighted.value === cardId
    ? 'border-slate-600 ring-2 ring-slate-500'
    : 'border-slate-800 ring-0';

/** Scroll a rail card into view and flash it — what a condensed-strip chip click does. */
const highlight = (cardId: string) => {
  document.getElementById(cardId)?.scrollIntoView({ behavior: 'smooth', block: 'center' });

  highlighted.value = cardId;
  if (highlightTimer) clearTimeout(highlightTimer);
  highlightTimer = setTimeout(() => {
    highlighted.value = null;
    highlightTimer = null;
  }, 1200);
};

defineExpose({ sentinelRef, highlight });
</script>
