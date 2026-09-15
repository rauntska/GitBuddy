<template>
  <div class="flex-1 min-w-0">
    <!-- Branch Info -->
    <div class="p-4 border border-slate-800 rounded mb-4">
      <div class="text-xs text-slate-400 mb-2 uppercase tracking-wider">Branches</div>
      <div class="flex flex-wrap items-center gap-2 text-sm">
        <span class="text-emerald-400 font-mono truncate max-w-[200px] sm:max-w-none">{{ prDetail.sourceBranch }}</span>
        <svg class="w-4 h-4 text-slate-500 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
        <span class="text-blue-400 font-mono truncate max-w-[200px] sm:max-w-none">{{ prDetail.targetBranch }}</span>
      </div>
    </div>

    <!-- Description -->
    <div class="p-4 border border-slate-800 rounded">
      <div class="flex items-center justify-between" :class="descriptionOpen ? 'mb-3' : ''">
        <button
          type="button"
          @click="descriptionOpen = !descriptionOpen"
          class="flex items-center gap-2 text-xs font-semibold text-slate-300 uppercase tracking-wider hover:text-slate-100 transition-colors"
          :aria-expanded="descriptionOpen"
        >
          <svg
            class="w-3 h-3 transition-transform duration-150 flex-shrink-0"
            :class="descriptionOpen ? 'rotate-90' : ''"
            fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M9 5l7 7-7 7" />
          </svg>
          Description
          <span v-if="!descriptionOpen" class="text-slate-500 font-normal normal-case tracking-normal">
            {{ descriptionSummary }}
          </span>
        </button>
        <div v-if="prDetail.isMerged" class="text-xs text-slate-500 flex items-center gap-1 font-mono">
          <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
          </svg>
          Merged - read only
        </div>
      </div>
      <EditableDescription
        v-if="descriptionOpen"
        :content="prDetail.description || ''"
        :pr-id="prDetail.id"
        @save="(body: string) => emit('save-description', body)"
      />
    </div>

    <!-- General Comments -->
    <GeneralComments
      :comments="prDetail.allComments"
      :current-username="currentUsername"
      :is-merged="prDetail.isMerged"
      :pr-id="prDetail.id"
      @add-comment="(body: string) => emit('add-comment', body)"
      @update-comment="(gitHubId: string, body: string) => emit('update-comment', gitHubId, body)"
      @delete-comment="(gitHubId: string) => emit('delete-comment', gitHubId)"
      class="mt-4"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import EditableDescription from './EditableDescription.vue';
import GeneralComments from './GeneralComments.vue';
import type { PRDetail } from '../types';

const props = defineProps<{
  prDetail: PRDetail;
  currentUsername: string;
  /** Initial state only — once the user toggles, their choice wins. */
  collapseDescription: boolean;
}>();

const emit = defineEmits<{
  (e: 'save-description', body: string): void;
  (e: 'add-comment', body: string): void;
  (e: 'update-comment', gitHubId: string, body: string): void;
  (e: 'delete-comment', gitHubId: string): void;
}>();

const descriptionOpen = ref(!props.collapseDescription);

const descriptionSummary = computed(() => {
  const body = props.prDetail.description?.trim();
  if (!body) return '· empty';
  const lines = body.split('\n').length;
  return `· ${lines} line${lines === 1 ? '' : 's'}`;
});
</script>
