import { ref, watch, onMounted, onUnmounted, computed } from 'vue';
import api from '../utils/api';

interface DraftOptions {
  pullRequestId: number;
  reviewThreadId?: number;
  filePath?: string;
  lineNumber?: number;
  autosaveInterval?: number; // milliseconds
}

interface DraftState {
  body: string;
  updatedAt: Date | null;
  isSaving: boolean;
  lastSavedAt: Date | null;
  hasUnsavedChanges: boolean;
}

export function useDraftAutosave(options: DraftOptions) {
  const {
    pullRequestId,
    reviewThreadId,
    filePath,
    lineNumber,
    autosaveInterval = 3000 // 3 seconds default
  } = options;

  const body = ref('');
  const isSaving = ref(false);
  const lastSavedAt = ref<Date | null>(null);
  const hasUnsavedChanges = ref(false);
  const draftId = ref<number | null>(null);

  let saveTimeout: ReturnType<typeof setTimeout> | null = null;
  let lastSavedBody = '';

  const status = computed(() => {
    if (isSaving.value) return 'saving';
    if (hasUnsavedChanges.value) return 'unsaved';
    if (lastSavedAt.value) return 'saved';
    return 'empty';
  });

  const saveDraft = async (content: string) => {
    if (!content.trim() || content === lastSavedBody) {
      return;
    }

    isSaving.value = true;

    try {
      const response = await api.post('/api/comment-drafts', {
        pullRequestId,
        reviewThreadId: reviewThreadId || null,
        filePath: filePath || null,
        lineNumber: lineNumber || null,
        body: content
      });

      draftId.value = response.data.id;
      lastSavedAt.value = new Date();
      lastSavedBody = content;
      hasUnsavedChanges.value = false;
    } catch (error) {
      console.error('Failed to save draft:', error);
    } finally {
      isSaving.value = false;
    }
  };

  const loadDraft = async () => {
    try {
      const params = new URLSearchParams();
      params.append('pullRequestId', pullRequestId.toString());
      if (reviewThreadId) params.append('reviewThreadId', reviewThreadId.toString());
      if (filePath) params.append('filePath', filePath);
      if (lineNumber) params.append('lineNumber', lineNumber.toString());

      const response = await api.get(`/api/comment-drafts/prs/${pullRequestId}`, {
        params: {
          reviewThreadId: reviewThreadId || undefined,
          filePath: filePath || undefined,
          lineNumber: lineNumber || undefined
        }
      });

      if (response.data && response.data.length > 0) {
        // Find matching draft
        const draft = response.data.find((d: any) =>
          d.reviewThreadId === (reviewThreadId || null) &&
          d.filePath === (filePath || null) &&
          d.lineNumber === (lineNumber || null)
        );

        if (draft) {
          body.value = draft.body;
          draftId.value = draft.id;
          lastSavedAt.value = new Date(draft.updatedAt);
          lastSavedBody = draft.body;
          return true;
        }
      }
    } catch (error) {
      console.error('Failed to load draft:', error);
    }
    return false;
  };

  const clearDraft = async () => {
    if (!draftId.value) return;

    try {
      await api.delete(`/api/comment-drafts/${draftId.value}`);
      body.value = '';
      draftId.value = null;
      lastSavedAt.value = null;
      lastSavedBody = '';
      hasUnsavedChanges.value = false;
    } catch (error) {
      console.error('Failed to clear draft:', error);
    }
  };

  // Auto-save with debouncing
  watch(body, (newBody) => {
    if (newBody !== lastSavedBody) {
      hasUnsavedChanges.value = true;

      // Clear existing timeout
      if (saveTimeout) {
        clearTimeout(saveTimeout);
      }

      // Set new timeout
      saveTimeout = setTimeout(() => {
        saveDraft(newBody);
      }, autosaveInterval);
    }
  });

  // Load draft on mount
  onMounted(() => {
    loadDraft();
  });

  // Save on unmount if there are unsaved changes
  onUnmounted(() => {
    if (saveTimeout) {
      clearTimeout(saveTimeout);
    }

    if (hasUnsavedChanges.value && body.value.trim()) {
      // Use sendBeacon for reliable save on page unload
      const data = JSON.stringify({
        pullRequestId,
        reviewThreadId: reviewThreadId || null,
        filePath: filePath || null,
        lineNumber: lineNumber || null,
        body: body.value
      });

      // Fallback to regular save if sendBeacon not available
      if (!navigator.sendBeacon) {
        saveDraft(body.value);
      } else {
        navigator.sendBeacon('/api/comment-drafts', new Blob([data], { type: 'application/json' }));
      }
    }
  });

  return {
    body,
    status,
    isSaving,
    lastSavedAt,
    hasUnsavedChanges,
    saveDraft,
    loadDraft,
    clearDraft
  };
}

// Composable for managing multiple drafts
export function useDraftManager() {
  const drafts = ref<Map<string, DraftState>>(new Map());

  const getDraftKey = (prId: number, threadId?: number, path?: string, line?: number) => {
    return `${prId}-${threadId || 'general'}-${path || ''}-${line || ''}`;
  };

  const getDraft = (prId: number, threadId?: number, path?: string, line?: number) => {
    const key = getDraftKey(prId, threadId, path, line);
    return drafts.value.get(key);
  };

  const setDraft = (prId: number, threadId: number | undefined, path: string | undefined, line: number | undefined, state: DraftState) => {
    const key = getDraftKey(prId, threadId, path, line);
    drafts.value.set(key, state);
  };

  const clearAllDrafts = () => {
    drafts.value.clear();
  };

  return {
    drafts,
    getDraft,
    setDraft,
    clearAllDrafts
  };
}
