<template>
  <div class="comment-reactions flex items-center gap-1 flex-wrap">
    <!-- Existing Reactions -->
    <button
      v-for="group in sortedGroups"
      :key="group.reaction"
      @click="toggleReaction(group)"
      :class="[
        'flex items-center gap-1 px-2 py-1 rounded-full text-xs transition-colors',
        group.hasReacted
          ? 'bg-blue-900/50 text-blue-300 border border-blue-700'
          : 'bg-slate-700/50 text-slate-300 hover:bg-slate-700 border border-slate-600'
      ]"
      :title="group.usernames.join(', ')"
    >
      <span>{{ group.emoji }}</span>
      <span>{{ group.count }}</span>
    </button>

    <!-- Add Reaction Button -->
    <div class="relative">
      <button
        @click="showPicker = !showPicker"
        class="p-1 rounded hover:bg-slate-700 text-slate-400 hover:text-slate-200 transition-colors"
        title="Add reaction"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.828 14.828a4 4 0 01-5.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
        </svg>
      </button>

      <!-- Emoji Picker -->
      <Transition name="fade">
        <div
          v-if="showPicker"
          class="absolute left-0 bottom-full mb-2 bg-slate-800 border border-slate-600 rounded-lg shadow-xl p-2 z-10"
        >
          <div class="grid grid-cols-4 gap-1">
            <button
              v-for="emoji in availableReactions"
              :key="emoji.name"
              @click="addReaction(emoji.name)"
              class="w-8 h-8 flex items-center justify-center rounded hover:bg-slate-700 text-lg transition-transform hover:scale-125"
              :title="emoji.name"
            >
              {{ emoji.emoji }}
            </button>
          </div>
        </div>
      </Transition>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';

interface ReactionGroup {
  reaction: string;
  emoji: string;
  count: number;
  usernames: string[];
  hasReacted: boolean;
}

const props = defineProps<{
  commentId: number;
  reactions: ReactionGroup[];
}>();

const emit = defineEmits<{
  addReaction: [commentId: number, reaction: string];
  removeReaction: [commentId: number, reaction: string];
}>();

const showPicker = ref(false);

const availableReactions = [
  { name: 'thumbsup', emoji: '👍' },
  { name: 'thumbsdown', emoji: '👎' },
  { name: 'laugh', emoji: '😄' },
  { name: 'hooray', emoji: '🎉' },
  { name: 'confused', emoji: '😕' },
  { name: 'heart', emoji: '❤️' },
  { name: 'rocket', emoji: '🚀' },
  { name: 'eyes', emoji: '👀' }
];

const emojiMap = Object.fromEntries(availableReactions.map(r => [r.name, r.emoji]));

const sortedGroups = computed(() => {
  return [...props.reactions]
    .filter(g => g.count > 0)
    .map(g => ({
      ...g,
      emoji: emojiMap[g.reaction] || '👍'
    }))
    .sort((a, b) => b.count - a.count);
});

const toggleReaction = (group: ReactionGroup) => {
  if (group.hasReacted) {
    emit('removeReaction', props.commentId, group.reaction);
  } else {
    emit('addReaction', props.commentId, group.reaction);
  }
};

const addReaction = (reaction: string) => {
  emit('addReaction', props.commentId, reaction);
  showPicker.value = false;
};

// Close picker when clicking outside
const handleClickOutside = (event: MouseEvent) => {
  const target = event.target as HTMLElement;
  if (!target.closest('.comment-reactions')) {
    showPicker.value = false;
  }
};

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
});
</script>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.15s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
