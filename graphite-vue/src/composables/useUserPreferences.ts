import { ref } from 'vue';
import type { UserPreferences } from '../types';
import { apiService } from '../services/api';

const preferences = ref<UserPreferences>({
  diffViewMode: 'unified',
  showContext: true,
  fileTreeWidth: 256,
  commentsPanelWidth: 320,
  fileTreeVisible: true,
  listViewMode: 'comfortable',
  pinnedPrIds: [],
  dashboardGroupOrder: [],
  hiddenDashboardGroups: [],
});

const loaded = ref(false);
const loading = ref(false);

export function useUserPreferences() {
  const loadPreferences = async () => {
    if (loaded.value) return;

    loading.value = true;
    try {
      const prefs = await apiService.getUserPreferences();
      // Deserialize JSON-serialized fields
      if ((prefs as any).notificationPreferences && typeof (prefs as any).notificationPreferences === 'string') {
        (prefs as any).notificationPreferences = JSON.parse((prefs as any).notificationPreferences);
      }
      if ((prefs as any).pinnedPrIds && typeof (prefs as any).pinnedPrIds === 'string') {
        (prefs as any).pinnedPrIds = JSON.parse((prefs as any).pinnedPrIds);
      }
      if ((prefs as any).dashboardGroupOrder && typeof (prefs as any).dashboardGroupOrder === 'string') {
        (prefs as any).dashboardGroupOrder = JSON.parse((prefs as any).dashboardGroupOrder);
      }
      if ((prefs as any).hiddenDashboardGroups && typeof (prefs as any).hiddenDashboardGroups === 'string') {
        (prefs as any).hiddenDashboardGroups = JSON.parse((prefs as any).hiddenDashboardGroups);
      }
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
      if (key === 'notificationPreferences' || key === 'pinnedPrIds' || key === 'dashboardGroupOrder' || key === 'hiddenDashboardGroups') {
        payload[key] = JSON.stringify(value);
      }
      const updated = await apiService.updateUserPreferences(payload);
      if ((updated as any).notificationPreferences && typeof (updated as any).notificationPreferences === 'string') {
        (updated as any).notificationPreferences = JSON.parse((updated as any).notificationPreferences);
      }
      if ((updated as any).pinnedPrIds && typeof (updated as any).pinnedPrIds === 'string') {
        (updated as any).pinnedPrIds = JSON.parse((updated as any).pinnedPrIds);
      }
      if ((updated as any).dashboardGroupOrder && typeof (updated as any).dashboardGroupOrder === 'string') {
        (updated as any).dashboardGroupOrder = JSON.parse((updated as any).dashboardGroupOrder);
      }
      if ((updated as any).hiddenDashboardGroups && typeof (updated as any).hiddenDashboardGroups === 'string') {
        (updated as any).hiddenDashboardGroups = JSON.parse((updated as any).hiddenDashboardGroups);
      }
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

  const setListViewMode = async (mode: 'compact' | 'comfortable' | 'expanded') => {
    await updatePreference('listViewMode', mode);
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
      if (updates.notificationPreferences) {
        payload.notificationPreferences = JSON.stringify(updates.notificationPreferences);
      }
      if (updates.pinnedPrIds) {
        payload.pinnedPrIds = JSON.stringify(updates.pinnedPrIds);
      }
      if (updates.dashboardGroupOrder) {
        payload.dashboardGroupOrder = JSON.stringify(updates.dashboardGroupOrder);
      }
      if (updates.hiddenDashboardGroups) {
        payload.hiddenDashboardGroups = JSON.stringify(updates.hiddenDashboardGroups);
      }
      const updated = await apiService.updateUserPreferences(payload);
      // Deserialize JSON fields from backend
      if ((updated as any).notificationPreferences && typeof (updated as any).notificationPreferences === 'string') {
        (updated as any).notificationPreferences = JSON.parse((updated as any).notificationPreferences);
      }
      if ((updated as any).pinnedPrIds && typeof (updated as any).pinnedPrIds === 'string') {
        (updated as any).pinnedPrIds = JSON.parse((updated as any).pinnedPrIds);
      }
      if ((updated as any).dashboardGroupOrder && typeof (updated as any).dashboardGroupOrder === 'string') {
        (updated as any).dashboardGroupOrder = JSON.parse((updated as any).dashboardGroupOrder);
      }
      if ((updated as any).hiddenDashboardGroups && typeof (updated as any).hiddenDashboardGroups === 'string') {
        (updated as any).hiddenDashboardGroups = JSON.parse((updated as any).hiddenDashboardGroups);
      }
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
    togglePinnedPr,
    isPrPinned,
    setDashboardGroupOrder,
    setHiddenDashboardGroups,
    resetDashboardLayout,
    updatePreferences,
  };
}
