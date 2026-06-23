import { ref, computed } from 'vue';
import { apiService } from '../services/api';
import type { BranchWithoutPR } from '../types';

const branches = ref<BranchWithoutPR[]>([]);
const loading = ref(false);
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
    error,
    fetchBranches,
  };
}
