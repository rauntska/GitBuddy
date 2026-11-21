<template>
  <div class="space-y-3">
    <div class="flex items-center justify-between">
      <h3 class="text-sm font-semibold uppercase tracking-wider text-slate-300">Automations</h3>
      <div v-if="!editing" class="flex items-center gap-2">
        <select
          v-model="selectedTemplate"
          class="bg-slate-900 border border-slate-700 rounded px-2 py-1 text-sm text-slate-200"
          @change="startFromTemplate"
        >
          <option value="">New from template…</option>
          <option v-for="t in templates" :key="t.key" :value="t.key">{{ t.name }}</option>
        </select>
        <button class="px-3 py-1 rounded text-sm border border-slate-700 bg-slate-800/60 text-slate-200 hover:bg-slate-800" @click="startBlank">
          New rule
        </button>
      </div>
    </div>

    <p class="text-xs text-slate-400">
      Rules run when GitHub events arrive. Actions are performed with the GitHub token of the admin who last saved the rule.
      Use dry-run first: it logs what would happen without touching GitHub or Teams.
    </p>

    <div v-if="error" class="text-sm text-red-400 border border-red-900/60 bg-red-950/30 rounded px-3 py-2">{{ error }}</div>
    <div v-if="loading" class="text-center py-8 text-sm text-slate-200/60">Loading...</div>

    <!-- Editor -->
    <form v-else-if="editing" class="space-y-3 border border-slate-800 rounded p-4" @submit.prevent="submit">
      <h4 class="text-sm font-semibold text-slate-200">{{ editingId === null ? 'New rule' : 'Edit rule' }}</h4>

      <div class="grid grid-cols-2 gap-3">
        <label class="text-xs text-slate-400 space-y-1">
          Name
          <input v-model="form.name" required class="w-full bg-slate-900 border border-slate-700 rounded px-2 py-1 text-sm text-slate-200" />
        </label>
        <label class="text-xs text-slate-400 space-y-1">
          Repository (blank = all)
          <input v-model="form.repositoryFullName" placeholder="owner/repo" class="w-full bg-slate-900 border border-slate-700 rounded px-2 py-1 text-sm text-slate-200" />
        </label>
        <label class="text-xs text-slate-400 space-y-1">
          Trigger
          <select v-model="form.triggerType" class="w-full bg-slate-900 border border-slate-700 rounded px-2 py-1 text-sm text-slate-200" @change="pruneConditions">
            <option v-for="t in catalog?.triggers ?? []" :key="t" :value="t">{{ t }}</option>
          </select>
        </label>
        <label class="text-xs text-slate-400 space-y-1">
          Minimum seconds between runs per PR
          <input v-model.number="form.minIntervalSeconds" type="number" min="0" class="w-full bg-slate-900 border border-slate-700 rounded px-2 py-1 text-sm text-slate-200" />
        </label>
      </div>

      <div class="space-y-2">
        <div class="flex items-center justify-between">
          <span class="text-xs uppercase tracking-wider text-slate-400">Conditions (all must match)</span>
          <button type="button" class="text-xs text-sky-400 hover:text-sky-300" @click="addCondition">+ Add condition</button>
        </div>
        <div v-if="form.conditions.length === 0" class="text-xs text-slate-500">No conditions — runs on every {{ form.triggerType }}.</div>
        <div v-for="(c, i) in form.conditions" :key="i" class="flex gap-2 items-center">
          <select v-model="c.type" class="bg-slate-900 border border-slate-700 rounded px-2 py-1 text-sm text-slate-200">
            <option v-for="t in allowedConditions" :key="t" :value="t">{{ t }}</option>
          </select>
          <input v-model="c.value" :placeholder="conditionHint(c.type)" class="flex-1 bg-slate-900 border border-slate-700 rounded px-2 py-1 text-sm text-slate-200" />
          <button type="button" class="text-xs text-slate-400 hover:text-red-400" @click="form.conditions.splice(i, 1)">Remove</button>
        </div>
      </div>

      <div class="space-y-2">
        <div class="flex items-center justify-between">
          <span class="text-xs uppercase tracking-wider text-slate-400">Actions (in order)</span>
          <button type="button" class="text-xs text-sky-400 hover:text-sky-300" @click="addAction">+ Add action</button>
        </div>
        <div v-for="(a, i) in form.actions" :key="i" class="border border-slate-800 rounded p-2 space-y-2">
          <div class="flex gap-2 items-center">
            <select v-model="a.type" class="bg-slate-900 border border-slate-700 rounded px-2 py-1 text-sm text-slate-200" @change="resetParams(a)">
              <option v-for="t in catalog?.actions ?? []" :key="t" :value="t">{{ t }}</option>
            </select>
            <button type="button" class="ml-auto text-xs text-slate-400 hover:text-red-400" @click="form.actions.splice(i, 1)">Remove</button>
          </div>
          <div class="grid grid-cols-2 gap-2">
            <template v-for="field in actionFields[a.type] ?? []" :key="field.key">
              <label class="text-xs text-slate-400 space-y-1" :class="field.wide ? 'col-span-2' : ''">
                {{ field.label }}
                <select v-if="field.options" v-model="a.params![field.key]" class="w-full bg-slate-900 border border-slate-700 rounded px-2 py-1 text-sm text-slate-200">
                  <option v-for="o in field.options" :key="o" :value="o">{{ o || 'channel (default)' }}</option>
                </select>
                <textarea v-else-if="field.wide" v-model="a.params![field.key]" rows="2" class="w-full bg-slate-900 border border-slate-700 rounded px-2 py-1 text-sm text-slate-200" />
                <input v-else v-model="a.params![field.key]" :placeholder="field.placeholder" class="w-full bg-slate-900 border border-slate-700 rounded px-2 py-1 text-sm text-slate-200" />
              </label>
            </template>
          </div>
        </div>
        <p class="text-[11px] text-slate-500">Text tokens: {{ tokens }}</p>
      </div>

      <div class="flex items-center gap-4 text-sm text-slate-300">
        <label class="flex items-center gap-2"><input v-model="form.dryRun" type="checkbox" /> Dry run (log only)</label>
        <label class="flex items-center gap-2"><input v-model="form.enabled" type="checkbox" /> Enabled</label>
      </div>

      <div v-if="formError" class="text-sm text-red-400">{{ formError }}</div>
      <div class="flex gap-2">
        <button type="submit" :disabled="saving" class="px-3 py-1 rounded text-sm border border-sky-700 bg-sky-900/40 text-sky-200 hover:bg-sky-900/70 disabled:opacity-50">
          {{ saving ? 'Saving…' : 'Save rule' }}
        </button>
        <button type="button" class="px-3 py-1 rounded text-sm border border-slate-700 text-slate-300 hover:bg-slate-800/60" @click="cancel">Cancel</button>
      </div>
    </form>

    <!-- List -->
    <div v-else>
      <div v-if="rules.length === 0" class="text-sm text-slate-400 text-center py-8">
        No automation rules yet. Start from a template above.
      </div>
      <table v-else class="w-full">
        <thead>
          <tr class="border-b border-slate-800">
            <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Rule</th>
            <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Trigger</th>
            <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Scope</th>
            <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Last run</th>
            <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Runs</th>
            <th class="px-3 py-2"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in rules" :key="r.id" class="border-b border-slate-800/60">
            <td class="px-3 py-2 text-sm text-slate-200">
              {{ r.name }}
              <span v-if="r.dryRun" class="ml-2 text-[11px] px-1.5 py-0.5 rounded border border-amber-700/60 text-amber-300">dry run</span>
            </td>
            <td class="px-3 py-2 text-sm text-slate-300">{{ r.triggerType }}</td>
            <td class="px-3 py-2 text-sm text-slate-300">{{ r.repositoryFullName ?? 'All repositories' }}</td>
            <td class="px-3 py-2 text-sm text-slate-400">{{ r.lastTriggeredAt ? new Date(r.lastTriggeredAt).toLocaleString() : '—' }}</td>
            <td class="px-3 py-2 text-sm text-slate-400 tabular-nums">{{ r.triggerCount }}</td>
            <td class="px-3 py-2 text-sm text-right whitespace-nowrap space-x-3">
              <button class="text-slate-300 hover:text-slate-100" @click="setEnabled(r, !r.enabled)">{{ r.enabled ? 'Disable' : 'Enable' }}</button>
              <button class="text-slate-300 hover:text-slate-100" @click="edit(r)">Edit</button>
              <button class="text-slate-300 hover:text-slate-100" @click="showLog(r)">Log</button>
              <button class="text-slate-400 hover:text-red-400" @click="confirmRemove(r)">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>

      <div v-if="logRule" class="mt-4 border border-slate-800 rounded p-3 space-y-2">
        <div class="flex items-center justify-between">
          <h4 class="text-sm font-semibold text-slate-200">Run log — {{ logRule.name }}</h4>
          <button class="text-xs text-slate-400 hover:text-slate-200" @click="logRule = null">Close</button>
        </div>
        <div v-if="runs.length === 0" class="text-sm text-slate-500">No runs recorded yet.</div>
        <ul v-else class="space-y-2">
          <li v-for="run in runs" :key="run.id" class="text-sm">
            <div class="text-slate-300">
              {{ new Date(run.triggeredAt).toLocaleString() }} · {{ run.repository }}#{{ run.pullRequestNumber }} {{ run.pullRequestTitle }}
              <span v-if="run.dryRun" class="ml-1 text-[11px] text-amber-300">(dry run)</span>
              <span v-if="!run.conditionsMatched" class="ml-1 text-[11px] text-slate-500">(not matched)</span>
            </div>
            <ul class="pl-4 text-slate-400 list-disc">
              <li v-for="(line, i) in run.actionsTaken" :key="i">{{ line }}</li>
            </ul>
            <div v-if="run.error" class="pl-4 text-red-400">{{ run.error }}</div>
          </li>
        </ul>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { useAutomationRules } from '../../composables/useAutomationRules';
import type { AutomationAction, AutomationRule, AutomationRuleTemplate, UpsertAutomationRuleRequest } from '../../types/automation';

const { rules, templates, catalog, runs, loading, error, load, save, remove, setEnabled, loadRuns } = useAutomationRules();

const editing = ref(false);
const editingId = ref<number | null>(null);
const saving = ref(false);
const formError = ref<string | null>(null);
const selectedTemplate = ref('');
const logRule = ref<AutomationRule | null>(null);

const tokens = '{{author}} {{prTitle}} {{prUrl}} {{reviewers}} {{checkName}} {{label}} {{repository}}';

const emptyForm = (): UpsertAutomationRuleRequest => ({
  repositoryFullName: null,
  name: '',
  enabled: false,
  dryRun: true,
  triggerType: 'PROpened',
  minIntervalSeconds: 0,
  conditions: [],
  actions: [],
});

const form = reactive<UpsertAutomationRuleRequest>(emptyForm());

interface ActionField { key: string; label: string; placeholder?: string; wide?: boolean; options?: string[] }

const actionFields: Record<string, ActionField[]> = {
  AddComment: [{ key: 'template', label: 'Comment', wide: true }],
  AddLabel: [{ key: 'label', label: 'Label' }],
  RemoveLabel: [{ key: 'label', label: 'Label' }],
  SetLabelExclusive: [
    { key: 'group', label: 'Label group prefix', placeholder: 'size/' },
    { key: 'label', label: 'Label to set', placeholder: 'size/M' },
  ],
  RequestReview: [
    { key: 'users', label: 'Users (comma separated)', placeholder: 'octocat, hubot' },
    { key: 'teams', label: 'Team slugs (comma separated)', placeholder: 'design' },
  ],
  PostTeamsMessage: [
    { key: 'recipient', label: 'Recipient', options: ['channel', 'author', 'reviewers'] },
    { key: 'title', label: 'Title' },
    { key: 'template', label: 'Message', wide: true },
  ],
  SetPriority: [{ key: 'priority', label: 'Priority (0 low – 3 urgent)', placeholder: '2' }],
};

const conditionHints: Record<string, string> = {
  Label: 'label name',
  CheckName: 'check name',
  CheckConclusion: 'failure',
  AllRequiredChecksPassed: 'optional: comma separated override list',
  ConsecutiveFailures: '2',
  ApprovalCount: '>=1',
  HasRequestedReviewers: 'true or false',
  AuthorPattern: 'dependabot*',
  BaseBranch: 'release/*',
  ChangedFileGlob: '**/Migrations/**',
  ChangedFilesAllMatch: 'gitbuddy-vue/**',
  PrSizeLines: '<=50',
  ReviewState: 'changes_requested',
  MergeableState: 'CONFLICTING or NOT_CONFLICTING',
};

const conditionHint = (type: string) => conditionHints[type] ?? 'value';

const allowedConditions = computed(() => catalog.value?.conditionsByTrigger[form.triggerType] ?? []);

function assignForm(data: UpsertAutomationRuleRequest) {
  Object.assign(form, JSON.parse(JSON.stringify(data)));
  // Make sure every action has a params object so v-model bindings work.
  form.actions.forEach((a) => { a.params = a.params ?? {}; });
}

function startBlank() {
  assignForm(emptyForm());
  openEditor(null);
}

function startFromTemplate() {
  const t: AutomationRuleTemplate | undefined = templates.value.find((x) => x.key === selectedTemplate.value);
  selectedTemplate.value = '';
  if (!t) return;
  assignForm({
    ...emptyForm(),
    name: t.name,
    triggerType: t.triggerType,
    minIntervalSeconds: t.minIntervalSeconds,
    conditions: t.conditions,
    actions: t.actions,
  });
  openEditor(null);
}

function edit(rule: AutomationRule) {
  assignForm({
    repositoryFullName: rule.repositoryFullName,
    name: rule.name,
    enabled: rule.enabled,
    dryRun: rule.dryRun,
    triggerType: rule.triggerType,
    minIntervalSeconds: rule.minIntervalSeconds,
    conditions: rule.conditions,
    actions: rule.actions,
  });
  openEditor(rule.id);
}

function openEditor(id: number | null) {
  editingId.value = id;
  formError.value = null;
  logRule.value = null;
  editing.value = true;
}

function cancel() {
  editing.value = false;
}

function addCondition() {
  form.conditions.push({ type: allowedConditions.value[0] ?? 'Label', value: '' });
}

function pruneConditions() {
  form.conditions = form.conditions.filter((c) => allowedConditions.value.includes(c.type));
}

function addAction() {
  const type = catalog.value?.actions[0] ?? 'AddLabel';
  const action: AutomationAction = { type, params: {} };
  resetParams(action);
  form.actions.push(action);
}

function resetParams(action: AutomationAction) {
  const params: Record<string, string> = {};
  for (const field of actionFields[action.type] ?? []) params[field.key] = field.options ? (field.options[0] ?? '') : '';
  action.params = params;
}

async function submit() {
  saving.value = true;
  formError.value = null;
  const payload: UpsertAutomationRuleRequest = {
    ...JSON.parse(JSON.stringify(form)),
    repositoryFullName: form.repositoryFullName?.trim() || null,
  };
  const message = await save(editingId.value, payload);
  saving.value = false;
  if (message) formError.value = message;
  else editing.value = false;
}

async function showLog(rule: AutomationRule) {
  logRule.value = rule;
  await loadRuns(rule.id);
}

async function confirmRemove(rule: AutomationRule) {
  if (window.confirm(`Delete rule "${rule.name}" and its run log?`)) await remove(rule.id);
}

onMounted(load);
</script>
