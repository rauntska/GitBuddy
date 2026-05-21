<template>
  <Teleport to="body">
    <div
      v-if="visible"
      class="fixed inset-0 z-[9999]"
      @click="close"
      @contextmenu.prevent="close"
    >
      <div
        ref="menuRef"
        class="fixed bg-slate-800 border border-slate-700 rounded-lg shadow-2xl py-1 min-w-[200px] max-w-[280px] overflow-hidden"
        :style="menuStyle"
        @click.stop
      >
        <template v-for="(item, index) in items" :key="index">
          <div v-if="item.divider" class="border-t border-slate-700 my-1" />
          <button
            v-else
            :disabled="item.disabled"
            :class="[
              'w-full flex items-center gap-3 px-3 py-2 text-sm transition-colors text-left',
              item.disabled
                ? 'text-slate-600 cursor-not-allowed'
                : item.danger
                  ? 'text-red-400 hover:bg-red-500/10'
                  : 'text-slate-300 hover:bg-slate-700'
            ]"
            @click="handleClick(item)"
          >
            <span v-if="item.icon" class="w-4 h-4 flex-shrink-0" :class="item.iconClass" v-html="item.icon" />
            <span class="truncate">{{ item.label }}</span>
            <span v-if="item.shortcut" class="ml-auto text-xs text-slate-500">{{ item.shortcut }}</span>
          </button>
        </template>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick, onUnmounted } from 'vue';

export interface MenuItem {
  label: string;
  icon?: string;
  iconClass?: string;
  action?: string;
  disabled?: boolean;
  danger?: boolean;
  divider?: boolean;
  shortcut?: string;
}

const props = defineProps<{
  visible: boolean;
  items: MenuItem[];
  x: number;
  y: number;
}>();

const emit = defineEmits<{
  close: [];
  select: [action: string];
}>();

const menuRef = ref<HTMLElement | null>(null);

const menuStyle = computed(() => {
  return {
    left: `${props.x}px`,
    top: `${props.y}px`,
  };
});

const handleClick = (item: MenuItem) => {
  if (item.disabled) return;
  if (item.action) {
    emit('select', item.action);
  }
  close();
};

const close = () => {
  emit('close');
};

const handleKeyDown = (e: KeyboardEvent) => {
  if (e.key === 'Escape') {
    close();
  }
};

watch(() => props.visible, (val) => {
  if (val) {
    nextTick(() => {
      document.addEventListener('keydown', handleKeyDown);
      // Adjust position if menu overflows viewport
      if (menuRef.value) {
        const rect = menuRef.value.getBoundingClientRect();
        if (rect.right > window.innerWidth) {
          menuRef.value.style.left = `${props.x - rect.width}px`;
        }
        if (rect.bottom > window.innerHeight) {
          menuRef.value.style.top = `${props.y - rect.height}px`;
        }
      }
    });
  } else {
    document.removeEventListener('keydown', handleKeyDown);
  }
});

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeyDown);
});
</script>
