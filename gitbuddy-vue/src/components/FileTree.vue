<template>
  <div class="bg-slate-900 border-r border-slate-700 overflow-auto h-full" role="tree" aria-label="Files changed">
    <div class="sticky top-0 bg-slate-900 border-b border-slate-700 px-4 py-3 z-10">
      <div class="flex items-center justify-between">
        <h3 class="text-sm font-medium text-slate-200">Files Changed</h3>
        <button
          @click="toggleAll"
          class="text-xs text-slate-400 hover:text-slate-200"
        >
          {{ allExpanded ? 'Collapse' : 'Expand' }} all
        </button>
      </div>
      <div class="mt-2 text-xs flex items-center justify-between">
        <span class="text-slate-400">{{ totalFiles }} {{ totalFiles === 1 ? 'file' : 'files' }}</span>
        <div class="flex items-center gap-2">
          <button
            type="button"
            :class="[
              'flex items-center gap-1 rounded px-1 transition-colors',
              viewFilter === 'viewed'
                ? 'text-green-300 bg-green-500/10'
                : 'text-green-400 hover:text-green-300'
            ]"
            :aria-pressed="viewFilter === 'viewed'"
            title="Show only viewed files"
            @click="toggleViewFilter('viewed')"
          >
            <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
            </svg>
            {{ viewedCount }} viewed
          </button>
          <span class="text-slate-400">·</span>
          <button
            type="button"
            :class="[
              'rounded px-1 transition-colors',
              viewFilter === 'unviewed'
                ? 'text-slate-100 bg-slate-500/20'
                : 'text-slate-400 hover:text-slate-200'
            ]"
            :aria-pressed="viewFilter === 'unviewed'"
            title="Show only unviewed files"
            @click="toggleViewFilter('unviewed')"
          >
            {{ remainingCount }} remaining
          </button>
        </div>
      </div>
      <div class="mt-2 relative">
        <svg class="absolute left-2 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Filter files..."
          class="w-full bg-slate-800 border border-slate-700 rounded px-8 py-1.5 text-sm text-slate-200 placeholder-slate-500 focus:outline-none focus:border-slate-500"
          @keydown.escape="searchQuery = ''"
        />
        <button
          v-if="searchQuery"
          @click="searchQuery = ''"
          class="absolute right-2 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-300"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>
    </div>

    <div class="p-2">
      <div v-if="filteredNodes.length === 0 && searchQuery" class="px-2 py-4 text-sm text-slate-500 text-center">
        No files match "{{ searchQuery }}"
      </div>
      <FileTreeNode
        v-for="node in filteredNodes"
        :key="node.path"
        :node="node"
        :selected-file="selectedFile"
        :collapsed-folders="collapsedFolders"
        :files-with-comments="filesWithComments"
        :viewed-files="viewedFiles"
        @select="$emit('selectFile', $event)"
        @toggle-folder="toggleFolder"
        @toggle-viewed="(path, viewed) => $emit('toggleViewed', path, viewed)"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, shallowRef, computed, watch } from 'vue';
import { buildFileTree, type FileTreeNode as TreeNode } from '../utils/diffHelpers';
import FileTreeNode from './FileTreeNode.vue';
import type { FileDiff } from '../types';

const props = defineProps<{
  files: FileDiff[];
  selectedFile?: string;
  viewedFiles?: Set<string>;
}>();

defineEmits<{
  selectFile: [path: string];
  toggleViewed: [path: string, viewed: boolean];
}>();

const collapsedFolders = shallowRef<Set<string>>(new Set());
const searchQuery = ref('');
const viewFilter = ref<'all' | 'viewed' | 'unviewed'>('all');

const toggleViewFilter = (mode: 'viewed' | 'unviewed') => {
  viewFilter.value = viewFilter.value === mode ? 'all' : mode;
};

const filesWithComments = computed(() => {
  const commented = new Set<string>();
  return commented;
});

const isFileViewed = (path: string): boolean =>
  props.viewedFiles?.has(path) ?? false;

const treeNodes = computed(() => {
  return buildFileTree(props.files.filter(f => f.path).map(f => ({ path: f.path!, status: f.status || 'modified' })));
});

const filteredNodes = computed(() => {
  let nodes = treeNodes.value;
  if (viewFilter.value !== 'all') {
    nodes = filterByViewed(nodes, viewFilter.value);
  }
  if (searchQuery.value.trim()) {
    nodes = filterTreeNodes(nodes, searchQuery.value.toLowerCase());
  }
  return nodes;
});

const filterByViewed = (nodes: TreeNode[], mode: 'viewed' | 'unviewed'): TreeNode[] => {
  const result: TreeNode[] = [];
  for (const node of nodes) {
    if (node.type === 'file') {
      const viewed = isFileViewed(node.path);
      if ((mode === 'viewed') === viewed) {
        result.push(node);
      }
    } else {
      const filteredChildren = node.children ? filterByViewed(Object.values(node.children), mode) : [];
      if (filteredChildren.length > 0) {
        result.push({
          ...node,
          children: Object.fromEntries(filteredChildren.map(c => [c.name, c]))
        });
      }
    }
  }
  return result;
};

const filterTreeNodes = (nodes: TreeNode[], query: string): TreeNode[] => {
  const result: TreeNode[] = [];
  for (const node of nodes) {
    if (node.type === 'file') {
      if (node.name.toLowerCase().includes(query) || node.path.toLowerCase().includes(query)) {
        result.push(node);
      }
    } else {
      const filteredChildren = node.children ? filterTreeNodes(Object.values(node.children), query) : [];
      if (filteredChildren.length > 0) {
        result.push({
          ...node,
          children: Object.fromEntries(filteredChildren.map(c => [c.name, c]))
        });
      }
    }
  }
  return result;
};

const totalFiles = computed(() => props.files.length);

const viewedCount = computed(() => {
  if (props.viewedFiles) {
    return props.files.filter(f => f.path && props.viewedFiles!.has(f.path)).length;
  }
  return props.files.filter(f => f.viewedState === 'VIEWED' || f.viewed === true).length;
});

const remainingCount = computed(() => Math.max(0, totalFiles.value - viewedCount.value));

const toggleFolder = (path: string) => {
  const newSet = new Set(collapsedFolders.value);
  if (newSet.has(path)) {
    newSet.delete(path);
  } else {
    newSet.add(path);
  }
  collapsedFolders.value = newSet;
};

const collectAllFolderPaths = (nodes: TreeNode[]): string[] => {
  return nodes.flatMap(node => {
    if (node.type === 'folder') {
      const childPaths = node.children ? collectAllFolderPaths(Object.values(node.children)) : [];
      return [node.path, ...childPaths];
    }
    return [];
  });
};

const allExpanded = computed(() => collapsedFolders.value.size === 0);

const toggleAll = () => {
  if (allExpanded.value) {
    collapsedFolders.value = new Set(collectAllFolderPaths(treeNodes.value));
  } else {
    collapsedFolders.value = new Set();
  }
};

watch(searchQuery, (query) => {
  if (query.trim()) {
    collapsedFolders.value = new Set();
  }
});

watch(viewFilter, (mode) => {
  if (mode !== 'all') {
    collapsedFolders.value = new Set();
  }
});
</script>
