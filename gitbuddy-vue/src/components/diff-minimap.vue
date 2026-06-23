<template>
  <div
    class="absolute right-0 top-0 bottom-0 w-1.5 bg-slate-800/40 border-l border-slate-700/30 z-20 rounded-l overflow-hidden"
  >
    <div
      v-for="(comment, index) in comments"
      :key="index"
      :style="{ top: getPosition(comment.line) + '%' }"
      class="absolute w-full h-1.5 bg-gradient-to-b from-blue-500/80 to-blue-600/80 cursor-pointer hover:from-blue-400 hover:to-blue-500 transition-all shadow-sm"
      :title="`Comment by ${comment.author}`"
      @click="$emit('scrollTo', comment.line)"
    />
  </div>
</template>

<script setup lang="ts">
import type { Comment, DiffHunk } from '../types';

const props = defineProps<{
  comments: Comment[];
  hunks: DiffHunk[];
}>();

defineEmits<{
  scrollTo: [line: number | undefined];
}>();

const getPosition = (line: number | undefined): number => {
  if (!line) return 0;
  const totalLines = props.hunks.reduce((sum, hunk) => sum + hunk.lines.length, 0);
  const lineIndex = props.hunks.reduce((sum, hunk) => {
    const lineInHunk = hunk.lines.findIndex((l: any) => l.newLineNumber === line);
    return lineInHunk >= 0 ? sum + lineInHunk : sum + hunk.lines.length;
  }, 0);
  return (lineIndex / totalLines) * 100;
};
</script>
