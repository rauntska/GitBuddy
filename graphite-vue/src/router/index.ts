import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import Dashboard from '../views/Dashboard.vue';
import PRDetail from '../views/PRDetail.vue';
import AuthCallback from '../views/AuthCallback.vue';
import AccessDenied from '../views/AccessDenied.vue';
import AdminPanel from '../views/AdminPanel.vue';

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
      path: '/admin',
      name: 'admin',
      component: AdminPanel,
      meta: { requiresAuth: true, requiresAdmin: true },
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
