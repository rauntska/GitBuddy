import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import Dashboard from '../views/Dashboard.vue';
import PRDetail from '../views/PRDetail.vue';
import AuthCallback from '../views/AuthCallback.vue';
import AccessDenied from '../views/AccessDenied.vue';
import SettingsPage from '../views/SettingsPage.vue';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'dashboard',
      component: Dashboard,
    },
    {
      path: '/auth/callback',
      name: 'auth-callback',
      component: AuthCallback,
    },
    {
      path: '/access-denied',
      name: 'access-denied',
      component: AccessDenied,
    },
    {
      path: '/pr/:id',
      name: 'pr-detail',
      component: PRDetail,
      props: (route) => ({ id: Number(route.params.id) }),
      meta: { requiresAuth: true },
    },
    {
      path: '/invite/:token',
      name: 'invite',
      redirect: (to) => {
        return { path: '/auth/callback', query: { invite: to.params.token } };
      },
    },
    {
      path: '/settings',
      name: 'settings',
      component: SettingsPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/settings/github-app',
      name: 'settings-github-app',
      component: SettingsPage,
      meta: { requiresAuth: true, requiresAdmin: true },
    },
    {
      path: '/settings/admin',
      name: 'settings-admin',
      component: SettingsPage,
      meta: { requiresAuth: true, requiresAdmin: true },
    },
    {
      path: '/admin',
      redirect: '/settings/admin',
    },
  ],
});

router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore();
  const requiresAuth = to.meta.requiresAuth;
  const requiresAdmin = to.meta.requiresAdmin;
  
  if (requiresAuth && !authStore.isAuthenticated) {
    next('/');
  } else if (requiresAdmin && !authStore.isAdmin) {
    next('/');
  } else {
    next();
  }
});

export default router;
