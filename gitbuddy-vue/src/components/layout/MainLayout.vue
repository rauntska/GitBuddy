<template>
  <div class="flex min-h-screen bg-slate-900">
    <a href="#main-content" class="sr-only focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:z-50 focus:px-4 focus:py-2 focus:bg-blue-600 focus:text-white focus:rounded-lg focus:text-sm focus:font-medium">Skip to main content</a>
    <AppSidebar />
    <div class="flex-1 flex flex-col pl-14">
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
import AppSidebar from './AppSidebar.vue';
</script>
