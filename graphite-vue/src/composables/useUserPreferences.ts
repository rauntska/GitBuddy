import { ref } from 'vue';
import type { UserPreferences } from '../types';
import { apiService } from '../services/api';

const preferences = ref<UserPreferences>({
  diffViewMode: 'unified',
  showContext: true,
  fileTreeWidth: 256,
  commentsPanelWidth: 320,
  fileTreeVisible: true,
  listViewMode: 'normal',
  keyboardShortcuts: {
    toggleComments: 'c',
    toggleFileTree: 'f',
    nextFile: 'j',
    previousFile: 'k',
    nextComment: 'n',
    previousComment: 'p',
  },
});

const loaded = ref(false);
const loading = ref(false);

export function useUserPreferences() {
  const loadPreferences = async () => {
    if (loaded.value) return;
    
    loading.value = true;
    try {
      const prefs = await apiService.getUserPreferences();
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
      const updated = await apiService.updateUserPreferences({ [key]: value });
      // Merge the updated preferences, ensuring our value is preserved
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

  const setListViewMode = async (mode: 'compact' | 'normal') => {
    await updatePreference('listViewMode', mode);
  };

  const setKeyboardShortcut = async (key: keyof UserPreferences['keyboardShortcuts'], value: string) => {
    const oldShortcuts = preferences.value.keyboardShortcuts;
    preferences.value.keyboardShortcuts = { ...preferences.value.keyboardShortcuts, [key]: value };

    try {
      await apiService.updateUserPreferences({ keyboardShortcuts: preferences.value.keyboardShortcuts });
    } catch (error) {
      // Revert on error
      preferences.value.keyboardShortcuts = oldShortcuts;
      console.error('Failed to update keyboard shortcut:', error);
      throw error;
    }
  };

  const updatePreferences = async (updates: Partial<UserPreferences>) => {
    // Optimistic update
    const oldValues: Partial<UserPreferences> = {};
    for (const key in updates) {
      (oldValues as any)[key] = (preferences.value as any)[key];
      (preferences.value as any)[key] = (updates as any)[key];
    }

    try {
      const updated = await apiService.updateUserPreferences(updates);
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
    setShowContext,
    setFileTreeWidth,
    setCommentsPanelWidth,
    setFileTreeVisible,
    setListViewMode,
    setKeyboardShortcut,
    updatePreferences,
  };
}
