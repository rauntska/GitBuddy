<template>
  <nav class="flex items-center gap-2 text-sm" aria-label="Breadcrumb">
    <template v-for="(item, index) in items" :key="index">
      <router-link
        v-if="item.to && index !== items.length - 1"
        :to="item.to"
        class="flex items-center gap-2 text-slate-400 hover:text-slate-200 transition-colors"
      >
        <span>{{ item.label }}</span>
      </router-link>
      <span
        v-else
        :class="[
          'truncate',
          index === items.length - 1 ? 'text-slate-100 font-medium' : 'text-slate-400'
        ]"
      >
        {{ item.label }}
      </span>
      <svg
        v-if="index < items.length - 1"
        class="w-4 h-4 text-slate-600 flex-shrink-0"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
      </svg>
    </template>
  </nav>
</template>

<script setup lang="ts">
export interface BreadcrumbItem {
  label: string;
  to?: string;
}

defineProps<{
  items: BreadcrumbItem[];
}>();
</script>
