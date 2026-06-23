import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { apiService } from '../services/api';
import type { UserRole } from '../types';

interface User {
  id: number;
  username: string;
  email: string;
  avatarUrl?: string;
  role: UserRole;
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'));
  const user = ref<User | null>(JSON.parse(localStorage.getItem('user') || 'null'));

  const isAuthenticated = computed(() => !!token.value);
  const username = computed(() => user.value?.username || '');
  const avatarUrl = computed(() => user.value?.avatarUrl || '');
  const role = computed(() => user.value?.role || 'Developer');
  const isAdmin = computed(() => user.value?.role === 'Admin');

  function setToken(newToken: string) {
    token.value = newToken;
    localStorage.setItem('token', newToken);
  }

  function setUser(newUser: User) {
    user.value = newUser;
    localStorage.setItem('user', JSON.stringify(newUser));
  }

  function clearAuth() {
    token.value = null;
    user.value = null;
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  }

  function updateFromQuery(queryToken: string, queryUsername: string, queryAvatar?: string, queryRole?: string) {
    setToken(queryToken);
    setUser({
      id: 0,
      username: queryUsername,
      email: '',
      avatarUrl: queryAvatar || undefined,
      role: (queryRole as UserRole) || 'Developer',
    });
  }

  function updateUser(userData: User) {
    user.value = userData;
    localStorage.setItem('user', JSON.stringify(userData));
  }

  async function refreshUserData() {
    if (!token.value) return;
    try {
      const userData = await apiService.getCurrentUser();
      setUser(userData);
    } catch {
      // Token might be invalid, clear auth
    }
  }

  return {
    token,
    user,
    isAuthenticated,
    username,
    avatarUrl,
    role,
    isAdmin,
    setToken,
    setUser,
    clearAuth,
    updateFromQuery,
    updateUser,
    refreshUserData,
  };
});
