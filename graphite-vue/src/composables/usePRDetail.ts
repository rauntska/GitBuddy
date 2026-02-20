import { ref } from 'vue';
import type { PRDetail, Comment, ReviewThread, PendingReviewComment } from '../types';
import { apiService } from '../services/api';
import { useUserPreferences } from './useUserPreferences';

export function usePRDetail() {
  const prDetail = ref<PRDetail | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const commentsPanel = ref(false);
  
  const { preferences, setFileTreeVisible } = useUserPreferences();

  const fetchPRDetail = async (id: number, silent = false) => {
    if (!silent) {
      loading.value = true;
    }
    error.value = null;
    try {
      prDetail.value = await apiService.getPRDetail(id);
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to fetch PR details';
      console.error('Error fetching PR details:', err);
    } finally {
      if (!silent) {
        loading.value = false;
      }
    }
  };

  const addComment = async (
    prId: number,
    comment: { body: string; path?: string; line?: number }
  ): Promise<Comment | null> => {
    try {
      const newComment = await apiService.addComment(prId, comment);
      
      if (newComment && newComment.reviewThreadId && prDetail.value) {
        const existingThread = prDetail.value.reviewThreads.find(
          rt => rt.id === newComment.reviewThreadId
        );
        
        if (!existingThread && comment.path && comment.line) {
          const newThread: ReviewThread = {
            id: newComment.reviewThreadId,
            gitHubId: String(newComment.gitHubId),
            path: comment.path,
            line: comment.line,
            diffSide: 'RIGHT',
            state: 'UNRESOLVED',
            isResolved: false,
            isOutdated: false,
            createdAt: newComment.createdAt,
            updatedAt: newComment.updatedAt,
            firstCommentAuthor: newComment.author,
            firstCommentBody: newComment.body,
            commentCount: 1
          };
          prDetail.value.reviewThreads.push(newThread);
        }
        
        prDetail.value.allComments.push(newComment);
      }
      
      return newComment;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to add comment';
      console.error('Error adding comment:', err);
      return null;
    }
  };

  const addPendingReviewComment = async (
    prId: number,
    comment: { body: string; path: string; line: number }
  ): Promise<PendingReviewComment | null> => {
    try {
      const result = await apiService.addPendingReviewComment(prId, comment);
      
      if (prDetail.value) {
        if (!prDetail.value.pendingReview) {
          prDetail.value.pendingReview = {
            gitHubId: result.reviewId,
            state: 'PENDING',
            comments: []
          };
        }
        
        const newComment: PendingReviewComment = {
          gitHubId: result.commentId,
          path: result.path,
          line: result.line,
          body: result.body,
          author: result.author,
          authorAvatar: result.authorAvatar,
          createdAt: result.createdAt
        };
        
        prDetail.value.pendingReview.comments.push(newComment);
      }
      
      return {
        gitHubId: result.commentId,
        path: result.path,
        line: result.line,
        body: result.body,
        author: result.author,
        authorAvatar: result.authorAvatar,
        createdAt: result.createdAt
      };
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to add pending review comment';
      console.error('Error adding pending review comment:', err);
      return null;
    }
  };

  const deletePendingReviewComment = async (prId: number, commentId: string): Promise<boolean> => {
    try {
      await apiService.deletePendingReviewComment(prId, commentId);
      
      if (prDetail.value?.pendingReview) {
        prDetail.value.pendingReview.comments = prDetail.value.pendingReview.comments.filter(
          c => c.gitHubId !== commentId
        );
        
        if (prDetail.value.pendingReview.comments.length === 0) {
          prDetail.value.pendingReview = undefined;
        }
      }
      
      return true;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to delete pending review comment';
      console.error('Error deleting pending review comment:', err);
      return false;
    }
  };

  const addReply = async (
    prId: number,
    reply: { reviewThreadId: string; body: string }
  ): Promise<Comment | null> => {
    try {
      const result = await apiService.addCommentReply(prId, reply);
      
      if (!result) return null;
      
      // Handle pending reply
      if ('isPending' in result && result.isPending) {
        if (prDetail.value) {
          // Initialize pending review if needed
          if (!prDetail.value.pendingReview) {
            prDetail.value.pendingReview = {
              gitHubId: result.pendingReviewId || '',
              state: 'PENDING',
              comments: []
            };
          }
          
          // Add the pending reply to pending review comments
          const pendingComment: PendingReviewComment = {
            gitHubId: result.commentNodeId || '',
            path: '',
            line: undefined,
            body: result.body,
            author: result.author,
            authorAvatar: result.authorAvatar,
            createdAt: result.createdAt,
            updatedAt: result.updatedAt
          };
          prDetail.value.pendingReview.comments.push(pendingComment);
        }
        return null;
      }
      
      // Handle regular reply
      const newReply = result as Comment;
      if (newReply && prDetail.value) {
        const thread = prDetail.value.reviewThreads.find(
          rt => rt.gitHubId === reply.reviewThreadId || String(rt.id) === reply.reviewThreadId
        );
        
        if (thread) {
          thread.commentCount++;
          thread.updatedAt = newReply.updatedAt;
        }
        
        prDetail.value.allComments.push(newReply);
      }
      
      return newReply;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to add reply';
      console.error('Error adding reply:', err);
      return null;
    }
  };

  const submitReview = async (
    prId: number,
    review: { state: 'APPROVED' | 'CHANGES_REQUESTED' | 'COMMENT'; body?: string }
  ) => {
    try {
      await apiService.submitReview(prId, review);
      await fetchPRDetail(prId);
      return true;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to submit review';
      console.error('Error submitting review:', err);
      return false;
    }
  };

  const submitPendingReview = async (
    prId: number,
    review: { state: 'APPROVED' | 'CHANGES_REQUESTED' | 'COMMENT'; body?: string }
  ) => {
    try {
      await apiService.submitPendingReview(prId, review);
      await fetchPRDetail(prId);
      return true;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to submit pending review';
      console.error('Error submitting pending review:', err);
      return false;
    }
  };

  const deletePendingReview = async (prId: number) => {
    try {
      await apiService.deletePendingReview(prId);
      
      if (prDetail.value) {
        prDetail.value.pendingReview = undefined;
      }
      
      return true;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to delete pending review';
      console.error('Error deleting pending review:', err);
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

  const editComment = async (commentId: number, body: string): Promise<boolean> => {
    try {
      await apiService.updateComment(commentId, body);
      
      if (prDetail.value) {
        const comment = prDetail.value.allComments.find(c => c.id === commentId);
        if (comment) {
          comment.body = body;
          comment.updatedAt = new Date().toISOString();
        }
      }
      
      return true;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to edit comment';
      console.error('Error editing comment:', err);
      return false;
    }
  };

  const deleteComment = async (commentId: number): Promise<boolean> => {
    try {
      await apiService.deleteComment(commentId);
      
      if (prDetail.value) {
        prDetail.value.allComments = prDetail.value.allComments.filter(c => c.id !== commentId);
      }
      
      return true;
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to delete comment';
      console.error('Error deleting comment:', err);
      return false;
    }
  };

  return {
    prDetail,
    loading,
    error,
    commentsPanel,
    fileTreeVisible: preferences,
    fetchPRDetail,
    addComment,
    addPendingReviewComment,
    deletePendingReviewComment,
    addReply,
    submitReview,
    submitPendingReview,
    deletePendingReview,
    mergePR,
    getMergeOptions,
    publishDraftPR,
    toggleCommentsPanel,
    toggleFileTree,
    editComment,
    deleteComment,
  };
}
