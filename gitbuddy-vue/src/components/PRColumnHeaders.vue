<template>
  <div
    v-if="density !== 'expanded'"
    :class="[
      'flex items-center gap-4 font-mono uppercase tracking-wider text-slate-500 select-none',
      compact ? 'text-[10px] px-2' : 'text-[11px] px-3',
    ]"
  >
    <!-- Repo + PR Number -->
    <div
      :class="['flex-shrink-0 truncate', compact ? 'w-[80px] sm:w-[100px]' : 'w-[100px] sm:w-[140px]']"
      title="Repository and PR number"
    >Repo</div>

    <!-- Author -->
    <div
      :class="['hidden md:flex flex-shrink-0 truncate', compact ? 'w-[80px]' : 'w-[120px]']"
      title="PR author"
    >Author</div>

    <!-- Title (empty spacer, flex-1 pushes metadata to the right edge) -->
    <div class="flex-1 min-w-0"></div>

    <!-- Metadata section — mirrors PRRow's metadata wrapper (gap-1 sm:gap-2 md:gap-3) -->
    <div class="flex items-center flex-shrink-0 justify-end gap-1 sm:gap-2 md:gap-3">
      <!-- Priority -->
      <div
        :class="['flex justify-center', compact ? 'w-[24px]' : 'w-[32px]']"
        title="Priority — Low / Normal / High / Urgent"
      >Prio</div>

      <!-- Size -->
      <div
        :class="['flex justify-center', compact ? 'w-[36px]' : 'w-[44px]']"
        title="Lines added / removed"
      >Size</div>

      <!-- CI -->
      <div
        :class="['flex justify-center', compact ? 'w-[24px]' : 'w-[32px]']"
        title="Continuous integration check status"
      >CI</div>

      <!-- Merge readiness -->
      <div
        :class="['flex justify-center', compact ? 'w-[50px]' : 'w-[70px]']"
        title="Merge readiness — approvals, unresolved threads, block reasons"
      >Merge</div>

      <!-- Reviewers -->
      <div
        :class="['hidden lg:flex justify-center', compact ? 'w-[60px]' : 'w-[80px]']"
        title="Reviewers who have left a review"
      >Reviews</div>

      <!-- Threads (resolved / total) -->
      <div
        :class="['hidden md:flex justify-center', compact ? 'w-[36px]' : 'w-[44px]']"
        title="Resolved / total review threads"
      >Threads</div>

      <!-- Files changed -->
      <div
        :class="['hidden md:flex justify-center', compact ? 'w-[30px]' : 'w-[40px]']"
        title="Files changed"
      >Files</div>

      <!-- +/- lines -->
      <div
        :class="['hidden sm:flex justify-end gap-1', compact ? 'w-[70px]' : 'w-[90px]']"
        title="Lines added / removed"
      >+/-</div>

      <!-- Stale (compact only) -->
      <div
        v-if="compact"
        class="hidden md:flex w-[16px] justify-center"
        title="PRs created more than 24 hours ago"
      >⚑</div>

      <!-- Last updated -->
      <div
        :class="['hidden md:block text-right', compact ? 'w-[36px]' : 'w-[44px]']"
        title="Time since last update"
      >Updated</div>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  compact?: boolean;
  density?: 'compact' | 'comfortable' | 'expanded';
}>();
</script>
