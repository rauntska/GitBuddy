import { ref } from 'vue';
import apiClient from '../utils/api';
import type { FileContentResponse, ExpandRange } from '../types';

export function useFileContent(prId: number) {
  const isLoading = ref(false);
  const error = ref<string | null>(null);
  const expandedRanges = ref<ExpandRange[]>([]);
  const contentCache = ref<Map<string, FileContentResponse>>(new Map());

  const fetchFileContent = async (
    path: string,
    oldStartLine: number | null,
    oldEndLine: number | null,
    newStartLine: number | null,
    newEndLine: number | null
  ): Promise<FileContentResponse | null> => {
    const cacheKey = `${path}-${oldStartLine}-${oldEndLine}-${newStartLine}-${newEndLine}`;
    
    if (contentCache.value.has(cacheKey)) {
      return contentCache.value.get(cacheKey)!;
    }

    isLoading.value = true;
    error.value = null;

    try {
      const params = new URLSearchParams();
      params.append('path', path);
      if (oldStartLine !== null) params.append('oldStartLine', oldStartLine.toString());
      if (oldEndLine !== null) params.append('oldEndLine', oldEndLine.toString());
      if (newStartLine !== null) params.append('newStartLine', newStartLine.toString());
      if (newEndLine !== null) params.append('newEndLine', newEndLine.toString());

      const response = await apiClient.get<FileContentResponse>(
        `/pullrequests/${prId}/files/content?${params.toString()}`
      );

      contentCache.value.set(cacheKey, response.data);
      return response.data;
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to fetch file content';
      return null;
    } finally {
      isLoading.value = false;
    }
  };

  const addExpandedRange = (range: ExpandRange) => {
    expandedRanges.value.push(range);
  };

  const clearCache = () => {
    contentCache.value.clear();
    expandedRanges.value = [];
  };

  return {
    isLoading,
    error,
    expandedRanges,
    fetchFileContent,
    addExpandedRange,
    clearCache,
  };
}
