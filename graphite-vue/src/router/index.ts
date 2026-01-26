import { createRouter, createWebHistory } from 'vue-router';
import Dashboard from '../views/Dashboard.vue';
import PRDetail from '../views/PRDetail.vue';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'dashboard',
      component: Dashboard,
    },
    {
      path: '/pr/:id',
      name: 'pr-detail',
      component: PRDetail,
      props: (route) => ({ id: Number(route.params.id) }),
    },
  ],
});

export default router;
