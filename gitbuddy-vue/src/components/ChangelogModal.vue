<template>
  <Teleport to="body">
    <Transition name="modal">
      <div
        v-if="isOpen"
        class="fixed inset-0 z-50 overflow-y-auto"
        @keydown.esc="handleDismiss"
      >
        <div class="flex min-h-screen items-start justify-center p-4 pt-16">
          <div class="fixed inset-0 bg-black/70" @click="handleDismiss" />

          <div class="relative bg-slate-900 rounded-xl shadow-2xl w-full max-w-lg max-h-[80vh] overflow-hidden flex flex-col border border-slate-700">
            <div class="px-5 py-4 border-b border-slate-800">
              <h2 class="text-base font-semibold text-white">What's new</h2>
              <p class="text-xs text-slate-500 mt-0.5">
                {{ unseenEntries.length }} update{{ unseenEntries.length === 1 ? '' : 's' }} since your last visit
              </p>
            </div>

            <div class="flex-1 overflow-y-auto px-5">
              <ChangelogEntryCard v-for="entry in unseenEntries" :key="entry.slug" :entry="entry" />
            </div>

            <div class="px-5 py-3 border-t border-slate-800 flex justify-end">
              <button
                @click="handleDismiss"
                class="px-4 py-2 rounded-lg bg-slate-200 hover:bg-white text-slate-900 text-sm font-medium transition-colors"
              >
                Got it
              </button>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useChangelog } from '../composables/useChangelog';
import ChangelogEntryCard from './ChangelogEntryCard.vue';

const { unseenEntries, markSeen } = useChangelog();

const isOpen = computed(() => unseenEntries.value.length > 0);

const handleDismiss = async () => {
  await markSeen();
};
</script>

<style scoped>
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}

.modal-enter-active .relative,
.modal-leave-active .relative {
  transition: transform 0.2s ease, opacity 0.2s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-from .relative,
.modal-leave-to .relative {
  transform: scale(0.95);
  opacity: 0;
}
</style>

