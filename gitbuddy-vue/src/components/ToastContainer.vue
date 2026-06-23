<script setup lang="ts">
import { useToast } from '../composables/useToast';
import { CheckCircleIcon, ExclamationCircleIcon, InformationCircleIcon, XMarkIcon } from '@heroicons/vue/24/outline';

const { toasts, remove } = useToast();

const colorMap = {
  success: 'bg-green-600/90 border-green-500/50 text-white',
  error: 'bg-red-600/90 border-red-500/50 text-white',
  info: 'bg-blue-600/90 border-blue-500/50 text-white',
};

const iconMap = {
  success: CheckCircleIcon,
  error: ExclamationCircleIcon,
  info: InformationCircleIcon,
};
</script>

<template>
  <div
    aria-live="polite"
    aria-atomic="true"
    class="fixed bottom-4 right-4 z-[100] flex flex-col gap-2 max-w-sm"
  >
    <TransitionGroup
      enter-active-class="transition-all duration-300 ease-out"
      enter-from-class="translate-x-full opacity-0"
      enter-to-class="translate-x-0 opacity-100"
      leave-active-class="transition-all duration-200 ease-in"
      leave-from-class="translate-x-0 opacity-100"
      leave-to-class="translate-x-full opacity-0"
    >
      <div
        v-for="toast in toasts"
        :key="toast.id"
        :class="['rounded-lg border px-4 py-3 shadow-lg flex items-start gap-3', colorMap[toast.type]]"
      >
        <component :is="iconMap[toast.type]" class="w-5 h-5 flex-shrink-0 mt-0.5" />
        <p class="text-sm flex-1">{{ toast.message }}</p>
        <button
          @click="remove(toast.id)"
          class="flex-shrink-0 p-0.5 rounded hover:bg-white/20 transition-colors"
          aria-label="Dismiss notification"
        >
          <XMarkIcon class="w-4 h-4" />
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>
