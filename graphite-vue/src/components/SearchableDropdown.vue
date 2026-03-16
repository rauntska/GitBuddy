<template>
  <div class="relative" ref="dropdownRef">
    <div class="relative">
      <input
        ref="inputRef"
        v-model="searchQuery"
        type="text"
        :placeholder="placeholder"
        :disabled="disabled"
        class="w-full px-3 py-2 pl-9 text-sm bg-slate-800 border border-slate-600 rounded-lg text-slate-200 placeholder-slate-500 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        @focus="handleFocus"
        @keydown="handleKeydown"
        @input="handleInput"
      />
      <svg
        class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
      </svg>
      <svg
        v-if="showDropdown"
        class="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
      </svg>
      <svg
        v-else
        class="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
      </svg>
    </div>

    <Teleport to="body">
      <div
        v-if="showDropdown && (filteredItems.length > 0 || loading)"
        class="fixed bg-slate-800 border border-slate-600 rounded-lg shadow-xl max-h-64 overflow-y-auto z-50"
        :style="dropdownStyle"
      >
        <div v-if="loading" class="flex justify-center py-4">
          <div class="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-500"></div>
        </div>
        
        <template v-else>
          <button
            v-for="(item, index) in filteredItems"
            :key="`${item.type}-${item.name}`"
            type="button"
            :class="[
              'w-full flex items-center gap-3 px-3 py-2 text-left transition-colors',
              index === selectedIndex ? 'bg-blue-600/30' : 'hover:bg-slate-700/50'
            ]"
            @click="selectItem(item)"
            @mouseenter="selectedIndex = index"
          >
            <div class="flex-shrink-0">
              <img
                v-if="item.type === 'User' && item.avatar"
                :src="item.avatar"
                :alt="item.name"
                class="w-6 h-6 rounded-full"
              />
              <div
                v-else-if="item.type === 'User'"
                class="w-6 h-6 rounded-full bg-slate-700 flex items-center justify-center"
              >
                <span class="text-xs text-slate-400">{{ item.name[0]?.toUpperCase() }}</span>
              </div>
              <div
                v-else
                class="w-6 h-6 rounded-full bg-purple-500/20 flex items-center justify-center"
              >
                <svg class="w-4 h-4 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
              </div>
            </div>
            <div class="flex-1 min-w-0">
              <div class="text-sm text-slate-200 truncate">{{ item.name }}</div>
            </div>
            <span
              :class="[
                'text-xs px-2 py-0.5 rounded',
                item.type === 'User' ? 'bg-blue-500/20 text-blue-400' : 'bg-purple-500/20 text-purple-400'
              ]"
            >
              {{ item.type }}
            </span>
          </button>
        </template>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue';
import type { PotentialReviewer } from '../types';

const props = withDefaults(defineProps<{
  items: PotentialReviewer[];
  placeholder?: string;
  disabled?: boolean;
  loading?: boolean;
  excludedNames?: string[];
}>(), {
  placeholder: 'Search...',
  disabled: false,
  loading: false,
  excludedNames: () => []
});

const emit = defineEmits<{
  select: [item: PotentialReviewer];
}>();

const dropdownRef = ref<HTMLElement | null>(null);
const inputRef = ref<HTMLInputElement | null>(null);
const searchQuery = ref('');
const showDropdown = ref(false);
const selectedIndex = ref(0);
const dropdownStyle = ref<Record<string, string>>({});

const filteredItems = computed(() => {
  const query = searchQuery.value.toLowerCase().trim();
  
  return props.items
    .filter(item => {
      if (props.excludedNames.includes(item.name)) return false;
      if (!query) return true;
      return item.name.toLowerCase().includes(query);
    })
    .slice(0, 20);
});

const updateDropdownPosition = () => {
  if (!inputRef.value) return;
  
  const rect = inputRef.value.getBoundingClientRect();
  dropdownStyle.value = {
    top: `${rect.bottom + 4}px`,
    left: `${rect.left}px`,
    width: `${rect.width}px`
  };
};

const handleFocus = () => {
  showDropdown.value = true;
  selectedIndex.value = 0;
  updateDropdownPosition();
};

const handleInput = () => {
  selectedIndex.value = 0;
  updateDropdownPosition();
};

const handleKeydown = (e: KeyboardEvent) => {
  if (!showDropdown.value) {
    if (e.key === 'ArrowDown' || e.key === 'Enter') {
      showDropdown.value = true;
      selectedIndex.value = 0;
      updateDropdownPosition();
    }
    return;
  }

  switch (e.key) {
    case 'ArrowDown':
      e.preventDefault();
      if (selectedIndex.value < filteredItems.value.length - 1) {
        selectedIndex.value++;
      }
      break;
    case 'ArrowUp':
      e.preventDefault();
      if (selectedIndex.value > 0) {
        selectedIndex.value--;
      }
      break;
    case 'Enter':
      e.preventDefault();
      if (filteredItems.value[selectedIndex.value]) {
        selectItem(filteredItems.value[selectedIndex.value]);
      }
      break;
    case 'Escape':
      e.preventDefault();
      showDropdown.value = false;
      break;
  }
};

const selectItem = (item: PotentialReviewer) => {
  emit('select', item);
  searchQuery.value = '';
  showDropdown.value = false;
};

const handleClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  if (dropdownRef.value && !dropdownRef.value.contains(target)) {
    const dropdown = document.querySelector('.fixed.bg-slate-800');
    if (!dropdown?.contains(target)) {
      showDropdown.value = false;
    }
  }
};

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
  window.addEventListener('resize', updateDropdownPosition);
  window.addEventListener('scroll', updateDropdownPosition, true);
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
  window.removeEventListener('resize', updateDropdownPosition);
  window.removeEventListener('scroll', updateDropdownPosition, true);
});

watch(showDropdown, async (newVal) => {
  if (newVal) {
    await nextTick();
    updateDropdownPosition();
  }
});

watch(() => props.items, () => {
  if (showDropdown.value) {
    updateDropdownPosition();
  }
});
</script>
