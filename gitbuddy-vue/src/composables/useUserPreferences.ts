import { ref } from 'vue';
import type { MarkdownDiffMode, UserPreferences } from '../types';
import { apiService } from '../services/api';

const preferences = ref<UserPreferences>({
  diffViewMode: 'unified',
  markdownDiffMode: 'rendered',
  showContext: true,
  fileTreeWidth: 256,
  commentsPanelWidth: 320,
  fileTreeVisible: true,
  listViewMode: 'comfortable',
  showColumnHeaders: true,
  pinnedPrIds: [],
  dashboardGroupOrder: [],
  hiddenDashboardGroups: [],
  hiddenRepositories: [],
  prioritySort: true,
});

// Preferences the backend stores as JSON strings.
const JSON_FIELDS = ['notificationPreferences', 'pinnedPrIds', 'dashboardGroupOrder', 'hiddenDashboardGroups', 'hiddenRepositories'] as const;

function deserializeJsonFields(prefs: any) {
  for (const field of JSON_FIELDS) {
    if (prefs[field] && typeof prefs[field] === 'string') {
      try {
        prefs[field] = JSON.parse(prefs[field]);
      } catch {
        prefs[field] = undefined;
      }
    }
  }
  return prefs;
}

const loaded = ref(false);
const loading = ref(false);

export function useUserPreferences() {
  const loadPreferences = async () => {
    if (loaded.value) return;

    loading.value = true;
    try {
      const prefs = await apiService.getUserPreferences();
      deserializeJsonFields(prefs);
      preferences.value = prefs;
      loaded.value = true;
    } catch (error) {
      console.error('Failed to load user preferences:', error);
    } finally {
      loading.value = false;
    }
  };

  const updatePreference = async (key: keyof UserPreferences, value: any) => {
    // Optimistic update
    const oldValue = preferences.value[key];
    (preferences.value as any)[key] = value;

    try {
      const payload: any = { [key]: value };
      if ((JSON_FIELDS as readonly string[]).includes(key)) {
        payload[key] = JSON.stringify(value);
      }
      const updated = await apiService.updateUserPreferences(payload);
      deserializeJsonFields(updated);
      preferences.value = { ...preferences.value, ...updated };
    } catch (error) {
      // Revert on error
      (preferences.value as any)[key] = oldValue;
      console.error('Failed to update preference:', error);
      throw error;
    }
  };

  const setDiffViewMode = async (mode: 'split' | 'unified') => {
    await updatePreference('diffViewMode', mode);
  };

  const setMarkdownDiffMode = async (mode: MarkdownDiffMode) => {
    await updatePreference('markdownDiffMode', mode);
  };

  const setShowContext = async (show: boolean) => {
    await updatePreference('showContext', show);
  };

  const setFileTreeWidth = async (width: number) => {
    await updatePreference('fileTreeWidth', width);
  };

  const setCommentsPanelWidth = async (width: number) => {
    await updatePreference('commentsPanelWidth', width);
  };

  const setFileTreeVisible = async (visible: boolean) => {
    await updatePreference('fileTreeVisible', visible);
  };

  const setListViewMode = async (mode: 'compact' | 'comfortable' | 'expanded') => {
    await updatePreference('listViewMode', mode);
  };

  const setShowColumnHeaders = async (show: boolean) => {
    await updatePreference('showColumnHeaders', show);
  };

  const setPrioritySort = async (enabled: boolean) => {
    await updatePreference('prioritySort', enabled);
  };

  const togglePinnedPr = async (prId: number) => {
    const current = preferences.value.pinnedPrIds ?? [];
    const updated = current.includes(prId) ? current.filter(id => id !== prId) : [...current, prId];
    await updatePreference('pinnedPrIds', updated);
  };

  const isPrPinned = (prId: number) => {
    return (preferences.value.pinnedPrIds ?? []).includes(prId);
  };

  const setDashboardGroupOrder = async (order: string[]) => {
    await updatePreference('dashboardGroupOrder', order);
  };

  const setHiddenDashboardGroups = async (groups: string[]) => {
    await updatePreference('hiddenDashboardGroups', groups);
  };

  const setHiddenRepositories = async (repos: string[]) => {
    await updatePreference('hiddenRepositories', repos);
  };

  // PullRequest.repository is the bare repo name while the preference stores owner/repo,
  // so compare on the lower-cased bare name.
  const bareRepoName = (name: string) => (name.split('/').pop() ?? name).toLowerCase();

  const isRepositoryHidden = (repository: string) => {
    const hidden = preferences.value.hiddenRepositories ?? [];
    if (hidden.length === 0) return false;
    const bare = bareRepoName(repository);
    return hidden.some(name => bareRepoName(name) === bare);
  };

  const resetDashboardLayout = async () => {
    await updatePreferences({ dashboardGroupOrder: [], hiddenDashboardGroups: [] });
  };

  const updatePreferences = async (updates: Partial<UserPreferences>) => {
    // Optimistic update
    const oldValues: Partial<UserPreferences> = {};
    for (const key in updates) {
      (oldValues as any)[key] = (preferences.value as any)[key];
      (preferences.value as any)[key] = (updates as any)[key];
    }

    try {
      // Serialize JSON fields for backend
      const payload: any = { ...updates };
      for (const field of JSON_FIELDS) {
        if (updates[field]) payload[field] = JSON.stringify(updates[field]);
      }
      const updated = await apiService.updateUserPreferences(payload);
      deserializeJsonFields(updated);
      preferences.value = updated;
    } catch (error) {
      // Revert on error
      for (const key in oldValues) {
        (preferences.value as any)[key] = (oldValues as any)[key];
      }
      console.error('Failed to update preferences:', error);
      throw error;
    }
  };

  return {
    preferences,
    loading,
    loadPreferences,
    setDiffViewMode,
    setMarkdownDiffMode,
    setShowContext,
    setFileTreeWidth,
    setCommentsPanelWidth,
    setFileTreeVisible,
    setListViewMode,
    setShowColumnHeaders,
    setPrioritySort,
    togglePinnedPr,
    isPrPinned,
    setDashboardGroupOrder,
    setHiddenDashboardGroups,
    setHiddenRepositories,
    isRepositoryHidden,
    resetDashboardLayout,
    updatePreferences,
  };
}
