<template>
  <Teleport to="body">
    <div
      v-if="show && filteredUsers.length > 0"
      ref="dropdownRef"
      class="mention-dropdown fixed bg-slate-800 border border-slate-600 rounded-lg shadow-xl z-50 max-h-64 overflow-auto min-w-[200px]"
      :style="dropdownStyle"
    >
      <ul class="py-1">
        <li
          v-for="(user, index) in filteredUsers"
          :key="user.username"
          @click="selectUser(user)"
          @mouseenter="selectedIndex = index"
          :class="[
            'flex items-center gap-2 px-3 py-2 cursor-pointer transition-colors',
            selectedIndex === index ? 'bg-slate-700' : 'hover:bg-slate-700/50'
          ]"
        >
          <img
            v-if="user.avatarUrl"
            :src="user.avatarUrl"
            :alt="user.username"
            class="w-6 h-6 rounded-full"
          />
          <div v-else class="w-6 h-6 rounded-full bg-slate-600 flex items-center justify-center text-xs text-slate-300">
            {{ user.username.charAt(0).toUpperCase() }}
          </div>
          <div class="flex-1 min-w-0">
            <div class="text-sm font-medium text-slate-200 truncate">{{ user.username }}</div>
            <div v-if="user.name" class="text-xs text-slate-400 truncate">{{ user.name }}</div>
          </div>
        </li>
      </ul>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue';

interface MentionableUser {
  username: string;
  avatarUrl?: string;
  name?: string;
}

const props = defineProps<{
  textarea: HTMLTextAreaElement | null;
  users: MentionableUser[];
}>();

const emit = defineEmits<{
  insertMention: [username: string, start: number, end: number];
}>();

const show = ref(false);
const dropdownRef = ref<HTMLElement | null>(null);
const selectedIndex = ref(0);
const dropdownStyle = ref<Record<string, string>>({});
const query = ref('');
const mentionStart = ref(0);

const filteredUsers = computed(() => {
  if (!query.value) return props.users.slice(0, 10);

  const q = query.value.toLowerCase();
  return props.users
    .filter(user =>
      user.username.toLowerCase().includes(q) ||
      user.name?.toLowerCase().includes(q)
    )
    .slice(0, 10);
});

const handleInput = (event: Event) => {
  const textarea = event.target as HTMLTextAreaElement;
  const cursorPos = textarea.selectionStart;
  const text = textarea.value;

  // Look for @ mentions
  let atPos = -1;
  for (let i = cursorPos - 1; i >= 0; i--) {
    const char = text[i];
    if (char === '@') {
      // Check if this is a valid mention start (not part of an email)
      const prevChar = i > 0 ? text[i - 1]! : '';
      if (i === 0 || /\s/.test(prevChar)) {
        atPos = i;
        break;
      }
    } else if (char && /\s/.test(char)) {
      // Stop at whitespace
      break;
    }
  }

  if (atPos !== -1 && atPos < cursorPos) {
    query.value = text.substring(atPos + 1, cursorPos);
    mentionStart.value = atPos;
    show.value = true;
    selectedIndex.value = 0;
    updateDropdownPosition();
  } else {
    show.value = false;
  }
};

const handleKeydown = (event: KeyboardEvent) => {
  if (!show.value || filteredUsers.value.length === 0) return;

  switch (event.key) {
    case 'ArrowDown':
      event.preventDefault();
      selectedIndex.value = (selectedIndex.value + 1) % filteredUsers.value.length;
      scrollToSelected();
      break;
    case 'ArrowUp':
      event.preventDefault();
      selectedIndex.value = (selectedIndex.value - 1 + filteredUsers.value.length) % filteredUsers.value.length;
      scrollToSelected();
      break;
    case 'Enter':
    case 'Tab':
      event.preventDefault();
      {
        const selectedUser = filteredUsers.value[selectedIndex.value];
        if (selectedUser) {
          selectUser(selectedUser);
        }
      }
      break;
    case 'Escape':
      event.preventDefault();
      show.value = false;
      break;
  }
};

const selectUser = (user: MentionableUser) => {
  if (!props.textarea) return;

  const cursorPos = props.textarea.selectionEnd;
  emit('insertMention', user.username, mentionStart.value, cursorPos);
  show.value = false;
};

const updateDropdownPosition = async () => {
  if (!props.textarea) return;

  await nextTick();

  // Get cursor position in the textarea
  const rect = props.textarea.getBoundingClientRect();
  const text = props.textarea.value.substring(0, mentionStart.value);

  // Approximate position based on line count
  const lines = text.split('\n');
  const computedStyle = getComputedStyle(props.textarea);
  const lineHeight = parseInt(computedStyle.lineHeight) || 20;
  const paddingTop = parseInt(computedStyle.paddingTop) || 0;
  const paddingLeft = parseInt(computedStyle.paddingLeft) || 0;

  const lastLineLength = lines[lines.length - 1]?.length ?? 0;
  const top = rect.top + paddingTop + (lines.length * lineHeight);
  const left = rect.left + paddingLeft + ((lastLineLength % 80) * 8);

  dropdownStyle.value = {
    top: `${Math.min(top, window.innerHeight - 280)}px`,
    left: `${Math.min(left, window.innerWidth - 220)}px`
  };
};

const scrollToSelected = () => {
  nextTick(() => {
    const dropdown = dropdownRef.value;
    const selected = dropdown?.querySelector('.bg-slate-700');
    if (selected) {
      selected.scrollIntoView({ block: 'nearest' });
    }
  });
};

onMounted(() => {
  if (props.textarea) {
    props.textarea.addEventListener('input', handleInput);
    props.textarea.addEventListener('keydown', handleKeydown);
  }
});

onUnmounted(() => {
  if (props.textarea) {
    props.textarea.removeEventListener('input', handleInput);
    props.textarea.removeEventListener('keydown', handleKeydown);
  }
});

// Watch for textarea changes
watch(() => props.textarea, (newTextarea, oldTextarea) => {
  if (oldTextarea) {
    oldTextarea.removeEventListener('input', handleInput);
    oldTextarea.removeEventListener('keydown', handleKeydown);
  }
  if (newTextarea) {
    newTextarea.addEventListener('input', handleInput);
    newTextarea.addEventListener('keydown', handleKeydown);
  }
});
</script>

<style scoped>
.mention-dropdown {
  @apply backdrop-blur-sm;
}
</style>
