import { ref } from 'vue';
import { apiService } from '../services/api';
import type {
  AutomationCatalog,
  AutomationRule,
  AutomationRuleTemplate,
  AutomationRunLog,
  UpsertAutomationRuleRequest,
} from '../types/automation';

function extractError(e: unknown, fallback: string): string {
  const data = (e as { response?: { data?: { errors?: string[]; message?: string } } })?.response?.data;
  if (data?.errors?.length) return data.errors.join(' ');
  return data?.message ?? (e as Error)?.message ?? fallback;
}

export function useAutomationRules() {
  const rules = ref<AutomationRule[]>([]);
  const templates = ref<AutomationRuleTemplate[]>([]);
  const catalog = ref<AutomationCatalog | null>(null);
  const runs = ref<AutomationRunLog[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);

  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const [r, t, c] = await Promise.all([
        apiService.getAutomationRules(),
        apiService.getAutomationTemplates(),
        apiService.getAutomationCatalog(),
      ]);
      rules.value = r;
      templates.value = t;
      catalog.value = c;
    } catch (e) {
      error.value = extractError(e, 'Failed to load automation rules');
    } finally {
      loading.value = false;
    }
  }

  /** Returns an error message, or null on success. */
  async function save(id: number | null, data: UpsertAutomationRuleRequest): Promise<string | null> {
    try {
      const saved = id === null
        ? await apiService.createAutomationRule(data)
        : await apiService.updateAutomationRule(id, data);
      const index = rules.value.findIndex((r) => r.id === saved.id);
      if (index >= 0) rules.value[index] = saved;
      else rules.value.push(saved);
      rules.value.sort((a, b) => a.name.localeCompare(b.name));
      return null;
    } catch (e) {
      return extractError(e, 'Failed to save rule');
    }
  }

  async function remove(id: number) {
    try {
      await apiService.deleteAutomationRule(id);
      rules.value = rules.value.filter((r) => r.id !== id);
    } catch (e) {
      error.value = extractError(e, 'Failed to delete rule');
    }
  }

  async function setEnabled(rule: AutomationRule, enabled: boolean) {
    try {
      const updated = await apiService.setAutomationRuleEnabled(rule.id, enabled);
      const index = rules.value.findIndex((r) => r.id === rule.id);
      if (index >= 0) rules.value[index] = updated;
    } catch (e) {
      error.value = extractError(e, 'Failed to update rule');
    }
  }

  async function loadRuns(id: number) {
    try {
      runs.value = await apiService.getAutomationRuleRuns(id);
    } catch (e) {
      runs.value = [];
      error.value = extractError(e, 'Failed to load run log');
    }
  }

  return { rules, templates, catalog, runs, loading, error, load, save, remove, setEnabled, loadRuns };
}
