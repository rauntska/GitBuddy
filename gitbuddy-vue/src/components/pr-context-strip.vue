<template>
  <div
    role="toolbar"
    aria-label="Pull request context"
    class="flex items-center gap-3 sm:gap-4 px-3 sm:px-5 py-1.5 border-t border-slate-800/60 text-xs"
  >
    <!-- Priority -->
    <button
      type="button"
      @click="emit('jump-to', 'pr-card-priority')"
      :aria-label="`Priority: ${priorityLabel}, jump to priority`"
      :title="`Priority: ${priorityLabel}`"
      class="flex items-center gap-1.5 text-slate-400 hover:text-slate-100 transition-colors"
    >
      <span class="font-mono" :class="getPriorityColor(prDetail.priority ?? 1)">
        {{ getPriorityGlyph(prDetail.priority ?? 1) }}
      </span>
      <span class="hidden sm:inline">{{ priorityLabel }}</span>
    </button>

    <!-- Reviewers -->
    <button
      v-if="prDetail.reviews.length"
      type="button"
      @click="emit('jump-to', 'pr-card-reviewers')"
      aria-label="Jump to reviewers"
      title="Reviewers"
      class="flex items-center hover:opacity-80 transition-opacity"
    >
      <ReviewerAvatars :reviews="prDetail.reviews" size="sm" :max-display="4" />
    </button>

    <!-- CI -->
    <button
      v-if="prDetail.checksStatus || prDetail.checkRuns?.length"
      type="button"
      @click="emit('jump-to', 'pr-card-checks')"
      :aria-label="`Checks: ${checksLabel}, jump to checks`"
      :title="`Checks: ${checksLabel}`"
      class="flex items-center gap-1.5 text-slate-400 hover:text-slate-100 transition-colors"
    >
      <CIBadge
        :status="prDetail.checksStatus"
        :show-count="true"
        :total-count="prDetail.checkRuns?.length || 0"
        :compact="true"
      />
    </button>

    <!-- Unresolved threads -->
    <button
      v-if="unresolvedActiveCount > 0"
      type="button"
      @click="emit('jump-to', 'pr-card-reviewers')"
      :aria-label="`${unresolvedActiveCount} unresolved thread${unresolvedActiveCount === 1 ? '' : 's'}, jump to reviewers`"
      :title="`${unresolvedActiveCount} unresolved`"
      class="flex items-center gap-1.5 text-amber-400/90 hover:text-amber-300 transition-colors"
    >
      <svg class="w-3.5 h-3.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
        <path fill-rule="evenodd" d="M18 10c0 3.866-3.582 7-8 7a8.841 8.841 0 01-4.083-.98L2 17l1.338-3.123C2.493 12.767 2 11.434 2 10c0-3.866 3.582-7 8-7s8 3.134 8 7z" clip-rule="evenodd" />
      </svg>
      <span class="font-mono tabular-nums">{{ unresolvedActiveCount }}</span>
      <span class="hidden sm:inline">unresolved</span>
    </button>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import CIBadge from './CIBadge.vue';
import ReviewerAvatars from './ReviewerAvatars.vue';
import type { PRDetail } from '../types';
import { getPriorityColor, getPriorityGlyph, getPriorityLabel } from '../utils/prHelpers';

const props = defineProps<{
  prDetail: PRDetail;
}>();

const emit = defineEmits<{
  (e: 'jump-to', cardId: string): void;
}>();

const priorityLabel = computed(() => getPriorityLabel(props.prDetail.priority ?? 1));

const checksLabel = computed(() => {
  switch (props.prDetail.checksStatus) {
    case 'SUCCESS': return 'all passed';
    case 'FAILURE': return 'some failed';
    case 'PENDING': return 'running';
    default: return 'no status';
  }
});

const unresolvedActiveCount = computed(
  () => props.prDetail.reviewThreads.filter(t => !t.isResolved && !t.isOutdated).length
);
</script>
