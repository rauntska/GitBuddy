<template>
  <div class="flex items-center gap-3 p-3 rounded-lg border border-slate-700/50 bg-slate-800/50 animate-pulse">
    <!-- Status Badge Skeleton -->
    <div class="flex-shrink-0 w-24 h-6 bg-slate-700/50 rounded-full shimmer"></div>

    <!-- Repository Info Skeleton -->
    <div class="flex-shrink-0 min-w-[120px] space-y-2">
      <div class="h-4 bg-slate-700/50 rounded w-full shimmer"></div>
      <div class="h-3 bg-slate-700/50 rounded w-16 shimmer"></div>
    </div>

    <!-- Author Skeleton -->
    <div class="flex-shrink-0 min-w-[100px]">
      <div class="h-4 bg-slate-700/50 rounded w-20 shimmer"></div>
    </div>

    <!-- Title Skeleton -->
    <div class="flex-1 min-w-0">
      <div class="h-4 bg-slate-700/50 rounded w-3/4 shimmer"></div>
    </div>

    <!-- Reviewer Avatars Skeleton -->
    <div class="flex items-center gap-2 flex-shrink-0">
      <div class="w-7 h-7 rounded-full bg-slate-700/50 shimmer"></div>
      <div class="w-7 h-7 rounded-full bg-slate-700/50 shimmer"></div>
      <div class="w-7 h-7 rounded-full bg-slate-700/50 shimmer"></div>
    </div>

    <!-- Comment Badge Skeleton -->
    <div class="flex-shrink-0 w-5 h-5 bg-slate-700/50 rounded shimmer"></div>

    <!-- Line Changes Skeleton -->
    <div class="flex items-center gap-1.5 px-2 py-1 rounded flex-shrink-0">
      <div class="h-3 bg-slate-700/50 rounded w-10 shimmer"></div>
      <div class="h-3 bg-slate-700/50 rounded w-10 shimmer"></div>
    </div>

    <!-- Time Skeleton -->
    <div class="flex-shrink-0 min-w-[50px]">
      <div class="h-3 bg-slate-700/50 rounded w-full shimmer"></div>
    </div>
  </div>
</template>

<script setup lang="ts">
// No props needed for skeleton loader
</script>

<style scoped>
@keyframes shimmer {
  0% {
    background-position: -1000px 0;
  }
  100% {
    background-position: 1000px 0;
  }
}

.shimmer {
  animation: shimmer 2s infinite linear;
  background: linear-gradient(
    to right,
    transparent 0%,
    rgba(148, 163, 184, 0.1) 50%,
    transparent 100%
  );
  background-size: 1000px 100%;
}
</style>
