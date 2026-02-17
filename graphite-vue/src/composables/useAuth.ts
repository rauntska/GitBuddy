import { ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '../stores/auth';

export function useAuth() {
  const authStore = useAuthStore();
  const router = useRouter();
  const route = useRoute();

  const isLoading = ref(false);

  const login = (inviteToken?: string) => {
    isLoading.value = true;
    const baseUrl = 'http://localhost:5247/api/auth/github';
    window.location.href = inviteToken ? `${baseUrl}?invite=${inviteToken}` : baseUrl;
  };

  const handleCallback = async () => {
    const token = route.query.token as string;
    const username = route.query.username as string;
    const avatar = route.query.avatar ? decodeURIComponent(route.query.avatar as string) : undefined;
    const role = route.query.role as string | undefined;
    const error = route.query.error as string | undefined;

    if (error === 'not_invited') {
      await router.push('/access-denied?reason=not_invited');
      return;
    }

    if (token) {
      authStore.updateFromQuery(token, username, avatar, role);
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
    isAdmin: () => authStore.isAdmin,
    user: () => authStore.user,
  };
}
