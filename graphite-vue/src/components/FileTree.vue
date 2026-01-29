<template>
  <div class="bg-slate-900 border-r border-slate-700 overflow-auto">
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
          <span class="text-green-400 flex items-center gap-1">
            <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
            </svg>
            {{ viewedCount }} viewed
          </span>
        </div>
      </div>
    </div>

    <div class="p-2">
      <FileTreeNode
        v-for="node in treeNodes"
        :key="node.path"
        :node="node"
        :selected-file="selectedFile"
        :expanded-folders="expandedFolders"
        @select="$emit('selectFile', $event)"
        @toggle-folder="toggleFolder"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { buildFileTree, type FileTreeNode as TreeNode } from '../utils/diffHelpers';
import FileTreeNode from './FileTreeNode.vue';
import type { FileDiff } from '../types';

const props = defineProps<{
  files: FileDiff[];
  selectedFile?: string;
}>();

defineEmits<{
  selectFile: [path: string];
}>();

const expandedFolders = ref<Set<string>>(new Set());
const allExpanded = ref(false);

const treeNodes = computed(() => {
  return buildFileTree(props.files.filter(f => f.path).map(f => ({ path: f.path!, status: f.status || 'modified' })));
});

const totalFiles = computed(() => props.files.length);

const toggleFolder = (path: string) => {
  if (expandedFolders.value.has(path)) {
    expandedFolders.value.delete(path);
  } else {
    expandedFolders.value.add(path);
  }
};

const toggleAll = () => {
  allExpanded.value = !allExpanded.value;
  expandedFolders.value.clear();
  
  if (allExpanded.value) {
    const addAllFolders = (nodes: TreeNode[]) => {
      nodes.forEach(node => {
        if (node.type === 'folder') {
          expandedFolders.value.add(node.path);
          if (node.children) {
            addAllFolders(Object.values(node.children));
          }
        }
      });
    };
    addAllFolders(treeNodes.value);
  }
};
</script>
