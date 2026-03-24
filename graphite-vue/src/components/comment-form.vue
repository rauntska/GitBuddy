<template>
  <div class="p-4 border-t border-slate-700/20">
    <RichTextEditor
      :model-value="modelValue"
      @update:model-value="$emit('update:modelValue', $event)"
      ref="editorRef"
      :placeholder="placeholder"
      :min-height="minHeight"
      @save="$emit('submit')"
      @cancel="$emit('cancel')"
    />
    <div v-if="error" class="mt-2 text-xs text-rose-400">{{ error }}</div>
    <div class="flex gap-2 justify-end mt-3">
      <button
        @click="$emit('cancel')"
        class="px-4 py-2 bg-slate-700/50 hover:bg-slate-700 rounded-lg text-xs text-slate-200 transition-all duration-200 border border-slate-600/50 hover:border-slate-500/50"
      >
        {{ cancelText }}
      </button>
      <button
        @click="$emit('submit')"
        :disabled="!modelValue?.trim()"
        class="px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-lg text-xs text-white disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200 shadow-lg shadow-blue-500/20 hover:shadow-blue-500/30"
      >
        {{ submitText }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import RichTextEditor from './RichTextEditor.vue';

const props = withDefaults(defineProps<{
  modelValue: string;
  placeholder?: string;
  error?: string;
  minHeight?: number;
  submitText?: string;
  cancelText?: string;
}>(), {
  placeholder: 'Add your comment...',
  minHeight: 100,
  submitText: 'Add Comment',
  cancelText: 'Cancel',
});

defineEmits<{
  'update:modelValue': [value: string];
  'submit': [];
  'cancel': [];
}>();

const editorRef = ref<InstanceType<typeof RichTextEditor> | null>(null);

const focus = () => {
  editorRef.value?.focus();
};

defineExpose({ focus });
</script>
