import { ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '../stores/auth';

export function useAuth() {
  const authStore = useAuthStore();
  const router = useRouter();
  const route = useRoute();

  const isLoading = ref(false);

  const login = () => {
    isLoading.value = true;
    window.location.href = 'http://localhost:5247/api/auth/github';
  };

  const handleCallback = async () => {
    const token = route.query.token as string;
    const username = route.query.username as string;
    const avatar = route.query.avatar as string;

    if (token) {
      authStore.updateFromQuery(token, username, avatar);
      await router.push('/');
    } else {
      await router.push('/');
    }
  };

  const logout = async () => {
    authStore.clearAuth();
    await router.push('/');
  };

  const checkAuth = () => {
    if (!authStore.isAuthenticated) {
      return false;
    }
    return true;
  };

  return {
    isLoading,
    login,
    handleCallback,
    logout,
    checkAuth,
    isAuthenticated: () => authStore.isAuthenticated,
    user: () => authStore.user,
  };
}
