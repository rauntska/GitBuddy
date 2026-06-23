import { ref } from 'vue';
import { apiService } from '../services/api';
import { useToast } from './useToast';

export function useImageUpload() {
  const isUploading = ref(false);
  const pendingImage = ref<File | null>(null);
  const toast = useToast();

  const uploadImage = async (prId: number, file: File): Promise<string> => {
    isUploading.value = true;
    try {
      const url = await apiService.uploadImage(prId, file);
      return url;
    } catch (error: any) {
      const message = error?.response?.data?.message || error?.message || 'Failed to upload image.';
      toast.error(message);
      throw error;
    } finally {
      isUploading.value = false;
    }
  };

  const extractImageFile = (event: ClipboardEvent | DragEvent): File | null => {
    if ('clipboardData' in event && event.clipboardData) {
      for (const item of event.clipboardData.items) {
        if (item.type.startsWith('image/')) {
          return item.getAsFile();
        }
      }
    }
    if ('dataTransfer' in event && event.dataTransfer) {
      for (const file of event.dataTransfer.files) {
        if (file.type.startsWith('image/')) {
          return file;
        }
      }
    }
    return null;
  };

  const showResizeModal = (file: File) => {
    pendingImage.value = file;
  };

  const cancelPendingImage = () => {
    pendingImage.value = null;
  };

  const handleImageInsertion = async (
    prId: number | undefined,
    file: File,
    _currentValue: string,
    setText: (value: string) => void,
    textarea: HTMLTextAreaElement,
  ): Promise<boolean> => {
    if (!prId) {
      toast.error('Cannot upload image: no PR context.');
      return false;
    }

    const placeholder = `![Uploading ${file.name}...]()`;
    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const value = textarea.value;
    const newValue = value.substring(0, start) + placeholder + value.substring(end);
    setText(newValue);

    await new Promise<void>(r => setTimeout(r, 0));
    textarea.selectionStart = textarea.selectionEnd = start + placeholder.length;

    try {
      const url = await uploadImage(prId, file);
      const markdown = `![${file.name}](${url})`;
      const currentVal = textarea.value;
      const replacedValue = currentVal.replace(placeholder, markdown);
      setText(replacedValue);

      await new Promise<void>(r => setTimeout(r, 0));
      const idx = replacedValue.indexOf(markdown);
      if (idx !== -1) {
        textarea.selectionStart = textarea.selectionEnd = idx + markdown.length;
      }
      return true;
    } catch {
      const currentVal = textarea.value;
      setText(currentVal.replace(placeholder, ''));
      return false;
    }
  };

  const handleImagePaste = (
    event: ClipboardEvent,
  ): File | null => {
    const file = extractImageFile(event);
    if (!file) return null;
    event.preventDefault();
    return file;
  };

  const handleImageDrop = (
    event: DragEvent,
  ): File | null => {
    const file = extractImageFile(event);
    if (!file) return null;
    event.preventDefault();
    return file;
  };

  return {
    isUploading,
    pendingImage,
    uploadImage,
    handleImageInsertion,
    handleImagePaste,
    handleImageDrop,
    showResizeModal,
    cancelPendingImage,
  };
}
