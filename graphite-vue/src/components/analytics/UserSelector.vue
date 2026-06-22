<template>
  <div ref="root" class="relative border border-slate-800 rounded p-2">
    <div class="flex flex-wrap items-center gap-2">
      <div class="inline-flex items-center gap-1.5 text-[11px] uppercase tracking-wider text-slate-500">
        <UserIcon class="w-3.5 h-3.5" />
        <span>Authors</span>
      </div>

      <button
        type="button"
        @click="open = !open"
        :disabled="loading"
        class="inline-flex items-center gap-2 px-2.5 py-1 rounded bg-slate-900/60 border border-slate-700 hover:border-slate-600 text-slate-200 text-sm transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
      >
        <span v-if="selected.length === 0" class="text-slate-300">All users</span>
        <span v-else class="font-mono tabular-nums">{{ selected.length }} selected</span>
        <ChevronDownIcon :class="['w-3.5 h-3.5 text-slate-400 transition-transform', open && 'rotate-180']" />
      </button>

      <div v-if="selected.length > 0" class="flex flex-wrap items-center gap-1.5">
        <span
          v-for="username in selected"
          :key="username"
          class="inline-flex items-center gap-1 pl-0.5 pr-1 py-0.5 rounded bg-slate-800/60 border border-slate-700 text-xs text-slate-200"
        >
          <img
            v-if="avatarFor(username)"
            :src="avatarFor(username)!"
            :alt="username"
            class="w-4 h-4 rounded-full"
          />
          <span v-else class="w-4 h-4 rounded-full bg-slate-700 inline-flex items-center justify-center">
            <UserIcon class="w-2.5 h-2.5 text-slate-400" />
          </span>
          <span class="max-w-[140px] truncate">{{ username }}</span>
          <button
            type="button"
            @click="toggle(username)"
            class="text-slate-500 hover:text-slate-300 transition-colors"
            :aria-label="`Remove ${username}`"
          >
            <XMarkIcon class="w-3 h-3" />
          </button>
        </span>

        <button
          type="button"
          @click="clear"
          class="text-[11px] uppercase tracking-wider text-slate-500 hover:text-slate-300 transition-colors px-1"
        >
          Clear
        </button>
      </div>
    </div>

    <transition
      enter-active-class="transition duration-150 ease-out"
      enter-from-class="opacity-0 -translate-y-1"
      enter-to-class="opacity-100 translate-y-0"
      leave-active-class="transition duration-100 ease-in"
      leave-from-class="opacity-100 translate-y-0"
      leave-to-class="opacity-0 -translate-y-1"
    >
      <div
        v-if="open"
        class="absolute z-30 mt-2 w-full min-w-[260px] max-h-[340px] flex flex-col bg-slate-900 border border-slate-700 rounded shadow-lg"
      >
        <div class="p-2 border-b border-slate-800">
          <div class="relative">
            <MagnifyingGlassIcon class="w-4 h-4 text-slate-500 absolute left-2 top-1/2 -translate-y-1/2" />
            <input
              ref="searchInput"
              v-model="query"
              type="text"
              placeholder="Search authors…"
              class="w-full pl-7 pr-2 py-1.5 bg-slate-900/60 border border-slate-700 rounded text-slate-200 text-sm focus:outline-none focus:border-slate-600 transition-colors"
              @keydown.escape.prevent="open = false"
            />
          </div>
        </div>

        <div class="overflow-y-auto py-1">
          <button
            v-for="opt in filtered"
            :key="opt.username"
            type="button"
            @click="toggle(opt.username)"
            class="w-full flex items-center gap-2 px-3 py-1.5 text-left text-sm hover:bg-slate-800/60 transition-colors"
          >
            <CheckIcon
              v-if="selected.includes(opt.username)"
              class="w-4 h-4 text-slate-200 flex-shrink-0"
            />
            <span v-else class="w-4 h-4 flex-shrink-0"></span>
            <img
              v-if="opt.avatarUrl"
              :src="opt.avatarUrl"
              :alt="opt.username"
              class="w-5 h-5 rounded-full"
            />
            <span v-else class="w-5 h-5 rounded-full bg-slate-700 inline-flex items-center justify-center">
              <UserIcon class="w-3 h-3 text-slate-400" />
            </span>
            <span class="truncate text-slate-200">{{ opt.username }}</span>
          </button>
          <div v-if="filtered.length === 0" class="px-3 py-3 text-sm text-slate-500">
            No authors in this range.
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue';
import {
  ChevronDownIcon,
  CheckIcon,
  MagnifyingGlassIcon,
  UserIcon,
  XMarkIcon,
} from '@heroicons/vue/20/solid';
import type { AuthorOption } from '../../types';

const props = defineProps<{
  available: AuthorOption[];
  selected: string[];
  loading?: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:selected', next: string[]): void;
}>();

const open = ref(false);
const query = ref('');
const root = ref<HTMLElement | null>(null);
const searchInput = ref<HTMLInputElement | null>(null);

const filtered = computed<AuthorOption[]>(() => {
  const q = query.value.trim().toLowerCase();
  if (!q) return props.available;
  return props.available.filter((a) => a.username.toLowerCase().includes(q));
});

function avatarFor(username: string): string | null | undefined {
  return props.available.find((a) => a.username === username)?.avatarUrl;
}

function toggle(username: string) {
  const next = props.selected.includes(username)
    ? props.selected.filter((u) => u !== username)
    : [...props.selected, username];
  emit('update:selected', next);
}

function clear() {
  emit('update:selected', []);
}

function onDocClick(e: MouseEvent) {
  if (!root.value) return;
  if (!root.value.contains(e.target as Node)) open.value = false;
}

watch(open, (isOpen) => {
  if (isOpen) {
    document.addEventListener('mousedown', onDocClick);
    nextTick(() => searchInput.value?.focus());
  } else {
    document.removeEventListener('mousedown', onDocClick);
    query.value = '';
  }
});

onBeforeUnmount(() => {
  document.removeEventListener('mousedown', onDocClick);
});
</script>
