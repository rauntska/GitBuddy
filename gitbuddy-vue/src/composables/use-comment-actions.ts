import { ref, nextTick } from 'vue';
import type { Comment, PendingReviewComment } from '../types';

export interface CommentActionsProps {
  currentUsername?: string;
  onAddComment: (line: number, body: string, side: string) => Promise<void>;
  onReplyToThread?: (threadId: string, line: number, body: string) => Promise<void>;
  onEditComment?: (commentId: number, body: string) => Promise<void>;
  onDeleteComment?: (commentId: number) => Promise<void>;
  onDeletePendingComment?: (commentId: string) => Promise<void>;
}

export function useCommentActions(props: CommentActionsProps) {
  const commentingLine = ref<number | null>(null);
  const commentingSide = ref<'left' | 'right'>('right');
  const newCommentText = ref('');
  const commentError = ref('');
  
  const editingCommentId = ref<number | null>(null);
  const editText = ref('');
  
  const replyingToCommentId = ref<number | null>(null);
  const replyingToThread = ref<string | null>(null);
  const replyText = ref('');
  const replyErrors = ref<Map<number, string>>(new Map());
  
  const deletingCommentId = ref<number | null>(null);
  const isSubmitting = ref(false);

  const isOwnComment = (comment: Comment): boolean => {
    return props.currentUsername !== undefined && 
           comment.author.toLowerCase() === props.currentUsername.toLowerCase();
  };

  const startComment = async (line: number, side: 'left' | 'right' = 'right') => {
    commentingLine.value = line;
    commentingSide.value = side;
    newCommentText.value = '';
    commentError.value = '';
    await nextTick();
  };

  const cancelNewComment = () => {
    commentingLine.value = null;
    commentingSide.value = 'right';
    newCommentText.value = '';
    commentError.value = '';
  };

  const submitNewComment = async () => {
    if (!newCommentText.value.trim() || commentingLine.value === null) return;

    try {
      await props.onAddComment(commentingLine.value, newCommentText.value, commentingSide.value.toUpperCase());
      newCommentText.value = '';
      commentingLine.value = null;
      commentingSide.value = 'right';
      commentError.value = '';
    } catch (error) {
      commentError.value = 'Failed to add comment';
      console.error('Failed to add comment:', error);
    }
  };

  const startEditComment = (commentId: number, body: string) => {
    editingCommentId.value = commentId;
    editText.value = body;
  };

  const cancelEdit = () => {
    editingCommentId.value = null;
    editText.value = '';
  };

  const submitEdit = async (text: string) => {
    if (!text.trim() || editingCommentId.value === null) return;

    isSubmitting.value = true;
    try {
      if (props.onEditComment) {
        await props.onEditComment(editingCommentId.value, text);
        editingCommentId.value = null;
      }
    } catch (error) {
      console.error('Failed to edit comment:', error);
    } finally {
      isSubmitting.value = false;
    }
  };

  const startReplyToComment = (commentId: number, threadGitId: string | null) => {
    replyingToCommentId.value = commentId;
    replyingToThread.value = threadGitId;
    replyText.value = '';
    replyErrors.value.delete(commentId);
  };

  const cancelReply = () => {
    if (replyingToCommentId.value !== null) {
      replyErrors.value.delete(replyingToCommentId.value);
    }
    replyingToCommentId.value = null;
    replyingToThread.value = null;
    replyText.value = '';
  };

  const submitReply = async (text: string) => {
    if (!text.trim() || !replyingToCommentId.value) return;
    if (!replyingToThread.value) {
      console.error('No thread ID for reply');
      return;
    }

    isSubmitting.value = true;
    try {
      if (props.onReplyToThread) {
        await props.onReplyToThread(replyingToThread.value, commentingLine.value ?? 0, text);
        replyingToCommentId.value = null;
        replyingToThread.value = null;
      }
    } catch (error) {
      console.error('Failed to add reply:', error);
      if (replyingToCommentId.value !== null) {
        replyErrors.value.set(replyingToCommentId.value, 'Failed to add reply');
      }
    } finally {
      isSubmitting.value = false;
    }
  };

  const handleDeleteComment = async (commentId: number) => {
    if (!confirm('Are you sure you want to delete this comment?')) return;

    try {
      if (props.onDeleteComment) {
        deletingCommentId.value = commentId;
        await props.onDeleteComment(commentId);
        deletingCommentId.value = null;
      }
    } catch (error) {
      console.error('Failed to delete comment:', error);
      deletingCommentId.value = null;
    }
  };

  const handleDeletePendingComment = async (commentId: string) => {
    if (props.onDeletePendingComment) {
      await props.onDeletePendingComment(commentId);
    }
  };

  const formatTimeAgo = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

    if (seconds < 60) return 'just now';
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    if (days < 7) return `${days}d ago`;
    return date.toLocaleDateString();
  };

  const formatRelativeTime = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

    if (seconds < 60) return 'just now';
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    return `${days}d ago`;
  };

  return {
    commentingLine,
    commentingSide,
    newCommentText,
    commentError,
    editingCommentId,
    editText,
    replyingToCommentId,
    replyingToThread,
    replyText,
    replyErrors,
    deletingCommentId,
    isSubmitting,
    
    isOwnComment,
    startComment,
    cancelNewComment,
    submitNewComment,
    startEditComment,
    cancelEdit,
    submitEdit,
    startReplyToComment,
    cancelReply,
    submitReply,
    handleDeleteComment,
    handleDeletePendingComment,
    formatTimeAgo,
    formatRelativeTime,
  };
}

export function getPendingCommentsForLine(
  pendingComments: PendingReviewComment[],
  path: string,
  line: number | undefined
): PendingReviewComment[] {
  if (!line) return [];
  return pendingComments.filter(c => c.path === path && c.line === line);
}

export function getPendingRepliesForThread(
  pendingComments: PendingReviewComment[],
  threadGitId: string | undefined
): PendingReviewComment[] {
  if (!threadGitId) return [];
  return pendingComments.filter(c => c.threadId === threadGitId);
}
