<template>
  <div class="access-denied">
    <div class="access-denied-content">
      <div class="icon">
        <svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="12" cy="12" r="10"/>
          <line x1="4.93" y1="4.93" x2="19.07" y2="19.07"/>
        </svg>
      </div>
      <h1>Access Denied</h1>
      <p class="message">{{ message }}</p>
      <p class="description">{{ description }}</p>
      <button @click="goHome" class="btn-primary">Go to Home</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';

const route = useRoute();
const router = useRouter();

const reason = computed(() => route.query.reason as string | undefined);

const message = computed(() => {
  switch (reason.value) {
    case 'not_invited':
      return 'You need an invitation to access this application.';
    default:
      return 'You do not have permission to access this page.';
  }
});

const description = computed(() => {
  switch (reason.value) {
    case 'not_invited':
      return 'Please contact an administrator to request access or use a valid invitation link.';
    default:
      return 'If you believe this is an error, please contact your administrator.';
  }
});

function goHome() {
  router.push('/');
}
</script>

<style scoped>
.access-denied {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background-color: var(--bg-secondary, #f6f8fa);
}

.access-denied-content {
  text-align: center;
  padding: 3rem;
  background: var(--bg-primary, #ffffff);
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  max-width: 480px;
}

.icon {
  color: var(--text-secondary, #656d76);
  margin-bottom: 1.5rem;
}

h1 {
  font-size: 1.5rem;
  font-weight: 600;
  color: var(--text-primary, #24292f);
  margin-bottom: 1rem;
}

.message {
  font-size: 1.1rem;
  color: var(--text-primary, #24292f);
  margin-bottom: 0.5rem;
}

.description {
  font-size: 0.9rem;
  color: var(--text-secondary, #656d76);
  margin-bottom: 2rem;
}

.btn-primary {
  background-color: var(--accent-primary, #2da44e);
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  font-size: 1rem;
  border-radius: 6px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-primary:hover {
  background-color: var(--accent-primary-hover, #2c974b);
}
</style>
