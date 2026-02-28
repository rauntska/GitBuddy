<template>
  <nav class="w-56 bg-slate-850 border-r border-slate-800 p-4">
    <h2 class="text-lg font-semibold text-white mb-4">Settings</h2>
    <ul class="space-y-1">
      <li v-for="item in menuItems" :key="item.id">
        <router-link
          :to="item.path"
          class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors"
          :class="isActive(item.path) 
            ? 'bg-blue-600 text-white' 
            : 'text-slate-400 hover:bg-slate-800 hover:text-slate-200'"
        >
          <component :is="item.icon" class="w-5 h-5" />
          {{ item.label }}
        </router-link>
      </li>
    </ul>
  </nav>
</template>

<script setup lang="ts">
import { computed, h } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthStore } from '../../stores/auth';
import { 
  UserCircleIcon,
  Cog8ToothIcon,
  ShieldCheckIcon
} from '@heroicons/vue/24/outline';

const route = useRoute();
const authStore = useAuthStore();
const isAdmin = computed(() => authStore.isAdmin);

interface MenuItem {
  id: string;
  label: string;
  path: string;
  icon: any;
  adminOnly?: boolean;
}

const menuItems = computed<MenuItem[]>(() => {
  const items: MenuItem[] = [
    {
      id: 'user',
      label: 'User Settings',
      path: '/settings',
      icon: h(UserCircleIcon)
    },
    {
      id: 'github-app',
      label: 'GitHub App',
      path: '/settings/github-app',
      icon: h(Cog8ToothIcon),
      adminOnly: true
    },
    {
      id: 'admin',
      label: 'Administration',
      path: '/settings/admin',
      icon: h(ShieldCheckIcon),
      adminOnly: true
    }
  ];
  
  return items.filter(item => !item.adminOnly || isAdmin.value);
});

const isActive = (path: string): boolean => {
  if (path === '/settings') {
    return route.path === '/settings';
  }
  return route.path.startsWith(path);
};
</script>
