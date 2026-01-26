<template>
  <div>
    <div
      :class="[
        'flex items-center gap-2 px-2 py-1.5 rounded cursor-pointer hover:bg-slate-800 transition-colors',
        { 'bg-slate-800': isSelected }
      ]"
      :style="{ paddingLeft: `${depth * 12 + 8}px` }"
      @click="handleClick"
    >
      <!-- Folder Icon or File Icon -->
      <svg
        v-if="node.type === 'folder'"
        :class="['w-4 h-4 text-slate-400 transition-transform flex-shrink-0', { 'rotate-90': isExpanded }]"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
      </svg>
      
      <span
        v-else
        :class="['text-xs font-medium flex-shrink-0', getStatusColor(node.status)]"
      >
        {{ getStatusIcon(node.status) }}
      </span>

      <!-- Name -->
      <span
        :class="[
          'text-sm truncate',
          node.type === 'folder' ? 'text-slate-300 font-medium' : 'text-slate-400'
        ]"
        :title="node.name"
      >
        {{ node.name }}
      </span>
    </div>

    <!-- Children -->
    <template v-if="node.type === 'folder' && isExpanded && node.children">
      <FileTreeNode
        v-for="child in Object.values(node.children)"
        :key="child.path"
        :node="child"
        :depth="depth + 1"
        :selected-file="selectedFile"
        :expanded-folders="expandedFolders"
        @select="$emit('select', $event)"
        @toggle-folder="$emit('toggleFolder', $event)"
      />
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { FileTreeNode as TreeNode } from '../utils/diffHelpers';

const props = defineProps<{
  node: TreeNode;
  depth?: number;
  selectedFile?: string;
  expandedFolders: Set<string>;
}>();

const emit = defineEmits<{
  select: [path: string];
  toggleFolder: [path: string];
}>();

const depth = computed(() => props.depth || 0);
const isSelected = computed(() => props.node.path === props.selectedFile && props.node.type === 'file');
const isExpanded = computed(() => props.expandedFolders.has(props.node.path));

const handleClick = () => {
  if (props.node.type === 'folder') {
    emit('toggleFolder', props.node.path);
  } else {
    emit('select', props.node.path);
  }
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
</script>
