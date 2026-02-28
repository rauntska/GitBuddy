<template>
  <aside class="w-14 bg-slate-900 border-r border-slate-800 flex flex-col items-center py-4 fixed left-0 top-0 h-screen z-30">
    <router-link
      to="/"
      class="mb-6 p-2 rounded-lg transition-colors"
      :class="isActiveRoute('/') ? 'bg-blue-600 text-white' : 'text-slate-400 hover:bg-slate-800 hover:text-slate-200'"
      title="Dashboard"
    >
      <HomeIcon class="w-6 h-6" />
    </router-link>

    <div class="flex-1 flex flex-col items-center gap-2">
      <router-link
        to="/settings"
        class="p-2 rounded-lg transition-colors"
        :class="isActiveRoute('/settings') ? 'bg-blue-600 text-white' : 'text-slate-400 hover:bg-slate-800 hover:text-slate-200'"
        title="Settings"
      >
        <Cog6ToothIcon class="w-6 h-6" />
      </router-link>

      <router-link
        v-if="isAdmin"
        to="/settings/admin"
        class="p-2 rounded-lg transition-colors"
        :class="isActiveRoute('/settings/admin') ? 'bg-blue-600 text-white' : 'text-slate-400 hover:bg-slate-800 hover:text-slate-200'"
        title="Administration"
      >
        <ShieldCheckIcon class="w-6 h-6" />
      </router-link>
    </div>

    <div class="mt-auto flex flex-col items-center gap-2">
      <button
        v-if="isAuthenticated"
        @click="handleLogout"
        class="p-2 rounded-lg text-slate-400 hover:bg-slate-800 hover:text-slate-200 transition-colors"
        title="Logout"
      >
        <ArrowRightOnRectangleIcon class="w-6 h-6" />
      </button>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '../../stores/auth';
import { 
  HomeIcon, 
  Cog6ToothIcon, 
  ShieldCheckIcon,
  ArrowRightOnRectangleIcon 
} from '@heroicons/vue/24/outline';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const isAuthenticated = computed(() => authStore.isAuthenticated);
const isAdmin = computed(() => authStore.isAdmin);

const isActiveRoute = (path: string): boolean => {
  if (path === '/') {
    return route.path === '/';
  }
  return route.path.startsWith(path);
};

const handleLogout = async () => {
  authStore.clearAuth();
  await router.push('/');
};
</script>
