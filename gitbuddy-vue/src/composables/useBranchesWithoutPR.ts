import { ref, computed } from 'vue';
import { apiService } from '../services/api';
import type { BranchWithoutPR } from '../types';

const branches = ref<BranchWithoutPR[]>([]);
const loading = ref(false);
const refreshing = ref(false);
const error = ref<string | null>(null);

export function useBranchesWithoutPR() {
  const fetchBranches = async () => {
    loading.value = true;
    error.value = null;
    try {
      branches.value = await apiService.getBranchesWithoutPR();
    } catch (err) {
      error.value = 'Failed to fetch branches without PRs';
      console.error(err);
    } finally {
      loading.value = false;
    }
  };

  const manualRefresh = async () => {
    if (refreshing.value) return;
    refreshing.value = true;
    try {
      await apiService.refreshBranchesWithoutPR();
      error.value = null;
    } catch (err) {
      console.error('Failed to trigger branches-without-prs refresh', err);
      error.value = 'Failed to trigger branches-without-prs refresh';
    } finally {
      // The worker cycle takes a moment; keep the spinner briefly so the user sees feedback,
      // then let the next poll or SignalR event update the list.
      setTimeout(() => { refreshing.value = false; }, 1500);
    }
  };

  const applyBranchResolved = (repoFullName: string, branchName: string) => {
    branches.value = branches.value.filter(
      b => !(b.repoFullName === repoFullName && b.branchName === branchName)
    );
  };

  const applyBranchAdded = (branch: BranchWithoutPR) => {
    const idx = branches.value.findIndex(
      b => b.repoFullName === branch.repoFullName && b.branchName === branch.branchName
    );
    if (idx >= 0) {
      const next = [...branches.value];
      next[idx] = branch;
      branches.value = next;
    } else {
      branches.value = [branch, ...branches.value];
    }
  };

  const groupedByRepo = computed(() => {
    const groups: Record<string, BranchWithoutPR[]> = {};
    for (const branch of branches.value) {
      if (!groups[branch.repoFullName]) {
        groups[branch.repoFullName] = [];
      }
      groups[branch.repoFullName]!.push(branch);
    }
    return Object.entries(groups)
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([repo, repoBranches]) => ({ repo, branches: repoBranches }));
  });

  return {
    branches,
    groupedByRepo,
    loading,
    refreshing,
    error,
    fetchBranches,
    manualRefresh,
    applyBranchResolved,
    applyBranchAdded,
  };
}
