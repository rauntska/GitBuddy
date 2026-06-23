import { ref } from 'vue';

export interface Toast {
  id: number;
  type: 'success' | 'error' | 'info';
  message: string;
  duration?: number;
}

const toasts = ref<Toast[]>([]);
let nextId = 0;

function addToast(type: Toast['type'], message: string, duration = 4000) {
  const id = nextId++;
  toasts.value.push({ id, type, message, duration });
  if (duration > 0) {
    setTimeout(() => removeToast(id), duration);
  }
}

function removeToast(id: number) {
  toasts.value = toasts.value.filter(t => t.id !== id);
}

export function useToast() {
  return {
    toasts,
    success: (message: string, duration?: number) => addToast('success', message, duration),
    error: (message: string, duration?: number) => addToast('error', message, duration ?? 6000),
    info: (message: string, duration?: number) => addToast('info', message, duration),
    remove: removeToast,
  };
}
