import { computed, ref, watch } from 'vue';
import { apiService } from '../services/api';
import type { AnalyticsPreset, AuthorOption, HealthAnalytics, ReviewerAnalytics, ThroughputAnalytics } from '../types';

function presetToRange(preset: AnalyticsPreset): { from?: string; to?: string } {
  const now = new Date();
  const to = now.toISOString().slice(0, 10);
  if (preset === 'all') {
    return { from: '2000-01-01', to };
  }
  const days = preset === '7d' ? 7 : preset === '30d' ? 30 : 90;
  const from = new Date(now.getTime() - days * 24 * 60 * 60 * 1000);
  return { from: from.toISOString().slice(0, 10), to };
}

const preset = ref<AnalyticsPreset | 'custom'>('30d');
const customFrom = ref<string>('');
const customTo = ref<string>('');
const selectedAuthors = ref<string[]>([]);

const throughput = ref<ThroughputAnalytics | null>(null);
const reviewers = ref<ReviewerAnalytics | null>(null);
const health = ref<HealthAnalytics | null>(null);

const loading = ref(false);
const error = ref<string | null>(null);
let initialized = false;

const activeRange = computed<{ from?: string; to?: string }>(() => {
  if (preset.value === 'custom') {
    return { from: customFrom.value || undefined, to: customTo.value || undefined };
  }
  return presetToRange(preset.value);
});

const availableAuthors = computed<AuthorOption[]>(() => reviewers.value?.authors ?? []);

async function refresh() {
  loading.value = true;
  error.value = null;
  const { from, to } = activeRange.value;
  const authors = selectedAuthors.value.length > 0 ? selectedAuthors.value : undefined;
  const results = await Promise.allSettled([
    apiService.getAnalyticsThroughput(from, to, authors),
    apiService.getAnalyticsReviewers(from, to, authors),
    apiService.getAnalyticsHealth(from, to, authors),
  ]);
  if (results[0].status === 'fulfilled') throughput.value = results[0].value;
  if (results[1].status === 'fulfilled') reviewers.value = results[1].value;
  if (results[2].status === 'fulfilled') health.value = results[2].value;
  const firstError = results.find((r) => r.status === 'rejected') as PromiseSettledResult<unknown> | undefined;
  if (firstError && firstError.status === 'rejected') {
    error.value = String(firstError.reason ?? 'Failed to load analytics');
  }
  loading.value = false;
}

function setPreset(p: AnalyticsPreset) {
  preset.value = p;
}

function setCustomRange(from: string, to: string) {
  preset.value = 'custom';
  customFrom.value = from;
  customTo.value = to;
}

function setSelectedAuthors(next: string[]) {
  selectedAuthors.value = next;
}

watch([activeRange, selectedAuthors], () => { void refresh(); }, { deep: true });

if (!initialized) {
  initialized = true;
  void refresh();
}

export function useAnalytics() {
  return {
    preset,
    customFrom,
    customTo,
    activeRange,
    selectedAuthors,
    availableAuthors,
    throughput,
    reviewers,
    health,
    loading,
    error,
    refresh,
    setPreset,
    setCustomRange,
    setSelectedAuthors,
  };
}
