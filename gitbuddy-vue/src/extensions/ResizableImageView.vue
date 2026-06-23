<template>
  <NodeViewWrapper as="div" class="resizable-image-wrapper" style="position: relative; display: inline-block;">
    <img
      :src="node.attrs.src"
      :alt="node.attrs.alt || ''"
      :style="imgStyle"
      class="resizable-img"
      :class="{ 'is-uploading': node.attrs['data-uploading'] }"
      draggable="false"
    />
    <div v-if="isResizing" class="dimension-label">
      {{ Math.round(currentWidth) }} × {{ Math.round(currentHeight) }}
    </div>
    <div
      v-for="handle in handles"
      :key="handle"
      :class="['resize-handle', `handle-${handle}`]"
      @mousedown.prevent.stop="startDrag($event, handle)"
    />
  </NodeViewWrapper>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { NodeViewWrapper } from '@tiptap/vue-3';
import type { NodeViewProps } from '@tiptap/vue-3';

const props = defineProps<NodeViewProps>();

const naturalWidth = ref(0);
const naturalHeight = ref(0);
const currentWidth = ref(0);
const currentHeight = ref(0);
const isResizing = ref(false);

const imgStyle = computed(() => {
  if (currentWidth.value > 0) {
    return { width: `${currentWidth.value}px`, maxWidth: '100%' };
  }
  return { maxWidth: '100%' };
});

const handles = ['tl', 'tr', 'bl', 'br'] as const;

let dragStartX = 0;
let dragStartW = 0;
let activeHandle: string | null = null;

const MIN_SIZE = 50;

const startDrag = (event: MouseEvent, handle: string) => {
  activeHandle = handle;
  dragStartX = event.clientX;
  dragStartW = currentWidth.value || naturalWidth.value || 300;
  isResizing.value = true;

  document.addEventListener('mousemove', onDrag);
  document.addEventListener('mouseup', stopDrag);
};

const onDrag = (event: MouseEvent) => {
  if (!activeHandle) return;

  const dx = event.clientX - dragStartX;
  const isLeft = activeHandle === 'tl' || activeHandle === 'bl';
  const delta = isLeft ? -dx : dx;

  let newW = Math.max(MIN_SIZE, dragStartW + delta);
  const maxW = naturalWidth.value || 2000;
  newW = Math.min(newW, maxW);

  currentWidth.value = Math.round(newW);

  if (naturalWidth.value && naturalHeight.value) {
    currentHeight.value = Math.round((newW / naturalWidth.value) * naturalHeight.value);
  }

  props.updateAttributes({ width: Math.round(newW) });
};

const stopDrag = () => {
  activeHandle = null;
  isResizing.value = false;
  document.removeEventListener('mousemove', onDrag);
  document.removeEventListener('mouseup', stopDrag);
};

onMounted(() => {
  const img = new Image();
  img.onload = () => {
    naturalWidth.value = img.width;
    naturalHeight.value = img.height;

    const attrWidth = props.node.attrs.width;
    if (attrWidth) {
      currentWidth.value = attrWidth;
      currentHeight.value = Math.round((attrWidth / img.width) * img.height);
    } else {
      const maxDisplay = Math.min(img.width, 600);
      if (maxDisplay < img.width) {
        currentWidth.value = maxDisplay;
        currentHeight.value = Math.round((maxDisplay / img.width) * img.height);
        props.updateAttributes({ width: maxDisplay });
      } else {
        currentWidth.value = img.width;
        currentHeight.value = img.height;
      }
    }
  };
  img.src = props.node.attrs.src;
});

onUnmounted(() => {
  document.removeEventListener('mousemove', onDrag);
  document.removeEventListener('mouseup', stopDrag);
});
</script>

<style scoped>
.resizable-image-wrapper {
  margin: 8px 0;
  max-width: 100%;
}

.resizable-img {
  border-radius: 8px;
  display: block;
  max-width: 100%;
}

.resizable-img.is-uploading {
  opacity: 0.5;
  filter: blur(1px);
}

.resizable-image-wrapper:hover .resize-handle {
  opacity: 1;
}

.resize-handle {
  width: 10px;
  height: 10px;
  background: #3b82f6;
  border: 1.5px solid #93c5fd;
  border-radius: 50%;
  position: absolute;
  z-index: 10;
  opacity: 0;
  transition: opacity 0.15s, transform 0.15s;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.4);
}

.resize-handle:hover {
  background: #60a5fa;
  transform: scale(1.3);
}

.handle-tl { top: -5px; left: -5px; cursor: nwse-resize; }
.handle-tr { top: -5px; right: -5px; cursor: nesw-resize; }
.handle-bl { bottom: -5px; left: -5px; cursor: nesw-resize; }
.handle-br { bottom: -5px; right: -5px; cursor: nwse-resize; }

.dimension-label {
  position: absolute;
  bottom: -24px;
  left: 50%;
  transform: translateX(-50%);
  background: #1e293b;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 11px;
  color: #94a3b8;
  font-family: monospace;
  white-space: nowrap;
  pointer-events: none;
}
</style>
