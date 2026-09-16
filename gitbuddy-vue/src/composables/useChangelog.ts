import { ref, computed } from 'vue';
import type { ChangelogEntry } from '../types';
import { apiService } from '../services/api';

const entries = ref<ChangelogEntry[]>([]);
const lastSeenChangelogAt = ref<string | null>(null);
const loaded = ref(false);
const loading = ref(false);

export function useChangelog() {
  const loadChangelog = async () => {
    if (loaded.value) return;

    loading.value = true;
    try {
      const response = await apiService.getChangelog();
      entries.value = response.entries;
      lastSeenChangelogAt.value = response.lastSeenChangelogAt;
      loaded.value = true;
    } catch (error) {
      console.error('Failed to load changelog:', error);
    } finally {
      loading.value = false;
    }
  };

  const unseenEntries = computed(() => {
    if (!lastSeenChangelogAt.value) return [];
    const lastSeen = new Date(lastSeenChangelogAt.value).getTime();
    return entries.value.filter(e => new Date(e.createdAt).getTime() > lastSeen);
  });

  const markSeen = async () => {
    try {
      const response = await apiService.markChangelogSeen();
      lastSeenChangelogAt.value = response.lastSeenChangelogAt;
    } catch (error) {
      console.error('Failed to mark changelog seen:', error);
    }
  };

  return {
    entries,
    lastSeenChangelogAt,
    loading,
    loadChangelog,
    unseenEntries,
    markSeen,
  };
}
