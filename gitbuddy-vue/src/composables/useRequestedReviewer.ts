import { ref, type Ref } from 'vue';
import { apiService } from '../services/api';
import { useAuthStore } from '../stores/auth';

/**
 * Whether the signed-in user was asked to review this PR. Drives whether the description starts
 * collapsed — the people who want the diff first are the people asked to review it.
 *
 * Note: a review requested from a *team* cannot be matched to a username, so team-only requests
 * read as false.
 */
export function useRequestedReviewer(prId: Ref<number>) {
  const authStore = useAuthStore();
  const isRequestedReviewer = ref(false);

  const check = async () => {
    const me = authStore.username.toLowerCase();
    if (!me) return;

    try {
      const reviewers = await apiService.getReviewers(prId.value);
      isRequestedReviewer.value = reviewers.some(
        (r) => r.isRequested && r.username?.toLowerCase() === me
      );
    } catch (error) {
      // A failed lookup must never hide content — leave the description expanded.
      console.error('Failed to check requested-reviewer status:', error);
      isRequestedReviewer.value = false;
    }
  };

  return { isRequestedReviewer, check };
}
