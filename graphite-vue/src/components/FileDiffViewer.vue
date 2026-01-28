<template>
  <div class="border border-slate-700/50 rounded-lg overflow-hidden bg-slate-900/50">
    <!-- File Header -->
    <div
      class="flex items-center justify-between px-3 py-2 bg-slate-800/80 border-b border-slate-700/50 cursor-pointer hover:bg-slate-800"
      @click="handleHeaderClick"
    >
      <div class="flex items-center gap-2 min-w-0">
        <!-- Expand/Collapse Icon -->
        <svg
          :class="['w-3.5 h-3.5 text-slate-400 transition-transform flex-shrink-0', { 'rotate-90': expanded }]"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>

        <!-- File Status Icon -->
        <span :class="['text-xs font-semibold flex-shrink-0', getStatusColor(file.status)]">
          {{ getStatusIcon(file.status) }}
        </span>

        <!-- File Path -->
        <span class="text-sm font-mono text-slate-200 truncate">{{ file.path }}</span>
      </div>

      <!-- File Stats -->
      <div class="flex items-center gap-3 text-xs flex-shrink-0">
        <span class="text-green-400 font-medium">+{{ file.Additions }}</span>
        <span class="text-red-400 font-medium">-{{ file.Deletions }}</span>
        <span v-if="expanded" class="text-slate-500">|</span>
        <button
          v-if="expanded"
          @click.stop="toggleViewMode"
          class="px-2 py-1 rounded bg-slate-700/50 hover:bg-slate-700 text-slate-300 transition-colors"
        >
          {{ viewMode === 'split' ? 'Unified' : 'Split' }}
        </button>
        <button
          v-if="expanded"
          @click.stop="toggleUnchangedLines"
          class="px-2 py-1 rounded bg-slate-700/50 hover:bg-slate-700 text-slate-300 transition-colors"
        >
          {{ showUnchangedLines ? 'Hide' : 'Show' }} context
        </button>
      </div>
    </div>

    <!-- File Diff Content -->
    <div v-if="expanded && !loading" class="relative bg-slate-950/50">
      <!-- Minimap -->
      <div
        v-if="hunks.length > 0"
        class="absolute right-0 top-0 bottom-0 w-2 bg-slate-800/50 border-l border-slate-700/50 z-10"
      >
        <div
          v-for="(comment, index) in fileComments"
          :key="index"
          :style="{ top: getCommentPosition(comment.line) + '%' }"
          class="absolute w-full h-1 bg-blue-500 cursor-pointer hover:bg-blue-400"
          :title="`Comment by ${comment.author}`"
          @click="scrollToLine(comment.line)"
        />
      </div>

      <!-- Split View -->
      <div v-if="viewMode === 'split'" class="overflow-x-hidden text-xs font-mono" ref="diffContainer">
        <table class="w-full border-collapse table-fixed">
          <colgroup>
            <col style="width: 50px;">
            <col style="width: calc(50% - 78px);">
            <col style="width: 50px;">
            <col style="width: 28px;">
            <col style="width: calc(50% - 50px);">
          </colgroup>
          <tbody>
            <template v-for="(hunk, hunkIndex) in hunks" :key="hunkIndex">
              <!-- Hunk Header -->
              <tr class="bg-slate-800/50">
                <td colspan="5" class="px-3 py-1.5 text-slate-400 text-xs border-y border-slate-700/50">
                  @@ -{{ hunk.oldStart }},{{ hunk.oldLines }} +{{ hunk.newStart }},{{ hunk.newLines }} @@
                </td>
              </tr>

              <template v-for="(line, lineIndex) in getVisibleLines(hunk.lines)" :key="`${hunkIndex}-${lineIndex}`">
                <tr
                  :ref="el => setLineRef(line.newLineNumber || line.oldLineNumber, el)"
                  :class="[
                    'hover:bg-slate-800/30 group',
                    {
                      'bg-green-500/10': line.type === 'add',
                      'bg-red-500/10': line.type === 'delete',
                    }
                  ]"
                >
                  <!-- Old Side -->
                  <td class="px-2 py-0.5 text-slate-500 text-right select-none border-r border-slate-700/50 bg-slate-900/50">
                    {{ line.type !== 'add' ? line.oldLineNumber : '' }}
                  </td>
                  <td
                    :class="[
                      'px-3 py-0.5 font-mono text-sm overflow-hidden',
                      line.type === 'delete' ? 'bg-red-500/5' : 'bg-slate-900/30'
                    ]"
                  >
                    <span v-if="line.type === 'delete'" class="text-red-400 select-none">-</span>
                    <span v-else class="opacity-0 select-none">·</span>
                    <code
                      v-if="line.type !== 'add'"
                      :class="line.type === 'delete' ? 'text-red-300' : 'text-slate-300'"
                      style="white-space: pre-wrap; word-break: break-word; overflow-wrap: break-word;"
                      v-html="highlightSyntax(line.content)"
                    />
                  </td>

                  <!-- New Side -->
                  <td class="px-2 py-0.5 text-slate-500 text-right select-none border-x border-slate-700/50 bg-slate-900/50">
                    {{ line.type !== 'delete' ? line.newLineNumber : '' }}
                  </td>
                  <td class="px-1 bg-slate-900/50">
                    <button
                      v-if="line.newLineNumber && line.type !== 'delete'"
                      @click="startComment(line.newLineNumber!)"
                      class="opacity-0 group-hover:opacity-100 p-0.5 hover:bg-slate-700 rounded transition-opacity"
                      title="Add comment"
                    >
                      <svg class="w-3 h-3 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                      </svg>
                    </button>
                  </td>
                    <code
                      :class="[
                        'px-3 py-0.5 font-mono text-sm overflow-hidden',
                        line.type === 'add' ? 'bg-green-500/5' : 'bg-slate-900/30'
                      ]"
                    >
                    <span v-if="line.type === 'add'" class="text-green-400 select-none">+</span>
                    <span v-else-if="line.type === 'delete'" class="text-red-400 select-none mr-1">-</span>
                    <span v-else class="opacity-0 select-none mr-1">·</span>
                    <code style="white-space: pre-wrap; word-break: break-word; overflow-wrap: break-word;" v-html="highlightSyntax(line.content)" />
                  </td>
                </tr>

                <!-- Comments -->
                <tr v-if="commentingLine === line.newLineNumber || getCommentsForLine(line.newLineNumber).length > 0">
                  <td colspan="5" class="p-0 bg-slate-800/50">
                    <div v-if="commentingLine === line.newLineNumber" class="p-3 border-t border-slate-700/50">
                      <textarea
                        v-model="commentText"
                        placeholder="Add your comment..."
                        class="w-full px-3 py-2 bg-slate-900 border border-slate-700 rounded text-slate-200 text-sm resize-none focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
                        rows="3"
                        ref="commentTextarea"
                      />
                      <div class="flex gap-2 justify-end mt-2">
                        <button
                          @click="cancelComment"
                          class="px-3 py-1 bg-slate-700 hover:bg-slate-600 rounded text-xs text-slate-200 transition-colors"
                        >
                          Cancel
                        </button>
                        <button
                          @click="submitComment"
                          :disabled="!commentText.trim()"
                          class="px-3 py-1 bg-blue-600 hover:bg-blue-700 rounded text-xs text-white disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                        >
                          Comment
                        </button>
                      </div>
                    </div>

                    <div v-if="getCommentsForLine(line.newLineNumber).length > 0" class="p-3 space-y-2 border-t border-slate-700/50">
                      <div
                        v-for="comment in getCommentsForLine(line.newLineNumber)"
                        :key="comment.id"
                        class="flex gap-2 p-2 bg-slate-900/80 rounded border border-slate-700/50 hover:border-slate-600 transition-colors"
                      >
                        <img
                          v-if="comment.authorAvatar"
                          :src="comment.authorAvatar"
                          :alt="comment.author"
                          class="w-6 h-6 rounded-full flex-shrink-0"
                        />
                        <div class="flex-1 min-w-0">
                          <div class="flex items-center gap-2 mb-1">
                            <span class="text-sm font-medium text-slate-200">{{ comment.author }}</span>
                            <span class="text-xs text-slate-500">{{ formatRelativeTime(comment.createdAt) }}</span>
                          </div>

                          <!-- File Path and Line -->
                          <div v-if="comment.path" class="flex items-center gap-2 text-xs text-slate-400">
                            <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                    d="M7 21h10a2 2 0 002-2V9.414a1 1 0 01-1.414 0l-4-4a1 1 0 01-1.414 1.414L8 12.586l7.293-7.293a1 1 0 01-1.414 1.414 0z" clip-rule="evenodd" />
                            </svg>
                            <span class="truncate font-mono">{{ comment.path }}</span>
                            <span v-if="comment.line">:{{ comment.line }}</span>
                          </div>

                          <!-- Outdated Badge -->
                          <span
                            v-if="comment.isOutdated"
                            class="px-2 py-0.5 text-xs bg-orange-900/30 text-orange-400 rounded border border-orange-700/50"
                          >
                            Outdated
                          </span>
                        </div>

                        <!-- Comment Body -->
                        <p class="text-sm text-slate-300 whitespace-pre-wrap">{{ comment.body }}</p>
                      </div>
                    </div>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>
      </div>

    <!-- Loading State -->
    <div v-if="loading" class="p-6 text-center text-slate-400 bg-slate-900/50">
      <svg class="animate-spin h-6 w-6 mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0-3.042 1.135 5.824 3.7.938l3.2.647z"></path>
      </svg>
      <span class="text-xs">Loading diff...</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, nextTick } from 'vue';
import type { FileDiff, Comment } from '../types';
import { parsePatch } from '../utils/diffHelpers';
import { highlightCode, detectLanguageFromPath } from '../utils/syntaxHighlight';
import { useUserPreferences } from '../composables/useUserPreferences';

const props = defineProps<{
  file: FileDiff;
  comments: Comment[];
  onAddComment: (line: number, body: string) => Promise<void>;
  initialExpanded?: boolean;
  viewed?: boolean;
}>();

defineEmits<{
  toggleViewed: [path: string, viewed: boolean];
  changeViewed: [path: string, viewed: boolean];
}>();

const { preferences, setDiffViewMode } = useUserPreferences();

const expanded = ref(props.initialExpanded || false);
const loading = ref(false);
const hunks = ref<any[]>([]);
const showUnchangedLines = ref(false);
const commentingLine = ref<number | null>(null);
const commentText = ref('');
const commentTextarea = ref<HTMLTextAreaElement | null>(null);
const lineRefs = ref<Map<number, HTMLElement>>(new Map());
const language = ref(detectLanguageFromPath(props.file.path));

const viewMode = computed(() => preferences.value.diffViewMode);

const fileComments = computed(() => 
  props.comments.filter(c => c.path === props.file.path && c.line)
);

const toggleExpanded = async () => {
  expanded.value = !expanded.value;
  
  if (expanded.value && hunks.value.length === 0 && props.file.patch) {
    loading.value = true;
    await new Promise(resolve => setTimeout(resolve, 100));
    hunks.value = parsePatch(props.file.patch);
    loading.value = false;
  }
};

const handleHeaderClick = (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  if (target.tagName !== 'INPUT' && target.closest('input') === null) {
    toggleExpanded();
  }
};

const toggleViewed = (e: Event) => {
  const checked = (e.target as HTMLInputElement).checked;
  if (props.onToggleViewed) {
    props.onToggleViewed(props.file.path!, checked);
  }
  emit('toggleViewed', props.file.path!, checked);
  emit('changeViewed', props.file.path!, checked);
};

const toggleViewMode = async () => {
  const newMode = viewMode.value === 'split' ? 'unified' : 'split';
  await setDiffViewMode(newMode);
};

const toggleUnchangedLines = () => {
  showUnchangedLines.value = !showUnchangedLines.value;
};

const getVisibleLines = (lines: any[]) => {
  if (showUnchangedLines.value) return lines;

  const result: any[] = [];
  const contextLines = 3;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    if (line.type !== 'context') {
      result.push(line);
      continue;
    }

    let hasNearbyChange = false;
    for (let j = Math.max(0, i - contextLines); j <= Math.min(lines.length - 1, i + contextLines); j++) {
      if (lines[j].type !== 'context') {
        hasNearbyChange = true;
        break;
      }
    }

    if (hasNearbyChange) result.push(line);
  }

  return result;
};

const highlightSyntax = (code: string): string => {
  return highlightCode(code, language.value);
};

const getStatusIcon = (status: string) => {
  const icons: Record<string, string> = {
    added: 'A',
    modified: 'M',
    deleted: 'D',
    renamed: 'R',
  };
  return icons[status] || 'M';
};

const getStatusColor = (status: string) => {
  const colors: Record<string, string> = {
    added: 'text-green-400',
    modified: 'text-yellow-400',
    deleted: 'text-red-400',
    renamed: 'text-blue-400',
  };
  return colors[status] || 'text-slate-400';
};

const startComment = async (line: number) => {
  commentingLine.value = line;
  await nextTick();
  commentTextarea.value?.focus();
};

const cancelComment = () => {
  commentingLine.value = null;
  commentText.value = '';
};

const submitComment = async () => {
  if (!commentText.value.trim() || commentingLine.value === null) return;

  await props.onAddComment(commentingLine.value!, commentText.value);
  commentText.value = '';
  commentingLine.value = null;
};

const getCommentsForLine = (line: number | undefined): Comment[] => {
  if (!line) return [];
  return fileComments.value.filter(c => c.line === line);
};

const getCommentPosition = (line: number | undefined): number => {
  if (!line) return 0;
  const totalLines = hunks.value.reduce((sum, hunk) => sum + hunk.lines.length, 0);
  const lineIndex = hunks.value.reduce((sum, hunk) => {
    const lineInHunk = hunk.lines.findIndex((l: any) => l.newLineNumber === line);
    return lineInHunk >= 0 ? sum + lineInHunk : sum + hunk.lines.length;
  }, 0);
  return (lineIndex / totalLines) * 100;
};

const scrollToLine = (line: number | undefined) => {
  if (!line) return;
  const el = lineRefs.value.get(line);
  if (el) {
    el.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }
};

const setLineRef = (lineNumber: number | undefined, el: any) => {
  if (lineNumber && el) {
    lineRefs.value.set(lineNumber, el);
  }
};

const formatRelativeTime = (dateString: string): string => {
  const date = new Date(dateString);
  const now = new Date();
  const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
};
</script>

<style scoped>
/* Prism syntax highlighting theme for dark backgrounds */
:deep(.token.comment),
:deep(.token.prolog),
:deep(.token.doctype),
:deep(.token.cdata) {
  color: #6b7280;
}

:deep(.token.punctuation) {
  color: #94a3b8;
}

:deep(.token.property),
:deep(.token.tag),
:deep(.token.boolean),
:deep(.token.number),
:deep(.token.constant),
:deep(.token.symbol),
:deep(.token.deleted) {
  color: #f87171;
}

:deep(.token.selector),
:deep(.token.attr-name),
:deep(.token.string),
:deep(.token.char),
:deep(.token.builtin),
:deep(.token.inserted) {
  color: #4ade80;
}

:deep(.token.operator),
:deep(.token.entity),
:deep(.token.url),
:deep(.language-css .token.string),
:deep(.style .token.string) {
  color: #60a5fa;
}

:deep(.token.atrule),
:deep(.token.attr-value),
:deep(.token.keyword) {
  color: #a78bfa;
}

:deep(.token.function),
:deep(.token.class-name) {
  color: #fbbf24;
}

:deep(.token.regex),
:deep(.token.important),
:deep(.token.variable) {
  color: #fb923c;
}
</style>
