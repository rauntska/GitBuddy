import { ref } from 'vue';
import { apiService } from '../services/api';

const hasPersonalAccessToken = ref<boolean | null>(null);
const loading = ref(false);
const loaded = ref(false);

export function useUserSettings() {
  const fetchUserSettings = async (force = false) => {
    if (loaded.value && !force) return;
    
    loading.value = true;
    try {
      const settings = await apiService.getUserSettings();
      hasPersonalAccessToken.value = settings.hasPersonalAccessToken;
      loaded.value = true;
    } catch (error) {
      console.error('Failed to fetch user settings:', error);
    } finally {
      loading.value = false;
    }
  };

  return {
    hasPersonalAccessToken,
    loading,
    loaded,
    fetchUserSettings,
  };
}
