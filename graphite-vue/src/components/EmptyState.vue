<template>
  <div class="flex flex-col items-center justify-center py-20 px-4 text-center">
    <!-- Icon/Illustration -->
    <div class="mb-6" :class="iconColorClass">
      <component :is="iconComponent" />
    </div>

    <!-- Title -->
    <h3 class="text-xl font-semibold text-slate-200 mb-2">
      {{ title }}
    </h3>

    <!-- Description -->
    <p class="text-sm text-slate-400 mb-6 max-w-md">
      {{ description }}
    </p>

    <!-- Actions -->
    <div class="flex items-center gap-3">
      <button
        v-if="primaryAction"
        @click="$emit('primary-action')"
        class="px-5 py-2.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-medium transition-colors flex items-center gap-2"
      >
        <svg v-if="primaryAction.icon" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="primaryAction.icon" />
        </svg>
        {{ primaryAction.label }}
      </button>

      <button
        v-if="secondaryAction"
        @click="$emit('secondary-action')"
        class="px-5 py-2.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 font-medium transition-colors flex items-center gap-2"
      >
        <svg v-if="secondaryAction.icon" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="secondaryAction.icon" />
        </svg>
        {{ secondaryAction.label }}
      </button>
    </div>

    <!-- Help Link -->
    <a
      v-if="helpLink"
      :href="helpLink.url"
      target="_blank"
      rel="noopener noreferrer"
      class="mt-4 text-xs text-slate-500 hover:text-slate-400 transition-colors flex items-center gap-1"
    >
      {{ helpLink.label }}
      <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
      </svg>
    </a>
  </div>
</template>

<script setup lang="ts">
import { computed, h } from 'vue';

interface Action {
  label: string;
  icon?: string;
}

interface HelpLink {
  label: string;
  url: string;
}

const props = defineProps<{
  type?: 'no-config' | 'no-prs' | 'no-results' | 'error';
  title?: string;
  description?: string;
  primaryAction?: Action;
  secondaryAction?: Action;
  helpLink?: HelpLink;
}>();

defineEmits<{
  'primary-action': [];
  'secondary-action': [];
}>();

// Default content based on type
const defaultContent = {
  'no-config': {
    title: 'Welcome to Graphite!',
    description: 'Track pull requests across your GitHub organization. Configure your GitHub settings to get started.',
    icon: 'rocket',
    iconColor: 'text-blue-500',
  },
  'no-prs': {
    title: 'No Open Pull Requests',
    description: 'Your team is all caught up! There are no open pull requests at the moment.',
    icon: 'celebration',
    iconColor: 'text-green-500',
  },
  'no-results': {
    title: 'No Pull Requests Found',
    description: 'No PRs match your current filters. Try adjusting your search criteria or clearing filters.',
    icon: 'search',
    iconColor: 'text-slate-500',
  },
  error: {
    title: 'Something Went Wrong',
    description: 'Failed to load pull requests. This might be due to an invalid token or network issues.',
    icon: 'error',
    iconColor: 'text-red-500',
  },
};

const content = computed(() => defaultContent[props.type || 'no-prs']);

const iconColorClass = computed(() => content.value.iconColor);

const iconComponent = computed(() => {
  const iconType = content.value.icon;
  
  // SVG icon components
  const icons = {
    rocket: () => h('svg', {
      class: 'w-20 h-20 opacity-50',
      fill: 'none',
      stroke: 'currentColor',
      viewBox: '0 0 24 24',
    }, [
      h('path', {
        'stroke-linecap': 'round',
        'stroke-linejoin': 'round',
        'stroke-width': '1.5',
        d: 'M13 10V3L4 14h7v7l9-11h-7z',
      }),
    ]),
    
    celebration: () => h('svg', {
      class: 'w-20 h-20 opacity-50',
      fill: 'none',
      stroke: 'currentColor',
      viewBox: '0 0 24 24',
    }, [
      h('path', {
        'stroke-linecap': 'round',
        'stroke-linejoin': 'round',
        'stroke-width': '1.5',
        d: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z',
      }),
    ]),
    
    search: () => h('svg', {
      class: 'w-20 h-20 opacity-50',
      fill: 'none',
      stroke: 'currentColor',
      viewBox: '0 0 24 24',
    }, [
      h('path', {
        'stroke-linecap': 'round',
        'stroke-linejoin': 'round',
        'stroke-width': '1.5',
        d: 'M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z',
      }),
    ]),
    
    error: () => h('svg', {
      class: 'w-20 h-20 opacity-50',
      fill: 'none',
      stroke: 'currentColor',
      viewBox: '0 0 24 24',
    }, [
      h('path', {
        'stroke-linecap': 'round',
        'stroke-linejoin': 'round',
        'stroke-width': '1.5',
        d: 'M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
      }),
    ]),
  };
  
  return icons[iconType as keyof typeof icons] || icons.search;
});
</script>
