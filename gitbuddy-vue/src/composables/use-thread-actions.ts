import { ref } from 'vue';
import type { Comment, ReviewThread } from '../types';

export interface ThreadActionsProps {
  reviewThreads: ReviewThread[];
  onResolveThread?: (threadId: string, resolved: boolean) => Promise<void>;
}

export function useThreadActions(props: ThreadActionsProps) {
  const expandedThreads = ref<Set<string>>(new Set());
  const resolvingThreads = ref<Set<string>>(new Set());

  const getThreadInfo = (threadId: number | null): ReviewThread | null => {
    if (!threadId) return null;
    return props.reviewThreads.find(rt => rt.id === threadId) ?? null;
  };

  const toggleThreadExpanded = (threadId: number | null) => {
    if (!threadId) return;
    const threadInfo = getThreadInfo(threadId);
    if (!threadInfo) return;

    const threadNodeId = threadInfo.gitHubId;
    if (expandedThreads.value.has(threadNodeId)) {
      expandedThreads.value.delete(threadNodeId);
    } else {
      expandedThreads.value.add(threadNodeId);
    }
  };

  const handleResolveThread = async (threadId: number | null, resolved: boolean) => {
    if (!threadId || !props.onResolveThread) return;

    const threadInfo = getThreadInfo(threadId);
    if (!threadInfo) return;

    const threadNodeId = threadInfo.gitHubId;
    resolvingThreads.value.add(threadNodeId);

    try {
      await props.onResolveThread(threadNodeId, resolved);
    } catch (error) {
      console.error('Failed to resolve thread:', error);
    } finally {
      resolvingThreads.value.delete(threadNodeId);
    }
  };

  const isThreadExpanded = (threadId: number | null): boolean => {
    if (!threadId) return true;
    const threadInfo = getThreadInfo(threadId);
    if (!threadInfo) return true;
    return expandedThreads.value.has(threadInfo.gitHubId);
  };

  return {
    expandedThreads,
    resolvingThreads,
    getThreadInfo,
    toggleThreadExpanded,
    handleResolveThread,
    isThreadExpanded,
  };
}

export function getCommentsForLine(
  comments: Comment[],
  path: string,
  line: number | undefined,
  reviewThreads: ReviewThread[],
  side?: 'left' | 'right'
): Comment[] {
  if (!line) return [];

  const fileComments = comments.filter(c => c.path === path && c.line === line);

  if (!side) return fileComments;

  const expectedSide = side === 'left' ? 'LEFT' : 'RIGHT';

  return fileComments.filter(comment => {
    if (!comment.reviewThreadId) return true;

    const reviewThread = reviewThreads.find(rt => rt.id === comment.reviewThreadId);
    if (!reviewThread) {
      return true;
    }

    if (!reviewThread.diffSide) return true;

    return reviewThread.diffSide.toUpperCase() === expectedSide;
  });
}

export function getCommentsGroupedByThread(
  comments: Comment[],
  line: number | undefined,
  path: string,
  reviewThreads: ReviewThread[],
  side?: 'left' | 'right'
): [number | null, Comment[]][] {
  const lineComments = getCommentsForLine(comments, path, line, reviewThreads, side);

  const threadMap = new Map<number | null, Comment[]>();

  lineComments.forEach(comment => {
    const threadId = comment.reviewThreadId ?? null;
    if (!threadMap.has(threadId)) {
      threadMap.set(threadId, []);
    }
    threadMap.get(threadId)!.push(comment);
  });

  threadMap.forEach(threadComments => {
    threadComments.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
  });

  return Array.from(threadMap.entries());
}

export function isLastCommentInThread(
  comments: Comment[],
  threadId: number | null,
  commentIds: number[]
): boolean {
  if (!threadId) return true;
  const threadComments = comments.filter(c => c.reviewThreadId === threadId);
  if (threadComments.length === 0) return true;
  const lastComment = threadComments[threadComments.length - 1];
  return lastComment ? commentIds.includes(lastComment.id) : false;
}
