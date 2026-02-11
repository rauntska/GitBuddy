<template>
   <div 
     :class="['flex items-center', badgeClass]"
     :title="tooltip"
   >
      <!-- Success Icon -->
      <svg v-if="status === 'SUCCESS'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" :class="['w-4 h-4 flex-shrink-0', iconColor]">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
      </svg>

      <!-- Failure Icon -->
      <svg v-else-if="status === 'FAILURE'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" :class="['w-4 h-4 flex-shrink-0', iconColor]">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
      </svg>

      <!-- Pending Icon -->
      <svg v-else-if="status === 'PENDING'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" :class="['w-4 h-4 flex-shrink-0', iconColor]">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clip-rule="evenodd" />
      </svg>

      <!-- No Checks Icon (GitHub Logo) -->
      <svg v-else xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" :class="['w-4 h-4 flex-shrink-0', iconColor]">
        <path d="M10 2C5.58 2 2 6.58 2 11c0 4.42 3.58 8 8 8 4.42 0 8-3.58 8-8 0-4.42-3.58-8-8-8zm3.5 11.5c-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5-.17.31-.48.5-.83.5-.31 0-.54-.23-.83-.5z" />
      </svg>

      <span v-if="!compact && status" class="ml-1.5 text-xs font-medium" :class="iconColor">{{ statusLabel }}</span>
   </div>
 </template>

 <script setup lang="ts">
 import { computed } from 'vue';

 const props = withDefaults(defineProps<{
   status?: 'SUCCESS' | 'FAILURE' | 'PENDING' | 'NEUTRAL' | null;
   totalCount?: number;
   showCount?: boolean;
   compact?: boolean;
 }>(), {
   status: null,
   totalCount: 0,
   showCount: false,
   compact: false
 });

 const iconColor = computed(() => {
   switch (props.status) {
     case 'SUCCESS':
       return 'text-emerald-400';
     case 'FAILURE':
       return 'text-red-400';
     case 'PENDING':
       return 'text-amber-400';
     default:
       return 'text-slate-400';
   }
 });

  const badgeClass = computed(() => {
    return '';
  });

 const statusLabel = computed(() => {
   switch (props.status) {
     case 'SUCCESS':
       return props.compact ? 'PASS' : 'SUCCESS';
     case 'FAILURE':
       return props.compact ? 'FAIL' : 'FAILED';
     case 'PENDING':
       return 'PENDING';
     default:
       return 'NONE';
   }
 });

 const tooltip = computed(() => {
   if (!props.status) {
     return 'No CI/CD checks configured';
   }
   const count = props.totalCount > 1 ? ` (${props.totalCount} checks)` : '';
   return `${statusLabel.value}${count}`;
 });
 </script>