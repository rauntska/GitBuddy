<template>
  <button
    class="sticky top-[8.5rem] z-10 flex flex-wrap items-center justify-between gap-2 px-3 sm:px-4 py-2 bg-gradient-to-r from-slate-800/60 to-slate-800/40 border-b border-slate-700/30 cursor-pointer hover:from-slate-800/80 hover:to-slate-800/60 select-none w-full text-left transition-all duration-200 backdrop-blur-sm"
    type="button"
    @click="$emit('toggle')"
  >
    <div class="flex items-center gap-2 sm:gap-3 min-w-0 flex-1">
      <svg
        :class="['w-4 h-4 text-blue-400 transition-transform flex-shrink-0', { 'rotate-90': expanded }]"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M9 5l7 7-7 7" />
      </svg>

      <span :class="['text-xs font-bold px-2 py-0.5 rounded flex-shrink-0', statusBadgeClass]">
        {{ statusIcon }}
      </span>

      <span class="text-sm font-mono text-slate-100 truncate tracking-wide">{{ path }}</span>
    </div>

    <div class="flex items-center gap-2 sm:gap-4 text-xs flex-shrink-0">
      <div class="flex items-center gap-1 sm:gap-2">
        <span class="text-emerald-400 font-semibold text-xs px-1.5 sm:px-2 py-0.5 rounded bg-emerald-500/10 border border-emerald-500/20">+{{ additions }}</span>
        <span class="text-rose-400 font-semibold text-xs px-1.5 sm:px-2 py-0.5 rounded bg-rose-500/10 border border-rose-500/20">-{{ deletions }}</span>
      </div>
      <div class="flex items-center gap-2 pl-2 sm:pl-3 border-l border-slate-600/50">
        <label class="flex items-center gap-1 sm:gap-2 cursor-pointer group">
          <input
            type="checkbox"
            :checked="viewed"
            @click.stop
            @change="$emit('toggleViewed', ($event.target as HTMLInputElement).checked)"
            class="w-3.5 h-3.5 sm:w-4 sm:h-4 rounded border-slate-600 bg-slate-800 text-emerald-500 focus:ring-emerald-500/50 focus:ring-offset-slate-900 cursor-pointer transition-all"
            title="Mark as viewed"
          />
          <span class="text-xs text-slate-400 group-hover:text-slate-300 transition-colors hidden sm:inline">Viewed</span>
        </label>
      </div>
    </div>
  </button>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  path: string;
  status?: string;
  additions: number;
  deletions: number;
  expanded: boolean;
  viewed: boolean;
}>();

defineEmits<{
  toggle: [];
  toggleViewed: [viewed: boolean];
}>();

const statusIcon = computed(() => {
  const icons: Record<string, string> = {
    added: 'A',
    modified: 'M',
    deleted: 'D',
    renamed: 'R',
  };
  return icons[props.status || 'modified'] || 'M';
});

const statusBadgeClass = computed(() => {
  const classes: Record<string, string> = {
    added: 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30',
    modified: 'bg-amber-500/10 text-amber-400 border border-amber-500/30',
    deleted: 'bg-rose-500/10 text-rose-400 border border-rose-500/30',
    renamed: 'bg-blue-500/10 text-blue-400 border border-blue-500/30',
  };
  return classes[props.status || 'modified'] || 'bg-slate-500/10 text-slate-400 border border-slate-500/30';
});
</script>
