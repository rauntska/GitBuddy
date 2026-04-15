import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import type { PullRequest, NotificationPreferences } from '../types';
import type {
  PRListUpdate,
  ReviewNotification,
  CommentNotification,
  ThreadNotification,
  PRClosedNotification,
  CheckRunsNotification,
} from './useSignalR';

const defaultPreferences: NotificationPreferences = {
  enabled: true,
  events: {
    prCreated: true,
    reviewAdded: true,
    commentAdded: true,
    threadResolved: true,
    prMerged: true,
    checkFailed: true,
  },
  quietHours: {
    enabled: false,
    start: '22:00',
    end: '08:00',
  },
};

const lastNotified = new Map<string, number>();
const DEDUP_WINDOW_MS = 30_000;

export function useBrowserNotifications() {
  const router = useRouter();
  const authStore = useAuthStore();

  const permissionStatus = ref<NotificationPermission>(
    'Notification' in window ? Notification.permission : 'denied'
  );
  const dismissed = ref(localStorage.getItem('notifications-dismissed') === 'true');
  const showPermissionBanner = computed(
    () =>
      permissionStatus.value === 'default' &&
      !dismissed.value &&
      authStore.isAuthenticated
  );

  const isSupported = computed(() => 'Notification' in window);

  const preferences = ref<NotificationPreferences>({ ...defaultPreferences });

  function loadPreferences(prefs?: NotificationPreferences) {
    preferences.value = prefs ? { ...defaultPreferences, ...prefs } : { ...defaultPreferences };
  }

  async function requestPermission(): Promise<NotificationPermission> {
    if (!isSupported.value) return 'denied';
    const result = await Notification.requestPermission();
    permissionStatus.value = result;
    return result;
  }

  function dismissBanner() {
    dismissed.value = true;
    localStorage.setItem('notifications-dismissed', 'true');
  }

  function resetDismiss() {
    dismissed.value = false;
    localStorage.removeItem('notifications-dismissed');
  }

  function isQuietHours(): boolean {
    if (!preferences.value.quietHours.enabled) return false;
    const now = new Date();
    const currentMinutes = now.getHours() * 60 + now.getMinutes();
    const [startH = 0, startM = 0] = preferences.value.quietHours.start.split(':').map(Number);
    const [endH = 0, endM = 0] = preferences.value.quietHours.end.split(':').map(Number);
    const startMinutes = startH * 60 + startM;
    const endMinutes = endH * 60 + endM;
    if (startMinutes <= endMinutes) {
      return currentMinutes >= startMinutes && currentMinutes < endMinutes;
    }
    // Crosses midnight
    return currentMinutes >= startMinutes || currentMinutes < endMinutes;
  }

  function shouldNotify(eventType: string): boolean {
    if (!isSupported.value) return false;
    if (!preferences.value.enabled) return false;
    if (permissionStatus.value !== 'granted') return false;
    if (document.hidden === false) return false;
    if (isQuietHours()) return false;

    const eventMap: Record<string, boolean> = {
      prCreated: preferences.value.events.prCreated,
      reviewAdded: preferences.value.events.reviewAdded,
      commentAdded: preferences.value.events.commentAdded,
      threadResolved: preferences.value.events.threadResolved,
      prMerged: preferences.value.events.prMerged,
      checkFailed: preferences.value.events.checkFailed,
    };
    return eventMap[eventType] ?? false;
  }

  function isDuplicate(key: string): boolean {
    const last = lastNotified.get(key);
    if (last && Date.now() - last < DEDUP_WINDOW_MS) return true;
    return false;
  }

  function recordNotification(key: string) {
    lastNotified.set(key, Date.now());
    // Clean up old entries
    const now = Date.now();
    for (const [k, t] of lastNotified) {
      if (now - t > 60_000) lastNotified.delete(k);
    }
  }

  function fireNotification(title: string, body: string, tag: string, prId?: number) {
    try {
      const notification = new Notification(title, {
        body,
        icon: '/favicon.ico',
        tag,
      });
      notification.onclick = () => {
        window.focus();
        if (prId) router.push({ name: 'pr-detail', params: { id: prId } });
        notification.close();
      };
    } catch {
      // Notification constructor can throw in some contexts
    }
  }

  function isAuthor(pr: PullRequest | PRListUpdate): boolean {
    return pr.author?.toLowerCase() === authStore.username?.toLowerCase();
  }

  function isReviewer(pr: PullRequest | PRListUpdate): boolean {
    const username = authStore.username?.toLowerCase();
    if (!username) return false;
    const reviews = 'reviews' in pr ? pr.reviews : [];
    return reviews.some(r => r.reviewer?.toLowerCase() === username);
  }

  // --- Event-specific notification methods ---

  function notifyPRCreated(pr: PRListUpdate) {
    if (!shouldNotify('prCreated')) return;
    const key = `prCreated-${pr.id}`;
    if (isDuplicate(key)) return;
    recordNotification(key);
    fireNotification(
      `New PR: ${pr.title}`,
      `${pr.author} opened #${pr.gitHubId} in ${pr.repository}`,
      `pr-${pr.id}`,
      pr.id
    );
  }

  function notifyReviewAdded(notification: ReviewNotification, pr?: PullRequest | null) {
    if (!shouldNotify('reviewAdded')) return;
    if (!pr) return;
    const reviewer = notification.review.reviewer?.toLowerCase();
    const me = authStore.username?.toLowerCase();
    // Only notify if I'm the PR author or a requested reviewer — don't notify about my own reviews
    if (reviewer === me) return;
    if (!isAuthor(pr) && !isReviewer(pr)) return;

    const key = `reviewAdded-${pr.id}`;
    if (isDuplicate(key)) return;
    recordNotification(key);

    const state = notification.review.state?.toLowerCase().replace('_', ' ');
    fireNotification(
      `Review: ${state}`,
      `${notification.review.reviewer} ${state} on ${pr.title}`,
      `review-${pr.id}`,
      pr.id
    );
  }

  function notifyCommentAdded(notification: CommentNotification, pr?: PullRequest | null) {
    if (!shouldNotify('commentAdded')) return;
    if (!pr) return;
    const me = authStore.username?.toLowerCase();
    if (notification.comment.author?.toLowerCase() === me) return;
    if (!isAuthor(pr) && !isReviewer(pr)) return;

    const key = `commentAdded-${pr.id}`;
    if (isDuplicate(key)) return;
    recordNotification(key);

    fireNotification(
      'New comment',
      `${notification.comment.author} commented on ${pr.title}`,
      `comment-${pr.id}`,
      pr.id
    );
  }

  function notifyThreadResolved(notification: ThreadNotification, pr?: PullRequest | null) {
    if (!shouldNotify('threadResolved')) return;
    if (!pr) return;
    if (!isAuthor(pr) && !isReviewer(pr)) return;

    const key = `threadResolved-${notification.threadId}`;
    if (isDuplicate(key)) return;
    recordNotification(key);

    const action = notification.isResolved ? 'resolved' : 'unresolved';
    fireNotification(
      `Thread ${action}`,
      `Thread ${action} on ${pr.title}`,
      `thread-${notification.threadId}`,
      pr.id
    );
  }

  function notifyPRClosed(notification: PRClosedNotification, pr?: PullRequest | null) {
    if (!notification.wasMerged) return;
    if (!shouldNotify('prMerged')) return;
    if (!pr) return;
    if (!isAuthor(pr)) return;

    const key = `prMerged-${notification.pullRequestId}`;
    if (isDuplicate(key)) return;
    recordNotification(key);

    fireNotification(
      'PR Merged',
      `${pr.title} was merged`,
      `merged-${notification.pullRequestId}`,
      notification.pullRequestId
    );
  }

  function notifyCheckRunsUpdated(notification: CheckRunsNotification, pr?: PullRequest | null) {
    if (!shouldNotify('checkFailed')) return;
    if (!pr) return;
    if (!isAuthor(pr)) return;
    if (notification.checksStatus !== 'FAILURE') return;

    const key = `checkFailed-${pr.id}`;
    if (isDuplicate(key)) return;
    recordNotification(key);

    fireNotification(
      'CI Failed',
      `Checks failed on ${pr.title}`,
      `check-${pr.id}`,
      pr.id
    );
  }

  function sendTestNotification() {
    if (permissionStatus.value !== 'granted') return;
    fireNotification('Test notification', 'Desktop notifications are working!', 'test');
  }

  return {
    isSupported,
    permissionStatus,
    showPermissionBanner,
    preferences,
    requestPermission,
    dismissBanner,
    resetDismiss,
    loadPreferences,
    sendTestNotification,
    notifyPRCreated,
    notifyReviewAdded,
    notifyCommentAdded,
    notifyThreadResolved,
    notifyPRClosed,
    notifyCheckRunsUpdated,
  };
}
