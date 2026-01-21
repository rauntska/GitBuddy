import { ref } from 'vue';
import { apiService } from '../services/api';
import type { GroupedPRs, PRStats } from '../types';

export function usePullRequests() {
  const pullRequests = ref<GroupedPRs>({});
  const stats = ref<PRStats>({ totalOpen: 0, draft: 0, approved: 0, awaitingReview: 0 });
  const loading = ref(false);
  const error = ref<string | null>(null);
  const lastRefresh = ref<string | null>(null);

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
      await apiService.refreshPullRequests();
      await fetchPullRequests();
    } catch (err) {
      error.value = 'Failed to refresh pull requests';
      console.error(err);
    } finally {
      loading.value = false;
    }
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
  };
}
