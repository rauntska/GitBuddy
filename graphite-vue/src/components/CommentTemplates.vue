<template>
  <Teleport to="body">
    <Transition name="modal">
      <div
        v-if="show"
        class="fixed inset-0 bg-black/50 flex items-center justify-center z-50"
        @click.self="$emit('close')"
      >
        <div class="bg-slate-800 rounded-lg shadow-2xl w-full max-w-2xl max-h-[80vh] flex flex-col">
          <!-- Header -->
          <div class="flex items-center justify-between p-4 border-b border-slate-700">
            <h2 class="text-lg font-semibold text-slate-100">Comment Templates</h2>
            <button
              @click="$emit('close')"
              class="p-1 rounded hover:bg-slate-700 text-slate-400 hover:text-white transition-colors"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
              </button>
            </button>
          </div>

          <!-- Search -->
          <div class="p-4 border-b border-slate-700">
            <div class="relative">
              <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path>
              </svg>
              <input
                v-model="searchQuery"
                type="text"
                placeholder="Search templates..."
                class="w-full pl-10 pr-4 py-2 bg-slate-900 border border-slate-700 rounded-lg text-slate-100 placeholder-slate-500 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none"
              />
            </div>

            <!-- Tag Filters -->
            <div class="flex gap-2 mt-3 flex-wrap">
              <button
                @click="selectedTag = null"
                :class="[
                  'px-3 py-1 text-xs rounded-full transition-colors',
                  !selectedTag ? 'bg-blue-600 text-white' : 'bg-slate-700 text-slate-300 hover:bg-slate-600'
                ]"
              >
                All
              </button>
              <button
                v-for="tag in availableTags"
                :key="tag"
                @click="selectedTag = tag"
                :class="[
                  'px-3 py-1 text-xs rounded-full transition-colors',
                  selectedTag === tag ? 'bg-blue-600 text-white' : 'bg-slate-700 text-slate-300 hover:bg-slate-600'
                ]"
              >
                {{ tag }}
              </button>
            </div>
          </div>

          <!-- Templates List -->
          <div class="flex-1 overflow-auto p-4">
            <div v-if="loading" class="flex items-center justify-center py-12">
              <svg class="animate-spin w-8 h-8 text-slate-400" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
            </div>

            <div v-else-if="filteredTemplates.length === 0" class="text-center py-12">
              <svg class="w-12 h-12 mx-auto text-slate-600 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
              </svg>
              <p class="text-slate-400">No templates found</p>
              <p class="text-sm text-slate-500 mt-1">Create a template to reuse common feedback</p>
            </div>

            <div v-else class="space-y-2">
              <button
                v-for="template in filteredTemplates"
                :key="template.id"
                @click="selectTemplate(template)"
                class="w-full text-left p-3 bg-slate-700/50 hover:bg-slate-700 rounded-lg border border-slate-600 hover:border-slate-500 transition-colors group"
              >
                <div class="flex items-start gap-3">
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2">
                      <h3 class="font-medium text-slate-100 truncate">{{ template.title }}</h3>
                      <span
                        v-if="template.isOrganizationTemplate"
                        class="px-1.5 py-0.5 text-xs bg-purple-900/50 text-purple-300 rounded"
                      >
                        Org
                      </span>
                    </div>
                    <p class="text-sm text-slate-400 mt-1 line-clamp-2">{{ template.body }}</p>
                    <div class="flex items-center gap-3 mt-2 text-xs text-slate-500">
                      <span v-if="template.tags" class="flex items-center gap-1">
                        <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z"></path>
                        </svg>
                        {{ template.tags }}
                      </span>
                      <span>Used {{ template.usageCount }} times</span>
                    </div>
                  </div>
                  <svg class="w-5 h-5 text-slate-500 group-hover:text-blue-400 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
                  </svg>
                </div>
              </button>
            </div>
          </div>

          <!-- Footer -->
          <div class="flex items-center justify-between p-4 border-t border-slate-700">
            <button
              @click="showCreateForm = true"
              class="flex items-center gap-2 px-3 py-1.5 text-sm bg-slate-700 hover:bg-slate-600 text-slate-300 rounded-lg transition-colors"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
              </svg>
              New Template
            </button>
            <button
              @click="$emit('close')"
              class="px-4 py-2 text-sm text-slate-400 hover:text-white transition-colors"
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- Create Template Modal -->
    <Transition name="modal">
      <div
        v-if="showCreateForm"
        class="fixed inset-0 bg-black/50 flex items-center justify-center z-60"
        @click.self="showCreateForm = false"
      >
        <div class="bg-slate-800 rounded-lg shadow-2xl w-full max-w-md p-6">
          <h3 class="text-lg font-semibold text-slate-100 mb-4">Create Template</h3>

          <form @submit.prevent="createTemplate" class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-slate-300 mb-1">Title</label>
              <input
                v-model="newTemplate.title"
                type="text"
                required
                placeholder="Template title"
                class="w-full px-3 py-2 bg-slate-900 border border-slate-700 rounded-lg text-slate-100 placeholder-slate-500 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-slate-300 mb-1">Body</label>
              <textarea
                v-model="newTemplate.body"
                required
                rows="4"
                placeholder="Template content..."
                class="w-full px-3 py-2 bg-slate-900 border border-slate-700 rounded-lg text-slate-100 placeholder-slate-500 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none resize-y font-mono text-sm"
              ></textarea>
            </div>

            <div>
              <label class="block text-sm font-medium text-slate-300 mb-1">Tags (comma-separated)</label>
              <input
                v-model="newTemplate.tags"
                type="text"
                placeholder="nitpick, security, performance"
                class="w-full px-3 py-2 bg-slate-900 border border-slate-700 rounded-lg text-slate-100 placeholder-slate-500 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none"
              />
            </div>

            <div class="flex justify-end gap-3 pt-2">
              <button
                type="button"
                @click="showCreateForm = false"
                class="px-4 py-2 text-sm text-slate-400 hover:text-white transition-colors"
              >
                Cancel
              </button>
              <button
                type="submit"
                :disabled="creating"
                class="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-500 text-white rounded-lg transition-colors disabled:opacity-50"
              >
                {{ creating ? 'Creating...' : 'Create' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import api from '../utils/api';

interface CommentTemplate {
  id: number;
  title: string;
  body: string;
  tags?: string;
  usageCount: number;
  createdAt: string;
  lastUsedAt?: string;
  isOrganizationTemplate: boolean;
}

const props = defineProps<{
  show: boolean;
}>();

const emit = defineEmits<{
  close: [];
  select: [template: CommentTemplate];
}>();

const templates = ref<CommentTemplate[]>([]);
const loading = ref(false);
const searchQuery = ref('');
const selectedTag = ref<string | null>(null);
const showCreateForm = ref(false);
const creating = ref(false);

const newTemplate = ref({
  title: '',
  body: '',
  tags: ''
});

const availableTags = computed(() => {
  const tags = new Set<string>();
  templates.value.forEach(t => {
    if (t.tags) {
      t.tags.split(',').forEach(tag => tags.add(tag.trim().toLowerCase()));
    }
  });
  return Array.from(tags).sort();
});

const filteredTemplates = computed(() => {
  let result = templates.value;

  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase();
    result = result.filter(t =>
      t.title.toLowerCase().includes(query) ||
      t.body.toLowerCase().includes(query)
    );
  }

  if (selectedTag.value) {
    result = result.filter(t =>
      t.tags?.toLowerCase().includes(selectedTag.value!.toLowerCase())
    );
  }

  return result.sort((a, b) => b.usageCount - a.usageCount);
});

const loadTemplates = async () => {
  loading.value = true;
  try {
    const response = await api.get('/api/comment-templates');
    templates.value = response.data;
  } catch (error) {
    console.error('Failed to load templates:', error);
  } finally {
    loading.value = false;
  }
};

const selectTemplate = async (template: CommentTemplate) => {
  // Record usage
  try {
    await api.post(`/api/comment-templates/${template.id}/use`);
  } catch (error) {
    console.error('Failed to record template usage:', error);
  }

  emit('select', template);
  emit('close');
};

const createTemplate = async () => {
  creating.value = true;
  try {
    const response = await api.post('/api/comment-templates', newTemplate.value);
    templates.value.push(response.data);
    showCreateForm.value = false;
    newTemplate.value = { title: '', body: '', tags: '' };
  } catch (error) {
    console.error('Failed to create template:', error);
  } finally {
    creating.value = false;
  }
};

watch(() => props.show, (show) => {
  if (show) {
    loadTemplates();
  }
});

onMounted(() => {
  if (props.show) {
    loadTemplates();
  }
});
</script>

<style scoped>
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
