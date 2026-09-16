<template>
  <div class="min-h-screen bg-slate-900 text-slate-100">
    <main class="max-w-2xl mx-auto px-4 py-6">
      <h1 class="text-xl font-semibold text-white mb-1">What's new</h1>
      <p class="text-sm text-slate-500 mb-4">Everything GitBuddy has shipped, newest first.</p>

      <div v-if="loading" class="text-sm text-slate-500 py-8 text-center">Loading…</div>
      <div v-else-if="entries.length === 0" class="text-sm text-slate-500 py-8 text-center">
        Nothing published yet.
      </div>
      <div v-else>
        <ChangelogEntryCard v-for="entry in entries" :key="entry.slug" :entry="entry" />
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { useChangelog } from '../composables/useChangelog';
import ChangelogEntryCard from '../components/ChangelogEntryCard.vue';

const { entries, loading, loadChangelog, markSeen } = useChangelog();

onMounted(async () => {
  await loadChangelog();
  await markSeen();
});
</script>
