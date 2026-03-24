<template>
  <tr
    :ref="(el: any) => setRef(el)"
    :class="[
      'hover:bg-slate-800/40 group transition-colors duration-150',
      rowClasses
    ]"
  >
    <td class="px-3 py-1 text-slate-600 text-right select-none border-r border-slate-800 bg-slate-950/50">
      {{ leftLineNumber }}
    </td>
    <td class="px-1.5 bg-slate-950/50">
      <button
        v-if="showLeftCommentButton"
        @click="$emit('addComment', row.leftLine?.lineNumber, 'left')"
        class="opacity-0 group-hover:opacity-100 p-1 hover:bg-slate-700 rounded-md transition-all duration-200"
        title="Add comment on left side (old file)"
      >
        <svg class="w-3.5 h-3.5 text-rose-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
      </button>
    </td>
    <td
      :class="[
        'px-4 py-1 font-mono text-sm overflow-hidden',
        leftCellBgClass
      ]"
    >
      <span v-if="row.leftLine?.type === 'delete'" class="text-rose-400 select-none mr-1">-</span>
      <span v-else-if="row.leftLine?.type === 'context'" class="opacity-0 select-none mr-1">·</span>
      <span v-else class="select-none mr-1">&nbsp;</span>
      <code
        v-if="row.leftLine && row.leftLine.type !== 'spacer'"
        :class="row.leftLine.type === 'delete' ? 'diff-line-deleted' : 'diff-line-default'"
        style="white-space: pre-wrap; word-break: break-word; overflow-wrap: break-word;"
        v-html="renderLineContent(row.leftLine)"
      />
    </td>

    <td class="px-3 py-1 text-slate-600 text-right select-none border-x border-slate-800 bg-slate-950/50">
      {{ rightLineNumber }}
    </td>
    <td class="px-1.5 bg-slate-950/50">
      <button
        v-if="showRightCommentButton"
        @click="$emit('addComment', row.rightLine?.lineNumber, 'right')"
        class="opacity-0 group-hover:opacity-100 p-1 hover:bg-slate-700 rounded-md transition-all duration-200"
        title="Add comment on right side (new file)"
      >
        <svg class="w-3.5 h-3.5 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
      </button>
    </td>
    <td
      :class="[
        'px-4 py-1 font-mono text-sm overflow-hidden',
        rightCellBgClass
      ]"
    >
      <span v-if="row.rightLine?.type === 'add'" class="text-emerald-400 select-none mr-1">+</span>
      <span v-else-if="row.rightLine?.type === 'context'" class="opacity-0 select-none mr-1">·</span>
      <span v-else class="select-none mr-1">&nbsp;</span>
      <code
        v-if="row.rightLine && row.rightLine.type !== 'spacer'"
        :class="row.rightLine.type === 'add' ? 'diff-line-added' : 'diff-line-default'"
        style="white-space: pre-wrap; word-break: break-word; overflow-wrap: break-word;"
        v-html="renderLineContent(row.rightLine)"
      />
    </td>
  </tr>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { AlignedRow, AlignedLine } from '../types';

const props = defineProps<{
  row: AlignedRow;
  highlightedLine: number | null;
  renderLineContent: (line: AlignedLine | undefined) => string;
  setLineRef?: (lineNumber: number | undefined, el: HTMLElement | null) => void;
}>();

defineEmits<{
  addComment: [line: number | undefined, side: 'left' | 'right'];
}>();

const leftLineNumber = computed(() => {
  if (!props.row.leftLine) return '';
  const { type, lineNumber } = props.row.leftLine;
  return (type === 'delete' || type === 'context') && lineNumber ? lineNumber : '';
});

const rightLineNumber = computed(() => {
  if (!props.row.rightLine) return '';
  const { type, lineNumber } = props.row.rightLine;
  return (type === 'add' || type === 'context') && lineNumber ? lineNumber : '';
});

const showLeftCommentButton = computed(() => {
  if (!props.row.leftLine?.lineNumber) return false;
  const { type } = props.row.leftLine;
  return type === 'delete' || type === 'context';
});

const showRightCommentButton = computed(() => {
  if (!props.row.rightLine?.lineNumber) return false;
  const { type } = props.row.rightLine;
  return type === 'add' || type === 'context';
});

const rowClasses = computed(() => {
  const classes: string[] = [];
  
  if (props.row.rightLine?.type === 'add' && props.row.leftLine?.type !== 'delete') {
    classes.push('bg-emerald-500/5 border-l-2 border-emerald-500/50');
  } else if (props.row.leftLine?.type === 'delete' && props.row.rightLine?.type !== 'add') {
    classes.push('bg-rose-500/5 border-l-2 border-rose-500/50');
  } else if (props.row.leftLine?.type === 'delete' && props.row.rightLine?.type === 'add') {
    classes.push('bg-gradient-to-r from-rose-500/5 to-emerald-500/5');
  } else if (props.highlightedLine === props.row.rightLine?.lineNumber || props.highlightedLine === props.row.leftLine?.lineNumber) {
    classes.push('bg-amber-500/10 border-l-2 border-amber-500/50');
  } else {
    classes.push('border-l-2 border-transparent');
  }
  
  return classes.join(' ');
});

const leftCellBgClass = computed(() => {
  if (!props.row.leftLine) return 'bg-slate-950/20';
  if (props.row.leftLine.type === 'delete') return 'bg-rose-950/10';
  if (props.row.leftLine.type === 'spacer') return 'bg-slate-950/10 diff-spacer';
  return 'bg-slate-950/20';
});

const rightCellBgClass = computed(() => {
  if (!props.row.rightLine) return 'bg-slate-950/30';
  if (props.row.rightLine.type === 'add') return 'bg-emerald-950/10';
  if (props.row.rightLine.type === 'spacer') return 'bg-slate-950/10 diff-spacer';
  return 'bg-slate-950/30';
});

const setRef = (el: unknown) => {
  const lineNumber = props.row.rightLine?.lineNumber || props.row.leftLine?.lineNumber;
  if (props.setLineRef && lineNumber && el instanceof HTMLElement) {
    props.setLineRef(lineNumber, el);
  }
};
</script>
