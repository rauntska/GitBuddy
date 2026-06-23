<template>
  <div class="flex items-center border border-slate-700/50 bg-slate-800/50 rounded-lg p-2 gap-4">
    <!-- Repository & PR Number -->
    <div class="flex-shrink-0 w-[100px] sm:w-[140px] space-y-1.5">
      <div class="h-4 bg-slate-700/50 rounded shimmer" :style="{ width: '80%' }"></div>
      <div class="h-3 bg-slate-700/50 rounded shimmer" :style="{ width: '45%' }"></div>
    </div>

    <!-- Author (hidden on small screens) -->
    <div class="hidden md:flex-shrink-0 md:block w-[120px]">
      <div class="h-4 bg-slate-700/50 rounded shimmer" :style="{ width: '65%' }"></div>
    </div>

    <!-- Title -->
    <div class="flex-1 min-w-0">
      <div class="h-4 bg-slate-700/50 rounded shimmer" :style="{ width: randomWidth }"></div>
    </div>

    <!-- Metadata -->
    <div class="flex items-center flex-shrink-0 gap-2 sm:gap-3">
      <!-- Size Badge -->
      <div class="w-[60px] flex justify-center">
        <div class="h-4 w-8 bg-slate-700/50 rounded shimmer"></div>
      </div>

      <!-- CI Badge -->
      <div class="w-[40px] flex justify-center">
        <div class="h-4 w-4 bg-slate-700/50 rounded-full shimmer"></div>
      </div>

      <!-- Merge Ready Badge -->
      <div class="w-[70px] flex justify-center">
        <div class="h-4 w-10 bg-slate-700/50 rounded shimmer"></div>
      </div>

      <!-- Stale Spacer -->
      <div class="hidden md:flex w-[45px]"></div>

      <!-- Reviewer Avatars -->
      <div class="hidden lg:flex items-center -space-x-1 w-[80px]">
        <div class="w-5 h-5 rounded-full bg-slate-700/50 shimmer"></div>
        <div class="w-5 h-5 rounded-full bg-slate-700/50 shimmer"></div>
        <div class="w-5 h-5 rounded-full bg-slate-700/50 shimmer"></div>
      </div>

      <!-- Comments -->
      <div class="hidden md:flex w-[50px] justify-center">
        <div class="h-4 w-6 bg-slate-700/50 rounded shimmer"></div>
      </div>

      <!-- Files Changed -->
      <div class="hidden md:flex w-[45px] justify-center">
        <div class="h-4 w-5 bg-slate-700/50 rounded shimmer"></div>
      </div>

      <!-- Line Changes -->
      <div class="hidden sm:flex w-[90px] justify-center">
        <div class="flex items-center gap-1">
          <div class="h-3 w-6 bg-slate-700/50 rounded shimmer"></div>
          <div class="h-3 w-1 bg-slate-700/50 rounded"></div>
          <div class="h-3 w-6 bg-slate-700/50 rounded shimmer"></div>
        </div>
      </div>

      <!-- Time -->
      <div class="hidden md:block w-[50px]">
        <div class="h-3 bg-slate-700/50 rounded shimmer" :style="{ width: '60%' }"></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const randomWidth = computed(() => {
  const widths = ['55%', '65%', '70%', '75%', '80%'];
  return widths[Math.floor(Math.random() * widths.length)];
});
</script>

<style scoped>
.shimmer {
  background: linear-gradient(
    to right,
    transparent 0%,
    rgba(148, 163, 184, 0.1) 50%,
    transparent 100%
  );
  background-size: 1000px 100%;
  animation: shimmer 2s infinite linear;
}

@keyframes shimmer {
  0% {
    background-position: -1000px 0;
  }
  100% {
    background-position: 1000px 0;
  }
}
</style>
