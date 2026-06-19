<template>
  <span v-html="html" />
</template>

<script setup lang="ts">
import { toRef } from 'vue';
import { useProxiedHtml } from '../composables/useProxiedHtml';

const props = defineProps<{
  body: string;
}>();

const { html } = useProxiedHtml(toRef(props, 'body'), { isMarkdown: true });
</script>
