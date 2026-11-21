<template>
  <div ref="root" class="relative">
    <button
      type="button"
      class="w-full flex items-center justify-between gap-2 bg-slate-900 border border-slate-700 rounded px-2 py-1 text-sm text-slate-200 text-left"
      :disabled="disabled"
      @click="open = !open"
    >
      <span class="truncate">{{ summary }}</span>
      <ChevronUpDownIcon class="w-4 h-4 shrink-0 text-slate-400" />
    </button>

    <div v-if="selected.length > 0" class="flex flex-wrap gap-1 mt-1">
      <span
        v-for="name in selected"
        :key="name"
        class="inline-flex items-center gap-1 text-[11px] px-1.5 py-0.5 rounded border border-slate-700 bg-slate-800/60 text-slate-300"
      >
        {{ name }}
        <button type="button" class="text-slate-500 hover:text-red-400" :aria-label="`Remove ${name}`" @click="toggle(name)">×</button>
      </span>
    </div>

    <div v-if="open" class="absolute z-20 mt-1 w-full bg-slate-900 border border-slate-700 rounded shadow-lg">
      <div class="p-2 border-b border-slate-800">
        <input
          v-model="search"
          type="text"
          placeholder="Search repositories…"
          class="w-full bg-slate-950 border border-slate-700 rounded px-2 py-1 text-sm text-slate-200"
          @keydown.esc="open = false"
        />
      </div>

      <div class="max-h-56 overflow-y-auto py-1">
        <div v-if="loading" class="px-3 py-2 text-sm text-slate-400">Loading repositories…</div>
        <div v-else-if="filtered.length === 0" class="px-3 py-2 text-sm text-slate-500">No repositories found.</div>
        <label
          v-for="repo in filtered"
          :key="repo"
          class="flex items-center gap-2 px-3 py-1 text-sm text-slate-200 hover:bg-slate-800/60 cursor-pointer"
        >
          <input type="checkbox" :checked="selected.includes(repo)" @change="toggle(repo)" />
          <span class="truncate">{{ repo }}</span>
        </label>
      </div>

      <div class="flex justify-between px-3 py-2 border-t border-slate-800 text-xs">
        <button type="button" class="text-slate-400 hover:text-slate-200" @click="clear">All repositories</button>
        <button type="button" class="text-sky-400 hover:text-sky-300" @click="open = false">Done</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { ChevronUpDownIcon } from '@heroicons/vue/24/outline';

const props = withDefaults(defineProps<{
  /** Selected full names (owner/repo). Empty means the rule applies to all repositories. */
  modelValue: string[];
  /** Available full names. */
  options: string[];
  loading?: boolean;
  disabled?: boolean;
}>(), { loading: false, disabled: false });

const emit = defineEmits<{ (e: 'update:modelValue', value: string[]): void }>();

const open = ref(false);
const search = ref('');
const root = ref<HTMLElement | null>(null);

const selected = computed(() => props.modelValue);

// Keep already-selected names visible even when they are no longer returned by GitHub.
const allOptions = computed(() =>
  [...new Set([...props.options, ...props.modelValue])].sort((a, b) => a.localeCompare(b)));

const filtered = computed(() => {
  const q = search.value.trim().toLowerCase();
  return q ? allOptions.value.filter((r) => r.toLowerCase().includes(q)) : allOptions.value;
});

const summary = computed(() => {
  if (props.modelValue.length === 0) return 'All repositories';
  if (props.modelValue.length === 1) return props.modelValue[0];
  return `${props.modelValue.length} repositories selected`;
});

function toggle(name: string) {
  const next = props.modelValue.includes(name)
    ? props.modelValue.filter((r) => r !== name)
    : [...props.modelValue, name];
  emit('update:modelValue', next);
}

function clear() {
  emit('update:modelValue', []);
}

function onDocumentClick(event: MouseEvent) {
  if (open.value && root.value && !root.value.contains(event.target as Node)) open.value = false;
}

onMounted(() => document.addEventListener('mousedown', onDocumentClick));
onBeforeUnmount(() => document.removeEventListener('mousedown', onDocumentClick));
</script>
