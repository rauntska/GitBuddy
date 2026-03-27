import { ref, computed, watch } from 'vue';
import { apiService } from '../services/api';
import type { Repository, Branch, BranchComparison, CreatePullRequestResult, Settings } from '../types';

export function useCreatePR() {
  const repositories = ref<Repository[]>([]);
  const branches = ref<Branch[]>([]);
  const comparison = ref<BranchComparison | null>(null);
  
  const selectedRepository = ref<Repository | null>(null);
  const selectedBaseBranch = ref<string>('');
  const selectedHeadBranch = ref<string>('');
  
  const title = ref('');
  const body = ref('');
  const isDraft = ref(false);
  
  const loadingRepositories = ref(false);
  const loadingBranches = ref(false);
  const loadingComparison = ref(false);
  const creating = ref(false);
  const error = ref<string | null>(null);
  const result = ref<CreatePullRequestResult | null>(null);
  const settings = ref<Settings | null>(null);

  const isValid = computed(() => {
    return (
      selectedRepository.value &&
      selectedBaseBranch.value &&
      selectedHeadBranch.value &&
      selectedBaseBranch.value !== selectedHeadBranch.value &&
      title.value.trim().length > 0
    );
  });

  const canCreate = computed(() => {
    return isValid.value && !creating.value;
  });

  const hasChanges = computed(() => {
    return comparison.value && comparison.value.aheadBy > 0;
  });

  const totalAdditions = computed(() => comparison.value?.files.reduce((sum, f) => sum + (f.additions || 0), 0) || 0);
  const totalDeletions = computed(() => comparison.value?.files.reduce((sum, f) => sum + (f.deletions || 0), 0) || 0);

  const fetchSettings = async () => {
    try {
      settings.value = await apiService.getSettings();
    } catch {
      settings.value = null;
    }
  };

  const fetchRepositories = async () => {
    loadingRepositories.value = true;
    error.value = null;
    try {
      let repos: Repository[];
      
      if (settings.value?.organization) {
        const orgResult = await apiService.getOrganizationRepositories();
        repos = orgResult.repositories;
      } else {
        repos = await apiService.getRepositories();
      }

      repositories.value = repos.sort((a, b) => a.fullName.localeCompare(b.fullName));
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to fetch repositories';
      repositories.value = [];
    } finally {
      loadingRepositories.value = false;
    }
  };

  const fetchBranches = async () => {
    if (!selectedRepository.value) {
      branches.value = [];
      return;
    }

    loadingBranches.value = true;
    error.value = null;
    try {
      branches.value = await apiService.getBranches(
        selectedRepository.value.owner,
        selectedRepository.value.name
      );
      
      if (branches.value.length > 0) {
        const defaultBranch = selectedRepository.value.defaultBranch || 'main';
        const matchingBranch = branches.value.find(b => b.name === defaultBranch);
        selectedBaseBranch.value = matchingBranch?.name ?? branches.value[0]!.name;
      }
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to fetch branches';
      branches.value = [];
    } finally {
      loadingBranches.value = false;
    }
  };

  const fetchComparison = async () => {
    if (!selectedRepository.value || !selectedBaseBranch.value || !selectedHeadBranch.value) {
      comparison.value = null;
      return;
    }

    if (selectedBaseBranch.value === selectedHeadBranch.value) {
      comparison.value = null;
      return;
    }

    loadingComparison.value = true;
    error.value = null;
    try {
      comparison.value = await apiService.compareBranches(
        selectedRepository.value.owner,
        selectedRepository.value.name,
        selectedBaseBranch.value,
        selectedHeadBranch.value
      );
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to compare branches';
      comparison.value = null;
    } finally {
      loadingComparison.value = false;
    }
  };

  const createPullRequest = async (): Promise<CreatePullRequestResult | null> => {
    if (!isValid.value || !selectedRepository.value) {
      return null;
    }

    creating.value = true;
    error.value = null;
    result.value = null;

    try {
      result.value = await apiService.createPullRequest({
        owner: selectedRepository.value.owner,
        repository: selectedRepository.value.name,
        title: title.value.trim(),
        body: body.value.trim() || undefined,
        head: selectedHeadBranch.value,
        base: selectedBaseBranch.value,
        draft: isDraft.value
      });

      return result.value;
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to create pull request';
      return null;
    } finally {
      creating.value = false;
    }
  };

  const reset = () => {
    selectedRepository.value = null;
    selectedBaseBranch.value = '';
    selectedHeadBranch.value = '';
    title.value = '';
    body.value = '';
    isDraft.value = false;
    branches.value = [];
    comparison.value = null;
    error.value = null;
    result.value = null;
  };

  const init = async () => {
    await fetchSettings();
    await fetchRepositories();
  };

  watch(selectedRepository, () => {
    selectedBaseBranch.value = '';
    selectedHeadBranch.value = '';
    comparison.value = null;
    fetchBranches();
  });

  watch([selectedBaseBranch, selectedHeadBranch], () => {
    fetchComparison();
  });

  return {
    repositories,
    branches,
    comparison,
    selectedRepository,
    selectedBaseBranch,
    selectedHeadBranch,
    title,
    body,
    isDraft,
    loadingRepositories,
    loadingBranches,
    loadingComparison,
    creating,
    error,
    result,
    isValid,
    canCreate,
    hasChanges,
    totalAdditions,
    totalDeletions,
    init,
    fetchRepositories,
    fetchBranches,
    fetchComparison,
    createPullRequest,
    reset
  };
}
