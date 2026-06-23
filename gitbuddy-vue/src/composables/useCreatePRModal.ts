import { ref } from 'vue';
import type { BranchWithoutPR } from '../types';

export interface PrefillData {
  repoFullName: string;
  owner: string;
  repo: string;
  headBranch: string;
  baseBranch: string;
}

const isOpen = ref(false);
const prefill = ref<PrefillData | null>(null);

export function useCreatePRModal() {
  const openWithPrefill = (branch: BranchWithoutPR) => {
    prefill.value = {
      repoFullName: branch.repoFullName,
      owner: branch.owner,
      repo: branch.repo,
      headBranch: branch.branchName,
      baseBranch: branch.defaultBranch,
    };
    isOpen.value = true;
  };

  const open = () => {
    prefill.value = null;
    isOpen.value = true;
  };

  const close = () => {
    isOpen.value = false;
    prefill.value = null;
  };

  const consumePrefill = () => {
    const data = prefill.value;
    prefill.value = null;
    return data;
  };

  return {
    isOpen,
    prefill,
    open,
    openWithPrefill,
    close,
    consumePrefill,
  };
}
