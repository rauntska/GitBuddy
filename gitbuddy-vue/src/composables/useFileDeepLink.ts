import { computed, type Ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

/**
 * `?path=src/Program.cs` on the PR detail route — selects and scrolls to a file, and is
 * shareable. Layout-independent: it is a query param, so it needs no route entry.
 */
export function useFileDeepLink(prId: Ref<number>) {
  const route = useRoute();
  const router = useRouter();

  const selectedFilePath = computed(() => {
    const path = route.query.path;
    return typeof path === 'string' && path.length > 0 ? path : undefined;
  });

  const setSelectedFilePath = (path: string | undefined) => {
    if (selectedFilePath.value === path) return;

    const query = { ...route.query };
    if (path) {
      query.path = path;
    } else {
      delete query.path;
    }

    // Replace, not push: browsing the file tree shouldn't fill up browser history.
    router.replace({ name: 'pr-detail', params: { id: prId.value }, query });
  };

  return { selectedFilePath, setSelectedFilePath };
}
