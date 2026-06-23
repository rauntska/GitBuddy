<template>
  <nav class="w-56 border-r border-slate-800 p-4">
    <h2 class="text-sm font-semibold uppercase tracking-wider text-slate-300 mb-3">Settings</h2>
    <ul class="space-y-1">
      <li v-for="item in menuItems" :key="item.id">
        <router-link
          :to="item.path"
          class="flex items-center gap-2 px-2 py-1.5 rounded text-sm transition-all duration-150 ease-out border"
          :class="isActive(item.path)
            ? 'bg-slate-800/60 border-slate-700 text-slate-200'
            : 'border-transparent text-slate-300 hover:bg-slate-800/40 hover:border-slate-700'"
        >
          <component :is="item.icon" class="w-4 h-4 shrink-0" />
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
  ShieldCheckIcon,
  ChartBarIcon
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
    },
    {
      id: 'analytics',
      label: 'Analytics',
      path: '/settings/analytics',
      icon: h(ChartBarIcon),
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
