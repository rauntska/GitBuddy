import { ref, computed } from 'vue';
import { apiService } from '../services/api';
import type { GroupedPRs, PRStats, PullRequest } from '../types';
import { useSignalR, type PRListUpdate, type PRClosedNotification, type ReviewNotification, type ThreadNotification } from './useSignalR';

export function usePullRequests() {
  const pullRequests = ref<GroupedPRs>({});
  const stats = ref<PRStats>({ 
    totalOpen: 0, 
    draft: 0, 
    approved: 0, 
    awaitingReview: 0,
    totalComments: 0,
    resolvedComments: 0,
    pendingComments: 0
  });
  const loading = ref(false);
  const error = ref<string | null>(null);
  const lastRefresh = ref<string | null>(null);
  const mergedPRs = ref<PullRequest[]>([]);
  const mergedPRsLoading = ref(false);
  const mergedPRsSkip = ref(0);
  const mergedPRsTotal = ref(0);
  const mergedPRsHasMore = ref(true);

  const signalR = useSignalR();

  const setupSignalRHandlers = () => {
    signalR.onPRCreated.value = (pr: PRListUpdate) => {
      const newPR: PullRequest = {
        id: pr.id,
        gitHubId: pr.gitHubId,
        title: pr.title,
        repository: pr.repository,
        author: pr.author,
        authorAvatar: pr.authorAvatar,
        status: pr.status,
        draft: pr.draft,
        url: pr.url,
        additions: pr.additions,
        deletions: pr.deletions,
        changedFiles: pr.changedFiles,
        createdAt: pr.createdAt,
        updatedAt: pr.updatedAt,
        lastSyncedAt: pr.updatedAt,
        reviews: pr.reviews,
        reviewThreads: pr.reviewThreads.map(rt => ({
          ...rt,
          diffSide: rt.diffSide as 'LEFT' | 'RIGHT' | undefined
        })),
        checksStatus: pr.checksStatus as 'SUCCESS' | 'FAILURE' | 'PENDING' | 'NEUTRAL' | null | undefined
      };

      const status = pr.status || 'AwaitingReview';
      if (!pullRequests.value[status]) {
        pullRequests.value[status] = [];
      }
      
      const prList = pullRequests.value[status];
      if (!prList) return;
      
      const existingIndex = prList.findIndex(p => p.id === pr.id);
      if (existingIndex === -1) {
        prList.unshift(newPR);
        stats.value.totalOpen++;
        if (pr.draft) stats.value.draft++;
        if (pr.status === 'Approved') stats.value.approved++;
        if (pr.status === 'AwaitingReview') stats.value.awaitingReview++;
      }
    };

    signalR.onPRUpdated.value = (pr: PRListUpdate) => {
      let found = false;
      
      for (const status in pullRequests.value) {
        const prList = pullRequests.value[status];
        if (!prList) continue;
        
        const idx = prList.findIndex(p => p.id === pr.id);
        if (idx !== -1) {
          found = true;
          const existingPR = prList[idx];
          if (!existingPR) continue;
          
          if (pr.status && pr.status !== status) {
            prList.splice(idx, 1);
            
            if (!pullRequests.value[pr.status]) {
              pullRequests.value[pr.status] = [];
            }
            
            const targetList = pullRequests.value[pr.status];
            if (!targetList) continue;
            
            const updatedPR: PullRequest = {
              ...existingPR,
              title: pr.title,
              status: pr.status,
              draft: pr.draft,
              additions: pr.additions,
              deletions: pr.deletions,
              changedFiles: pr.changedFiles,
              updatedAt: pr.updatedAt,
              checksStatus: pr.checksStatus as 'SUCCESS' | 'FAILURE' | 'PENDING' | 'NEUTRAL' | null | undefined,
              reviews: pr.reviews,
              reviewThreads: pr.reviewThreads.map(rt => ({
                ...rt,
                diffSide: rt.diffSide as 'LEFT' | 'RIGHT' | undefined
              }))
            };
            
            targetList.push(updatedPR);
          } else {
            prList[idx] = {
              ...existingPR,
              title: pr.title,
              draft: pr.draft,
              additions: pr.additions,
              deletions: pr.deletions,
              changedFiles: pr.changedFiles,
              updatedAt: pr.updatedAt,
              checksStatus: pr.checksStatus as 'SUCCESS' | 'FAILURE' | 'PENDING' | 'NEUTRAL' | null | undefined,
              reviews: pr.reviews,
              reviewThreads: pr.reviewThreads.map(rt => ({
                ...rt,
                diffSide: rt.diffSide as 'LEFT' | 'RIGHT' | undefined
              }))
            };
          }
          break;
        }
      }

      if (!found && pr.status) {
        const newPR: PullRequest = {
          id: pr.id,
          gitHubId: pr.gitHubId,
          title: pr.title,
          repository: pr.repository,
          author: pr.author,
          authorAvatar: pr.authorAvatar,
          status: pr.status,
          draft: pr.draft,
          url: pr.url,
          additions: pr.additions,
          deletions: pr.deletions,
          changedFiles: pr.changedFiles,
          createdAt: pr.createdAt,
          updatedAt: pr.updatedAt,
          lastSyncedAt: pr.updatedAt,
          reviews: pr.reviews,
          reviewThreads: pr.reviewThreads.map(rt => ({
            ...rt,
            diffSide: rt.diffSide as 'LEFT' | 'RIGHT' | undefined
          })),
          checksStatus: pr.checksStatus as 'SUCCESS' | 'FAILURE' | 'PENDING' | 'NEUTRAL' | null | undefined
        };

        if (!pullRequests.value[pr.status]) {
          pullRequests.value[pr.status] = [];
        }
        const targetList = pullRequests.value[pr.status];
        if (targetList) {
          targetList.push(newPR);
          stats.value.totalOpen++;
        }
      }
    };

    signalR.onPRClosed.value = (notification: PRClosedNotification) => {
      for (const status in pullRequests.value) {
        const prList = pullRequests.value[status];
        if (!prList) continue;
        
        const idx = prList.findIndex(p => p.id === notification.pullRequestId);
        if (idx !== -1) {
          prList.splice(idx, 1);
          stats.value.totalOpen--;
          break;
        }
      }
    };

    signalR.onReviewAdded.value = (notification: ReviewNotification) => {
      const newStatus = notification.newStatus;
      
      for (const status in pullRequests.value) {
        const prList = pullRequests.value[status];
        if (!prList) continue;
        
        const idx = prList.findIndex(p => p.id === notification.pullRequestId);
        if (idx !== -1) {
          const pr = prList[idx];
          if (!pr) continue;
          
          if (newStatus && newStatus !== status) {
            prList.splice(idx, 1);
            
            const existingReviewIdx = pr.reviews.findIndex(r => r.id === notification.review.id);
            if (existingReviewIdx !== -1) {
              pr.reviews[existingReviewIdx] = notification.review;
            } else {
              pr.reviews.push(notification.review);
            }
            
            pr.status = newStatus;
            
            if (!pullRequests.value[newStatus]) {
              pullRequests.value[newStatus] = [];
            }
            const targetList = pullRequests.value[newStatus];
            if (targetList) {
              targetList.push(pr);
            }
          } else {
            const existingReviewIdx = pr.reviews.findIndex(r => r.id === notification.review.id);
            if (existingReviewIdx !== -1) {
              pr.reviews[existingReviewIdx] = notification.review;
            } else {
              pr.reviews.push(notification.review);
            }
          }
          break;
        }
      }
    };

    signalR.onThreadChanged.value = (notification: ThreadNotification) => {
      for (const status in pullRequests.value) {
        const prList = pullRequests.value[status];
        if (!prList) continue;
        
        for (const pr of prList) {
          const threadIdx = pr.reviewThreads.findIndex(t => t.id === notification.threadId);
          if (threadIdx !== -1) {
            const thread = pr.reviewThreads[threadIdx];
            if (thread) {
              thread.isResolved = notification.isResolved;
              thread.state = notification.isResolved ? 'RESOLVED' : 'UNRESOLVED';
            }
            return;
          }
        }
      }
    };
  };

  setupSignalRHandlers();

  const fetchPullRequests = async () => {
    loading.value = true;
    error.value = null;
    try {
      pullRequests.value = await apiService.getPullRequests();
      stats.value = await apiService.getStats();
    } catch (err) {
      error.value = 'Failed to fetch pull requests';
      console.error(err);
    } finally {
      loading.value = false;
    }
  };

  const refreshPullRequests = async () => {
    loading.value = true;
    error.value = null;
    try {
      pullRequests.value = await apiService.getPullRequests();
      stats.value = await apiService.getStats();
    } catch (err) {
      error.value = 'Failed to refresh pull requests';
      console.error(err);
    } finally {
      loading.value = false;
    }
  };

  const loadMergedPRs = async (reset = false) => {
    if (reset) {
      mergedPRs.value = [];
      mergedPRsSkip.value = 0;
    }

    mergedPRsLoading.value = true;
    try {
      const result = await apiService.getMergedPRs(mergedPRsSkip.value, 10);
      if (reset) {
        mergedPRs.value = result.pullRequests;
      } else {
        mergedPRs.value = [...mergedPRs.value, ...result.pullRequests];
      }
      mergedPRsTotal.value = result.total;
      mergedPRsHasMore.value = result.hasMore;
    } catch (err) {
      console.error('Failed to load merged PRs', err);
    } finally {
      mergedPRsLoading.value = false;
    }
  };

  const loadMoreMergedPRs = async () => {
    if (mergedPRsLoading.value || !mergedPRsHasMore.value) return;
    mergedPRsSkip.value += 10;
    await loadMergedPRs(false);
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

  const getStatusColor = (status: string): string => {
    const colors: Record<string, string> = {
      AwaitingReview: 'bg-blue-500',
      Approved: 'bg-green-500',
      Reviewed: 'bg-purple-500',
      ChangesRequested: 'bg-orange-500',
      Draft: 'bg-gray-500',
    };
    return colors[status] || 'bg-gray-500';
  };

  const getStatusLabel = (status: string): string => {
    const labels: Record<string, string> = {
      AwaitingReview: 'Awaiting Review',
      Approved: 'Approved',
      Reviewed: 'Reviewed',
      ChangesRequested: 'Changes Requested',
      Draft: 'Draft',
    };
    return labels[status] || status;
  };

  const getStatusIcon = (status: string): string => {
    const icons: Record<string, string> = {
      AwaitingReview: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
      Approved: 'M5 13l4 4L19 7',
      Reviewed: 'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z',
      ChangesRequested: 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z',
      Draft: 'M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z',
    };
    return icons[status] || 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z';
  };

  const readyToMergePRs = computed(() => {
    const allPRs: PullRequest[] = [];
    for (const status in pullRequests.value) {
      const prList = pullRequests.value[status];
      if (prList) {
        allPRs.push(...prList);
      }
    }
    return allPRs.filter(pr => 
      pr.isMergeReady === true && 
      !pr.draft && 
      !pr.isMerged && 
      pr.status !== 'Merged' && 
      pr.status !== 'Closed'
    );
  });

  return {
    pullRequests,
    stats,
    loading,
    error,
    lastRefresh,
    fetchPullRequests,
    refreshPullRequests,
    formatRelativeTime,
    getStatusColor,
    getStatusLabel,
    getStatusIcon,
    readyToMergePRs,
    mergedPRs,
    mergedPRsLoading,
    mergedPRsTotal,
    mergedPRsHasMore,
    loadMergedPRs,
    loadMoreMergedPRs,
    signalR: {
      connectionState: signalR.connectionState,
      connect: signalR.connect,
      disconnect: signalR.disconnect,
      joinPRRoom: signalR.joinPRRoom,
      leavePRRoom: signalR.leavePRRoom,
    },
  };
}
