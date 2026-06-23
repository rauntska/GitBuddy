import { ref } from 'vue';

const baseTitle = ref<string>('Git Buddy');

export function useFaviconBadge() {
  const initFavicon = () => {
    baseTitle.value = document.title.replace(/\s*\(\d+\)$/, '') || 'Git Buddy';
  };

  const updateBadge = (count: number) => {
    if (count === 0) {
      document.title = baseTitle.value;
    } else {
      document.title = `${baseTitle.value} (${count})`;
    }
  };

  return {
    initFavicon,
    updateBadge,
  };
}
