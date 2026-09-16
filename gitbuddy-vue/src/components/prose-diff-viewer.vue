<template>
  <div class="bg-slate-950">
    <div v-if="loading" class="p-8 text-center">
      <div class="flex flex-col items-center gap-3">
        <svg class="animate-spin h-7 w-7 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
        </svg>
        <span class="text-sm text-slate-400">Comparing revisions…</span>
      </div>
    </div>

    <div v-else-if="fetchError" class="p-8 text-center">
      <div class="flex flex-col items-center gap-3">
        <svg class="w-12 h-12 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
        <p class="text-slate-400 text-sm">Couldn't load the document. Try the Source view.</p>
        <p class="text-slate-600 text-xs">{{ fetchError }}</p>
      </div>
    </div>

    <div v-else-if="loadInconsistency" class="p-8 text-center">
      <div class="flex flex-col items-center gap-3 max-w-lg mx-auto">
        <span class="font-mono text-amber-400 text-lg leading-none">⚠</span>
        <p class="text-slate-300 text-sm">Can't compare this file</p>
        <p class="text-slate-500 text-xs">{{ loadInconsistency }}</p>
        <p class="text-slate-500 text-xs">Use the Source view to see the exact change.</p>
      </div>
    </div>

    <div v-else-if="renderedSections.length === 0" class="p-8 text-center">
      <p class="text-slate-400 text-sm">No content to render</p>
    </div>

    <div v-else ref="contentRef" class="px-4 py-4">
      <div
        v-if="lowPairingQuality"
        class="mb-4 px-3 py-2 border border-slate-800 border-l-2 border-l-amber-600 text-xs text-slate-400"
      >
        This document restructured heavily, so matching blocks between revisions was unreliable.
        The Source view shows the exact change.
      </div>

      <ProseSectionMap :sections="sectionSummaries" @select="scrollToSection" />

      <section
        v-for="section in renderedSections"
        :key="section.domId"
        :id="section.domId"
        class="mb-5 pl-3 border-l-2"
        :class="borderFor(section.status)"
      >
        <h3 v-if="section.title" class="text-slate-100 mb-1">
          <span class="font-mono text-slate-600 mr-1.5">{{ '#'.repeat(section.depth) }}</span>
          <span :class="section.status === 'removed' ? 'line-through text-slate-600' : ''">{{ section.title }}</span>
          <span class="font-mono text-[10px] ml-2" :class="tagClassFor(section.status)">{{ tagFor(section) }}</span>
        </h3>

        <button
          v-if="section.status === 'unchanged' && !expanded.has(section.domId)"
          type="button"
          class="flex items-center gap-1.5 text-xs text-slate-500 hover:text-slate-400 py-0.5"
          @click="expand(section.domId)"
        >
          <span class="font-mono text-[10px] text-slate-600">▸</span>
          unchanged — {{ section.pairs.length }} {{ section.pairs.length === 1 ? 'block' : 'blocks' }}
        </button>

        <template v-else>
          <div
            v-if="section.suppressed.length > 0"
            class="my-2 px-3 py-1.5 border border-slate-800 rounded text-xs text-slate-500"
          >
            <div class="flex items-center gap-2">
              <span class="font-mono text-slate-600">≡</span>
              <span>
                {{ section.suppressed.length }} formatting-only
                {{ section.suppressed.length === 1 ? 'change' : 'changes' }} hidden in this section
              </span>
              <button
                type="button"
                class="text-blue-400 hover:text-blue-300 underline underline-offset-2"
                @click="toggleSuppressed(section.domId)"
              >{{ suppressedOpen.has(section.domId) ? 'hide' : 'show' }}</button>
            </div>
            <div
              v-if="suppressedOpen.has(section.domId)"
              class="font-mono text-[11px] text-slate-500 leading-relaxed pl-5 pt-1.5"
            >
              <div v-for="(reason, i) in section.suppressed" :key="i">{{ reason }}</div>
            </div>
          </div>

          <div
            class="prose prose-invert max-w-none text-slate-300 markdown-diff-content prose-diff-content"
            v-html="section.html"
          />
        </template>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue';
import type { FileDiff } from '../types';
import apiClient from '../utils/api';
import { renderMermaidBlocks } from '../composables/useMermaid';
import ProseSectionMap from './prose-section-map.vue';
import { parseBlocks, pathKey } from '../utils/proseDiff/parseBlocks';
import { pairBlocks, pairingQuality } from '../utils/proseDiff/pairBlocks';
import { classify, describeSuppressed } from '../utils/proseDiff/classifyChange';
import { renderPairedBlock } from '../utils/proseDiff/renderBlock';
import type { ClassifiedPair, PairStatus, SectionSummary } from '../utils/proseDiff/types';

const props = defineProps<{
  file: FileDiff;
  prId?: number;
}>();

const FULL_FILE_END_LINE = 1_000_000;
/** Below this, block matching is unreliable enough to warn about. */
const PAIRING_WARN_THRESHOLD = 0.5;

interface RenderedSection {
  domId: string;
  path: string[];
  title: string;
  depth: number;
  status: PairStatus;
  pairs: ClassifiedPair[];
  suppressed: string[];
  html: string;
}

const loading = ref(false);
const fetchError = ref<string | null>(null);
const oldText = ref('');
const newText = ref('');
const contentRef = ref<HTMLElement | null>(null);
const expanded = ref<Set<string>>(new Set());
const suppressedOpen = ref<Set<string>>(new Set());

/**
 * A revision that failed to load looks exactly like a revision that is legitimately empty, and
 * the difference matters enormously: rendering an unreachable "new" side as "every block was
 * removed" states something false with total confidence. Cross-check both sides against what
 * the patch says the file's status is, and refuse to diff when they disagree.
 */
const loadInconsistency = computed<string | null>(() => {
  if (loading.value || fetchError.value) return null;
  if (!oldText.value && !newText.value) return null;

  const status = (props.file.status ?? '').toLowerCase();

  if (!newText.value.trim() && status !== 'deleted') {
    return 'The updated version of this file could not be loaded, so the comparison would be wrong. This usually means the PR branch is unavailable — a fork, or a branch deleted after merge.';
  }
  if (!oldText.value.trim() && (status === 'modified' || status === 'renamed')) {
    return 'The previous version of this file could not be loaded, so the comparison would be wrong.';
  }
  return null;
});

const analysis = computed(() => {
  if (loadInconsistency.value) return null;
  if (!newText.value && !oldText.value) return null;

  const oldDoc = parseBlocks(oldText.value);
  const newDoc = parseBlocks(newText.value);
  const pairs = pairBlocks(oldDoc, newDoc).map<ClassifiedPair>(pair => {
    const changeClass = classify(pair);
    return {
      ...pair,
      changeClass,
      suppressionReason: changeClass === 'formatting-only' ? describeSuppressed(pair) : undefined,
    };
  });

  return { pairs, quality: pairingQuality(pairs), mapDepth: newDoc.mapDepth || oldDoc.mapDepth };
});

// Only meaningful when there were two sides to match. A newly added or wholly deleted file
// pairs nothing by definition, and warning about it would be noise on every new file.
const lowPairingQuality = computed(() => {
  if (!oldText.value.trim() || !newText.value.trim()) return false;
  return (analysis.value?.quality ?? 1) < PAIRING_WARN_THRESHOLD;
});

const renderedSections = computed<RenderedSection[]>(() => {
  if (!analysis.value) return [];
  const { pairs, mapDepth } = analysis.value;

  const order: string[] = [];
  const buckets = new Map<string, ClassifiedPair[]>();

  for (const pair of pairs) {
    const block = pair.new ?? pair.old;
    if (!block) continue;
    // Collapse deeper headings into their map-level ancestor so the map stays scannable.
    const path = block.sectionPath.slice(0, mapDepth);
    const key = pathKey(path);
    if (!buckets.has(key)) {
      buckets.set(key, []);
      order.push(key);
    }
    buckets.get(key)!.push(pair);
  }

  const sections = order.map(key => {
    const bucket = buckets.get(key) ?? [];
    const first = bucket[0];
    const path = (first?.new ?? first?.old)?.sectionPath.slice(0, mapDepth) ?? [];
    const heading = bucket.find(p => (p.new ?? p.old)!.kind === 'heading');
    const body = bucket.filter(p => p !== heading);
    const suppressed = body
      .filter(p => p.changeClass === 'formatting-only')
      .map(p => p.suppressionReason ?? '')
      .filter(Boolean);

    return {
      domId: `prose-sec-${slug(key)}`,
      path,
      title: path[path.length - 1] ?? '',
      depth: (heading?.new ?? heading?.old)?.depth ?? mapDepth,
      status: statusOf(body.length ? body : bucket),
      pairs: body,
      suppressed,
      html: body.filter(p => p.changeClass !== 'formatting-only').map(renderPairedBlock).join(''),
    };
  });

  // A heading whose content lives entirely in subsections (a document title above `##`
  // sections) has no body of its own — rendering "unchanged — 0 blocks" for it is noise.
  return sections.filter(s => s.pairs.length > 0 || s.status !== 'unchanged');
});

const sectionSummaries = computed<SectionSummary[]>(() =>
  renderedSections.value
    .filter(s => s.title)
    .map(s => ({
      path: s.path,
      title: s.title,
      domId: s.domId,
      status: s.status,
      changedCount: s.pairs.filter(p => p.status === 'changed' && p.changeClass === 'content').length,
      blockCount: s.pairs.length,
      suppressedCount: s.suppressed.length,
    }))
);

function statusOf(pairs: ClassifiedPair[]): PairStatus {
  if (pairs.length === 0) return 'unchanged';
  if (pairs.every(p => p.status === 'added')) return 'added';
  if (pairs.every(p => p.status === 'removed')) return 'removed';
  if (pairs.some(p => p.status !== 'unchanged' && p.changeClass !== 'formatting-only')) {
    return 'changed';
  }
  return 'unchanged';
}

function borderFor(status: PairStatus): string {
  switch (status) {
    case 'added': return 'border-emerald-900/70';
    case 'removed': return 'border-red-900/70';
    case 'changed': return 'border-amber-900/70';
    default: return 'border-transparent';
  }
}

function tagFor(section: RenderedSection): string {
  switch (section.status) {
    case 'added': return 'new section';
    case 'removed': return 'removed';
    case 'unchanged': return 'unchanged';
    default: {
      const n = section.pairs.filter(p => p.status === 'changed' && p.changeClass === 'content').length;
      return n === 1 ? '1 block changed' : `${n} blocks changed`;
    }
  }
}

function tagClassFor(status: PairStatus): string {
  switch (status) {
    case 'added': return 'text-emerald-400';
    case 'removed': return 'text-red-400';
    case 'changed': return 'text-amber-400';
    default: return 'text-slate-600';
  }
}

function slug(key: string): string {
  return key.replace(/[^a-z0-9]+/gi, '-').toLowerCase() || 'preamble';
}

function expand(domId: string): void {
  expanded.value = new Set(expanded.value).add(domId);
}

function toggleSuppressed(domId: string): void {
  const next = new Set(suppressedOpen.value);
  if (next.has(domId)) next.delete(domId);
  else next.add(domId);
  suppressedOpen.value = next;
}

async function scrollToSection(domId: string): Promise<void> {
  expand(domId);
  await nextTick();
  document.getElementById(domId)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
}

async function loadRevisions(): Promise<void> {
  if (!props.prId || !props.file.path) {
    fetchError.value = 'Missing PR or file path.';
    return;
  }

  loading.value = true;
  fetchError.value = null;
  try {
    const params = new URLSearchParams();
    params.append('path', props.file.path);
    params.append('oldStartLine', '1');
    params.append('oldEndLine', FULL_FILE_END_LINE.toString());
    params.append('newStartLine', '1');
    params.append('newEndLine', FULL_FILE_END_LINE.toString());

    const response = await apiClient.get<{
      oldLines: { lineNumber: number; content: string }[];
      newLines: { lineNumber: number; content: string }[];
    }>(`/pullrequests/${props.prId}/files/content?${params.toString()}`);

    oldText.value = (response.data.oldLines ?? []).map(l => l.content).join('\n');
    newText.value = (response.data.newLines ?? []).map(l => l.content).join('\n');
  } catch (err) {
    fetchError.value = err instanceof Error ? err.message : 'Failed to fetch file content';
  } finally {
    loading.value = false;
  }
}

watch(renderedSections, async () => {
  if (renderedSections.value.length === 0) return;
  await nextTick();
  if (contentRef.value) void renderMermaidBlocks(contentRef.value);
});

watch(
  () => [props.file.path, props.prId, props.file.patch] as const,
  () => {
    expanded.value = new Set();
    suppressedOpen.value = new Set();
    void loadRevisions();
  },
  { immediate: true }
);
</script>

<style>
.prose-diff-content .prose-block { margin: 0; }

.prose-diff-content .prose-added {
  background: rgba(2, 44, 34, 0.18);
  border-left: 1px solid rgb(6, 95, 70);
  padding: 2px 0 2px 10px;
  margin: 6px 0;
}

.prose-diff-content .prose-removed {
  background: rgba(44, 2, 2, 0.14);
  border-left: 1px solid rgb(127, 29, 29);
  padding: 2px 0 2px 10px;
  margin: 6px 0;
  color: rgb(71, 85, 105);
  text-decoration: line-through;
  text-decoration-color: rgba(71, 85, 105, 0.6);
}

.prose-diff-content ins.prose-ins {
  background: rgba(2, 44, 34, 0.35);
  color: rgb(52, 211, 153);
  text-decoration: none;
  border-bottom: 1px solid rgba(52, 211, 153, 0.4);
  padding: 0 1px;
}

.prose-diff-content del.prose-del {
  background: rgba(44, 2, 2, 0.35);
  color: rgb(248, 113, 113);
  text-decoration: line-through;
  text-decoration-color: rgba(248, 113, 113, 0.6);
  padding: 0 1px;
}
</style>
