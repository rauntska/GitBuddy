<template>
  <div class="flex min-h-screen bg-slate-900">
    <AppSidebar />
    <div class="flex-1 flex flex-col pl-14">
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
import AppSidebar from './AppSidebar.vue';
</script>
