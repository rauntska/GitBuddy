<template>
  <div>
    <div class="text-xs text-slate-400 mb-3 uppercase tracking-wider">CI/CD Checks</div>

    <div v-if="!checksStatus && !checkRuns?.length" class="text-sm text-slate-500">
      No checks reported for this PR.
    </div>

    <div v-else class="space-y-2">
      <div v-if="checksStatus" class="flex items-center gap-2 p-2 rounded bg-slate-800/40">
        <CIBadge :status="checksStatus" :show-count="true" :total-count="checkRuns?.length || 0" />
        <span class="text-sm text-slate-200">{{ checksStatusLabel }}</span>
      </div>

      <div v-if="checkRuns && checkRuns.length > 0" class="mt-3 space-y-1 max-h-48 overflow-y-auto">
        <div
          v-for="check in checkRuns"
          :key="check.id"
          class="flex items-center gap-2 p-2 rounded hover:bg-slate-800/60 transition-colors duration-150"
        >
          <CIBadge
            :status="getCheckStatusFromCheckRun(check)"
            :show-count="false"
            :compact="true"
          />
          <span class="text-xs text-slate-200 truncate flex-1 font-mono">{{ check.name }}</span>
          <a
            v-if="check.url"
            :href="check.url"
            target="_blank"
            rel="noopener noreferrer"
            class="text-slate-400 hover:text-slate-200 transition-colors duration-150"
            title="View on GitHub"
          >
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
            </svg>
          </a>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import CIBadge from './CIBadge.vue';
import type { CheckRun, PRDetail } from '../types';

const props = defineProps<{
  checksStatus?: PRDetail['checksStatus'];
  checkRuns?: CheckRun[];
}>();

const checksStatusLabel = computed(() => {
  if (!props.checksStatus) return 'No checks';
  switch (props.checksStatus) {
    case 'SUCCESS':
      return 'All checks passed';
    case 'FAILURE':
      return 'Some checks failed';
    case 'PENDING':
      return 'Checks pending';
    default:
      return 'Checks running';
  }
});

const getCheckStatusFromCheckRun = (check: CheckRun): 'SUCCESS' | 'FAILURE' | 'PENDING' | 'NEUTRAL' => {
  if (check.status === 'completed') {
    if (check.conclusion === 'success') return 'SUCCESS';
    if (check.conclusion === 'failure') return 'FAILURE';
    return 'NEUTRAL';
  }
  if (check.status === 'queued' || check.status === 'in_progress') return 'PENDING';
  return 'NEUTRAL';
};
</script>
