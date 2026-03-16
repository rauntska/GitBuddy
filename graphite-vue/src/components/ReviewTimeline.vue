<template>
  <div class="space-y-3">
    <div class="flex items-center justify-between">
      <h3 class="text-sm font-medium text-slate-300">Review Timeline</h3>
      <button
        v-if="!expanded"
        @click="expanded = true"
        class="text-xs text-blue-400 hover:text-blue-300"
      >
        Show all
      </button>
      <button
        v-else
        @click="expanded = false"
        class="text-xs text-blue-400 hover:text-blue-300"
      >
        Show less
      </button>
    </div>

    <div v-if="loading" class="flex justify-center py-4">
      <div class="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-500"></div>
    </div>

    <div v-else-if="events.length === 0" class="text-sm text-slate-500 py-2">
      No review activity yet
    </div>

    <div v-else class="relative">
      <div class="absolute left-4 top-0 bottom-0 w-px bg-slate-700"></div>
      
      <div class="space-y-3">
        <div
          v-for="event in displayedEvents"
          :key="event.id"
          class="relative pl-10"
        >
          <div
            :class="[
              'absolute left-2 top-1 w-4 h-4 rounded-full border-2 border-slate-800 flex items-center justify-center',
              getEventBgColor(event.type, event.state)
            ]"
          >
            <component :is="getEventIcon(event.type, event.state)" class="w-2.5 h-2.5" />
          </div>
          
          <div class="bg-slate-800/50 rounded p-2.5 hover:bg-slate-800 transition-colors">
            <div class="flex items-start gap-2">
              <img
                v-if="event.actorAvatar"
                :src="event.actorAvatar"
                :alt="event.actor"
                class="w-5 h-5 rounded-full flex-shrink-0"
              />
              <div v-else class="w-5 h-5 rounded-full bg-slate-700 flex items-center justify-center flex-shrink-0">
                <span class="text-[10px] text-slate-400">{{ event.actor[0]?.toUpperCase() }}</span>
              </div>
              <div class="flex-1 min-w-0">
                <div class="text-sm text-slate-200">
                  <span class="font-medium">{{ event.actor }}</span>
                  <span class="text-slate-400 ml-1">{{ event.summary }}</span>
                </div>
                <div class="flex items-center gap-2 mt-1">
                  <span class="text-xs text-slate-500">{{ formatTimestamp(event.timestamp) }}</span>
                  <span
                    v-if="event.state && event.type === 'REVIEW_SUBMITTED'"
                    :class="['text-xs px-1.5 py-0.5 rounded', getStateBadgeClass(event.state)]"
                  >
                    {{ event.state }}
                  </span>
                </div>
                <div v-if="event.filePath" class="mt-1">
                  <span class="text-xs text-slate-500 font-mono truncate block">{{ event.filePath }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, h } from 'vue';
import { apiService } from '../services/api';
import type { ReviewEvent } from '../types';

const props = defineProps<{
  pullRequestId: number;
}>();

const emit = defineEmits<{
  error: [message: string];
}>();

const events = ref<ReviewEvent[]>([]);
const loading = ref(true);
const expanded = ref(false);

const maxDisplay = 5;

const displayedEvents = computed(() => {
  const sorted = [...events.value].sort((a, b) => 
    new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()
  );
  return expanded.value ? sorted : sorted.slice(0, maxDisplay);
});

const getEventBgColor = (type: string, state?: string): string => {
  if (type === 'REVIEW_SUBMITTED') {
    if (state === 'APPROVED') return 'bg-green-500/20';
    if (state === 'CHANGES_REQUESTED') return 'bg-orange-500/20';
    return 'bg-blue-500/20';
  }
  if (type === 'THREAD_RESOLVED') return 'bg-green-500/20';
  return 'bg-slate-700';
};

const getStateBadgeClass = (state: string): string => {
  const classes: Record<string, string> = {
    APPROVED: 'bg-green-500/20 text-green-400',
    CHANGES_REQUESTED: 'bg-orange-500/20 text-orange-400',
    COMMENTED: 'bg-blue-500/20 text-blue-400',
  };
  return classes[state] || 'bg-slate-700 text-slate-400';
};

const getEventIcon = (type: string, state?: string) => {
  if (type === 'REVIEW_SUBMITTED') {
    if (state === 'APPROVED') {
      return {
        render() {
          return h('svg', { class: 'text-green-400', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
            h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '3', d: 'M5 13l4 4L19 7' })
          ]);
        }
      };
    }
    if (state === 'CHANGES_REQUESTED') {
      return {
        render() {
          return h('svg', { class: 'text-orange-400', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
            h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '3', d: 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z' })
          ]);
        }
      };
    }
    return {
      render() {
        return h('svg', { class: 'text-blue-400', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
          h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z' })
        ]);
      }
    };
  }
  if (type === 'THREAD_RESOLVED') {
    return {
      render() {
        return h('svg', { class: 'text-green-400', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
          h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '3', d: 'M5 13l4 4L19 7' })
        ]);
      }
    };
  }
  return {
    render() {
      return h('svg', { class: 'text-slate-400', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
        h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z' })
      ]);
    }
  };
};

const formatTimestamp = (timestamp: string): string => {
  const date = new Date(timestamp);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / (1000 * 60));
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffMins < 1) return 'Just now';
  if (diffMins < 60) return `${diffMins}m ago`;
  if (diffHours < 24) return `${diffHours}h ago`;
  if (diffDays < 7) return `${diffDays}d ago`;
  
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
};

const fetchTimeline = async () => {
  try {
    loading.value = true;
    const response = await apiService.getReviewTimeline(props.pullRequestId);
    events.value = response.events || [];
  } catch (err) {
    emit('error', 'Failed to load review timeline');
  } finally {
    loading.value = false;
  }
};

onMounted(fetchTimeline);

watch(() => props.pullRequestId, fetchTimeline);
</script>
