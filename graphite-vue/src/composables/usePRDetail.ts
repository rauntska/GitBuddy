import { ref } from 'vue';
import type { PRDetail, Comment } from '../types';
import { apiService } from '../services/api';
import { useUserPreferences } from './useUserPreferences';

export function usePRDetail() {
  const prDetail = ref<PRDetail | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const commentsPanel = ref(false);
  
  const { preferences, setFileTreeVisible } = useUserPreferences();

  const fetchPRDetail = async (id: number) => {
    loading.value = true;
    error.value = null;
    try {
      prDetail.value = await apiService.getPRDetail(id);
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to fetch PR details';
      console.error('Error fetching PR details:', err);
    } finally {
      loading.value = false;
    }
  };

  const addComment = async (
    prId: number,
    comment: { body: string; path?: string; line?: number }
  ): Promise<Comment | null> => {
    try {
      const newComment = await apiService.addComment(prId, comment);
      
      // Refresh PR details to get review threads from backend
      await fetchPRDetail(prId);
      
      return newComment;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to add comment';
      console.error('Error adding comment:', err);
      return null;
    }
  };

  const submitReview = async (
    prId: number,
    review: { state: 'APPROVED' | 'CHANGES_REQUESTED' | 'COMMENT'; body?: string }
  ) => {
    try {
      await apiService.submitReview(prId, review);
      // Refresh PR details after submitting review
      await fetchPRDetail(prId);
      return true;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to submit review';
      console.error('Error submitting review:', err);
      return false;
    }
  };

  const mergePR = async (
    prId: number, 
    options?: { 
      mergeMethod?: 'merge' | 'squash' | 'rebase';
      commitTitle?: string;
      commitMessage?: string;
    }
  ) => {
    try {
      const result = await apiService.mergePR(prId, options);
      await fetchPRDetail(prId);
      return { success: true, message: result.message };
    } catch (err: any) {
      const errorMessage = err.response?.data?.message || 'Failed to merge PR';
      error.value = errorMessage;
      console.error('Error merging PR:', err);
      return { success: false, message: errorMessage };
    }
  };

  const getMergeOptions = async (prId: number) => {
    try {
      return await apiService.getMergeOptions(prId);
    } catch (err: any) {
      console.error('Error getting merge options:', err);
      return null;
    }
  };

  const publishDraftPR = async (prId: number) => {
    try {
      await apiService.publishDraftPR(prId);
      // Refresh PR details after publishing
      await fetchPRDetail(prId);
      return true;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to publish draft PR';
      console.error('Error publishing draft PR:', err);
      return false;
    }
  };

  const toggleCommentsPanel = () => {
    commentsPanel.value = !commentsPanel.value;
  };

  const toggleFileTree = async () => {
    await setFileTreeVisible(!preferences.value.fileTreeVisible);
  };

  return {
    prDetail,
    loading,
    error,
    commentsPanel,
    fileTreeVisible: preferences,
    fetchPRDetail,
    addComment,
    submitReview,
    mergePR,
    getMergeOptions,
    publishDraftPR,
    toggleCommentsPanel,
    toggleFileTree,
  };
}
