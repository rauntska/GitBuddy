<template>
  <div class="rich-text-editor">
    <TiptapEditor
      ref="tiptapRef"
      :model-value="modelValue"
      @update:model-value="$emit('update:modelValue', $event)"
      :placeholder="placeholder"
      :disabled="disabled"
      :show-toolbar="showToolbar"
      :min-height="minHeight"
      :pr-id="prId"
      @save="$emit('save')"
      @cancel="$emit('cancel')"
    />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import TiptapEditor from './TiptapEditor.vue';

const props = withDefaults(defineProps<{
  modelValue: string;
  placeholder?: string;
  disabled?: boolean;
  showToolbar?: boolean;
  minHeight?: number;
  prId?: number;
}>(), {
  placeholder: 'Write a comment...',
  disabled: false,
  showToolbar: true,
  minHeight: 120,
});

defineEmits<{
  'update:modelValue': [value: string];
  'save': [];
  'cancel': [];
}>();

const tiptapRef = ref<InstanceType<typeof TiptapEditor> | null>(null);

const focus = () => {
  tiptapRef.value?.focus();
};

defineExpose({ focus });
</script>
