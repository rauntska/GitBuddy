<template>
  <div>
    <div
      :class="[
        'flex items-center gap-2 px-2 py-1.5 rounded cursor-pointer hover:bg-slate-800 transition-colors',
        { 'bg-slate-800': isSelected }
      ]"
      :style="{ paddingLeft: `${depth * 12 + 8}px` }"
      role="treeitem"
      :aria-selected="isSelected"
      :aria-expanded="node.type === 'folder' ? isExpanded : undefined"
      :tabindex="isSelected ? 0 : -1"
      @click="handleClick"
      @keydown.enter="handleClick"
    >
      <svg
        v-if="node.type === 'folder'"
        :class="['w-4 h-4 text-slate-400 transition-transform flex-shrink-0', { 'rotate-90': isExpanded }]"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
        aria-hidden="true"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
      </svg>
      
      <template v-else>
        <span
          :class="['w-2 h-2 rounded-full flex-shrink-0', statusIndicator.color]"
          :title="statusIndicator.label"
          aria-hidden="true"
        />
        <span :class="['text-xs font-medium flex-shrink-0', fileIcon.color]" aria-hidden="true">
          {{ fileIcon.icon }}
        </span>
      </template>

      <span
        :class="[
          'text-sm truncate flex-1',
          node.type === 'folder' ? 'text-slate-300 font-medium' : 'text-slate-400'
        ]"
        :title="node.name"
      >
        {{ node.name }}
      </span>

      <span
        v-if="node.type === 'file' && hasComments"
        class="text-xs text-blue-400 flex items-center gap-1"
        title="Has comments"
      >
        <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M18 10c0 3.866-3.582 7-8 7a8.841 8.841 0 01-4.083-.98L2 17l1.338-3.123C2.493 12.767 2 11.434 2 10c0-3.866 3.582-7 8-7s8 3.134 8 7zM7 9H5v2h2V9zm8 0h-2v2h2V9zM9 9h2v2H9V9z" clip-rule="evenodd" />
        </svg>
      </span>
    </div>

    <div
      v-if="node.type === 'folder' && isExpanded && node.children"
      role="group"
    >
      <FileTreeNode
        v-for="child in Object.values(node.children)"
        :key="child.path"
        :node="child"
        :depth="depth + 1"
        :selected-file="selectedFile"
        :expanded-folders="expandedFolders"
        :files-with-comments="filesWithComments"
        @select="$emit('select', $event)"
        @toggle-folder="$emit('toggleFolder', $event)"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { FileTreeNode as TreeNode } from '../utils/diffHelpers';
import { useFileIcons } from '../composables/useFileIcons';

const props = defineProps<{
  node: TreeNode;
  depth?: number;
  selectedFile?: string;
  expandedFolders: Set<string>;
  filesWithComments?: Set<string>;
}>();

const emit = defineEmits<{
  select: [path: string];
  toggleFolder: [path: string];
}>();

const { getFileIcon, getStatusIndicator } = useFileIcons();

const depth = computed(() => props.depth || 0);
const isSelected = computed(() => props.node.path === props.selectedFile && props.node.type === 'file');
const isExpanded = computed(() => props.expandedFolders.has(props.node.path));
const hasComments = computed(() => props.filesWithComments?.has(props.node.path) ?? false);

const fileIcon = computed(() => getFileIcon(props.node.name));
const statusIndicator = computed(() => getStatusIndicator(props.node.status));

const handleClick = () => {
  if (props.node.type === 'folder') {
    emit('toggleFolder', props.node.path);
  } else {
    emit('select', props.node.path);
  }
};
</script>
