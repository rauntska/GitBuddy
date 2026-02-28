import { ref } from 'vue';
import { apiService } from '../services/api';
import type { Settings } from '../types';

export function useSettings() {
  const settings = ref<Settings>({
    organization: '',
    refreshIntervalMinutes: 5,
    appId: '',
    privateKey: '',
    installationId: '',
    useGitHubApp: true,
    deleteOldPRs: false,
  });
  const loading = ref(false);
  const error = ref<string | null>(null);

  const fetchSettings = async () => {
    loading.value = true;
    error.value = null;
    try {
      const data = await apiService.getSettings();
      settings.value = data;
    } catch (err) {
      error.value = 'Failed to fetch settings';
      console.error(err);
    } finally {
      loading.value = false;
    }
  };

  const saveSettings = async (settingsToSave?: Partial<Settings>) => {
    loading.value = true;
    error.value = null;
    try {
      const dataToSave = settingsToSave || settings.value;
      await apiService.saveSettings({
        organization: dataToSave.organization ?? '',
        refreshIntervalMinutes: dataToSave.refreshIntervalMinutes ?? 5,
        appId: dataToSave.appId ?? '',
        privateKey: dataToSave.privateKey ?? '',
        installationId: dataToSave.installationId ?? '',
        useGitHubApp: dataToSave.useGitHubApp ?? true,
        deleteOldPRs: dataToSave.deleteOldPRs ?? false,
      });
      if (settingsToSave) {
        settings.value = { ...settings.value, ...settingsToSave };
      }
      return true;
    } catch (err) {
      error.value = 'Failed to save settings';
      console.error(err);
      return false;
    } finally {
      loading.value = false;
    }
  };

  return {
    settings,
    loading,
    error,
    fetchSettings,
    saveSettings,
  };
}
