<template>
  <div class="space-y-3">
    <div>
      <h3 class="text-sm font-semibold uppercase tracking-wider text-slate-300 mb-3">Administration</h3>

      <div class="flex flex-wrap gap-1 border-b border-slate-800 py-1 mb-3">
        <button
          v-for="tab in tabs"
          :key="tab.id"
          @click="activeTab = tab.id"
          :class="[
            'px-3 py-1.5 rounded text-sm transition-all duration-150 ease-out flex items-center gap-2 border',
            activeTab === tab.id
              ? 'bg-slate-800/60 border-slate-700 text-slate-200'
              : 'border-transparent text-slate-300 hover:bg-slate-800/40 hover:border-slate-700'
          ]"
        >
          {{ tab.label }}
          <span v-if="tab.count !== undefined" class="bg-slate-800 border border-slate-700/60 text-slate-400 font-mono tabular-nums text-[11px] px-1.5 py-0.5 rounded">
            {{ tab.count }}
          </span>
        </button>
      </div>

      <div v-if="loading" class="text-center py-8 text-sm text-slate-200/60">Loading...</div>

      <div v-else-if="activeTab === 'users'">
        <div class="flex justify-between items-center mb-2">
          <h4 class="text-sm font-semibold uppercase tracking-wider text-slate-300">Users</h4>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full">
            <thead>
              <tr class="border-b border-slate-800">
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Username</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Email</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Role</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Created</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Last Login</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="user in users" :key="user.id" class="border-b border-slate-800 hover:bg-slate-800/40 hover:border-slate-700 transition-all duration-150 ease-out">
                <td class="px-3 py-2">
                  <div class="flex items-center gap-2">
                    <img v-if="user.avatarUrl" :src="user.avatarUrl" :alt="user.username" class="w-6 h-6 rounded-full" />
                    <span class="text-slate-200 font-mono">{{ user.username }}</span>
                  </div>
                </td>
                <td class="px-3 py-2 text-slate-200/60">{{ user.email }}</td>
                <td class="px-3 py-2">
                  <select
                    :value="user.role"
                    @change="updateUserRole(user.id, ($event.target as HTMLSelectElement).value as UserRole)"
                    :disabled="user.id === currentUserId"
                    class="px-2 py-1 rounded border border-slate-700 bg-slate-900/60 text-slate-200 text-sm focus:outline-none focus:border-slate-600 disabled:opacity-50"
                  >
                    <option value="Developer">Developer</option>
                    <option value="Admin">Admin</option>
                  </select>
                </td>
                <td class="px-3 py-2 text-slate-200/60 text-xs font-mono tabular-nums">{{ formatDate(user.createdAt) }}</td>
                <td class="px-3 py-2 text-slate-200/60 text-xs font-mono tabular-nums">{{ user.lastLoginAt ? formatDate(user.lastLoginAt) : 'Never' }}</td>
                <td class="px-3 py-2">
                  <button
                    v-if="user.id !== currentUserId"
                    @click="deleteUser(user.id)"
                    class="px-2.5 py-1 rounded border border-red-900/40 bg-red-950/20 text-red-400 text-xs hover:bg-red-950/40 hover:border-red-900/60 transition-colors"
                  >
                    Delete
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div v-else-if="activeTab === 'invitations'">
        <div class="flex justify-between items-center mb-2">
          <h4 class="text-sm font-semibold uppercase tracking-wider text-slate-300">Invitations</h4>
          <button @click="showCreateInvitation = true" class="px-3 py-1.5 rounded bg-slate-200 text-slate-900 hover:bg-white text-sm transition-colors">
            Create Invitation
          </button>
        </div>

        <div v-if="showCreateInvitation" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50" @click.self="showCreateInvitation = false">
          <div class="bg-slate-900 p-6 rounded border border-slate-800 w-full max-w-md">
            <h5 class="text-sm font-semibold uppercase tracking-wider text-slate-300 mb-4">Create Invitation</h5>
            <form @submit.prevent="createInvitation">
              <div class="mb-4">
                <label class="block text-xs uppercase tracking-wider text-slate-500 mb-1.5">Email *</label>
                <input v-model="newInvitation.email" type="email" required class="w-full px-3 py-2 rounded bg-slate-900/60 border border-slate-700 text-slate-200 placeholder-slate-500 focus:outline-none focus:border-slate-600" />
              </div>
              <div class="mb-4">
                <label class="block text-xs uppercase tracking-wider text-slate-500 mb-1.5">GitHub Username (optional)</label>
                <input v-model="newInvitation.gitHubUsername" type="text" class="w-full px-3 py-2 rounded bg-slate-900/60 border border-slate-700 text-slate-200 placeholder-slate-500 focus:outline-none focus:border-slate-600 font-mono" />
              </div>
              <div class="mb-4">
                <label class="block text-xs uppercase tracking-wider text-slate-500 mb-1.5">Role</label>
                <select v-model="newInvitation.assignedRole" class="w-full px-3 py-2 rounded bg-slate-900/60 border border-slate-700 text-slate-200 focus:outline-none focus:border-slate-600">
                  <option value="Developer">Developer</option>
                  <option value="Admin">Admin</option>
                </select>
              </div>
              <div class="mb-4">
                <label class="block text-xs uppercase tracking-wider text-slate-500 mb-1.5">Expires in (days, 0 = never)</label>
                <input v-model.number="newInvitation.expiresInDays" type="number" min="0" class="w-full px-3 py-2 rounded bg-slate-900/60 border border-slate-700 text-slate-200 placeholder-slate-500 focus:outline-none focus:border-slate-600 font-mono tabular-nums" />
              </div>
              <div class="flex gap-3 justify-end">
                <button type="button" @click="showCreateInvitation = false" class="px-3 py-1.5 rounded border border-slate-800 text-slate-300 hover:bg-slate-800 text-sm transition-colors">Cancel</button>
                <button type="submit" class="px-3 py-1.5 rounded bg-slate-200 text-slate-900 hover:bg-white text-sm transition-colors">Create</button>
              </div>
            </form>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full">
            <thead>
              <tr class="border-b border-slate-800">
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Email</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">GitHub</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Role</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Status</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Created</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Invite Link</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="invitation in invitations" :key="invitation.id" class="border-b border-slate-800 hover:bg-slate-800/40 hover:border-slate-700 transition-all duration-150 ease-out">
                <td class="px-3 py-2 text-slate-200">{{ invitation.email }}</td>
                <td class="px-3 py-2 text-slate-200/60 font-mono">{{ invitation.gitHubUsername || '–' }}</td>
                <td class="px-3 py-2 text-slate-200">{{ invitation.assignedRole }}</td>
                <td class="px-3 py-2">
                  <span class="inline-flex items-center gap-1 bg-slate-800/60 border border-slate-700/60 rounded px-2 py-0.5 text-xs text-slate-200">
                    <span class="font-mono" :class="invitation.status === 'Pending' ? 'text-amber-400' : invitation.status === 'Accepted' ? 'text-emerald-400' : 'text-slate-500'">{{ invitation.status === 'Pending' ? '⋯' : invitation.status === 'Accepted' ? '✓' : '✕' }}</span>
                    {{ invitation.status }}
                  </span>
                </td>
                <td class="px-3 py-2 text-slate-200/60 text-xs font-mono tabular-nums">{{ formatDate(invitation.createdAt) }}</td>
                <td class="px-3 py-2">
                  <div class="flex gap-2 items-center">
                    <input
                      :value="invitation.inviteUrl"
                      readonly
                      class="w-40 px-2 py-1 text-xs rounded bg-slate-900/60 border border-slate-800 text-slate-200/60 font-mono"
                    />
                    <button @click="copyInviteLink(invitation.inviteUrl)" class="px-2 py-1 text-xs rounded border border-slate-800 text-slate-300 hover:bg-slate-800 transition-colors">
                      Copy
                    </button>
                  </div>
                </td>
                <td class="px-3 py-2">
                  <button
                    v-if="invitation.status === 'Pending'"
                    @click="revokeInvitation(invitation.id)"
                    class="px-2.5 py-1 rounded border border-red-900/40 bg-red-950/20 text-red-400 text-xs hover:bg-red-950/40 hover:border-red-900/60 transition-colors"
                  >
                    Revoke
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div v-else-if="activeTab === 'allowlist'">
        <div class="flex justify-between items-center mb-2">
          <h4 class="text-sm font-semibold uppercase tracking-wider text-slate-300">Allowlist</h4>
          <button @click="showAddAllowlist = true" class="px-3 py-1.5 rounded bg-slate-200 text-slate-900 hover:bg-white text-sm transition-colors">
            Add Entry
          </button>
        </div>

        <div v-if="showAddAllowlist" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50" @click.self="showAddAllowlist = false">
          <div class="bg-slate-900 p-6 rounded border border-slate-800 w-full max-w-md">
            <h5 class="text-sm font-semibold uppercase tracking-wider text-slate-300 mb-4">Add to Allowlist</h5>
            <form @submit.prevent="addToAllowlist">
              <div class="mb-4">
                <label class="block text-xs uppercase tracking-wider text-slate-500 mb-1.5">Email (optional)</label>
                <input v-model="newAllowlist.email" type="email" class="w-full px-3 py-2 rounded bg-slate-900/60 border border-slate-700 text-slate-200 placeholder-slate-500 focus:outline-none focus:border-slate-600" />
              </div>
              <div class="mb-4">
                <label class="block text-xs uppercase tracking-wider text-slate-500 mb-1.5">GitHub Username (optional)</label>
                <input v-model="newAllowlist.gitHubUsername" type="text" class="w-full px-3 py-2 rounded bg-slate-900/60 border border-slate-700 text-slate-200 placeholder-slate-500 focus:outline-none focus:border-slate-600 font-mono" />
              </div>
              <p class="text-[11px] text-slate-500 mb-4">At least one of email or GitHub username is required.</p>
              <div class="mb-4">
                <label class="block text-xs uppercase tracking-wider text-slate-500 mb-1.5">Role</label>
                <select v-model="newAllowlist.assignedRole" class="w-full px-3 py-2 rounded bg-slate-900/60 border border-slate-700 text-slate-200 focus:outline-none focus:border-slate-600">
                  <option value="Developer">Developer</option>
                  <option value="Admin">Admin</option>
                </select>
              </div>
              <div class="flex gap-3 justify-end">
                <button type="button" @click="showAddAllowlist = false" class="px-3 py-1.5 rounded border border-slate-800 text-slate-300 hover:bg-slate-800 text-sm transition-colors">Cancel</button>
                <button type="submit" class="px-3 py-1.5 rounded bg-slate-200 text-slate-900 hover:bg-white text-sm transition-colors">Add</button>
              </div>
            </form>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full">
            <thead>
              <tr class="border-b border-slate-800">
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Email</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">GitHub</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Role</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Created</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Created By</th>
                <th class="px-3 py-2 text-left text-[11px] font-normal uppercase tracking-wider text-slate-500">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="entry in allowlist" :key="entry.id" class="border-b border-slate-800 hover:bg-slate-800/40 hover:border-slate-700 transition-all duration-150 ease-out">
                <td class="px-3 py-2 text-slate-200">{{ entry.email || '–' }}</td>
                <td class="px-3 py-2 text-slate-200/60 font-mono">{{ entry.gitHubUsername || '–' }}</td>
                <td class="px-3 py-2 text-slate-200">{{ entry.assignedRole }}</td>
                <td class="px-3 py-2 text-slate-200/60 text-xs font-mono tabular-nums">{{ formatDate(entry.createdAt) }}</td>
                <td class="px-3 py-2 text-slate-200/60 font-mono">{{ entry.createdBy?.username || '–' }}</td>
                <td class="px-3 py-2">
                  <button @click="removeFromAllowlist(entry.id)" class="px-2.5 py-1 rounded border border-red-900/40 bg-red-950/20 text-red-400 text-xs hover:bg-red-950/40 hover:border-red-900/60 transition-colors">
                    Remove
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div v-else-if="activeTab === 'stats'">
        <div class="mb-2">
          <h4 class="text-sm font-semibold uppercase tracking-wider text-slate-300">Statistics</h4>
        </div>
        <div class="flex flex-wrap gap-x-8 gap-y-4 border-t border-slate-800 pt-4">
          <div>
            <div class="flex items-baseline gap-1.5">
              <span class="font-mono text-sm text-slate-400">●</span>
              <span class="text-xl sm:text-2xl font-semibold tabular-nums text-slate-100 font-mono">{{ stats.totalUsers }}</span>
            </div>
            <div class="text-[11px] uppercase tracking-wider text-slate-500 mt-1">Total Users</div>
          </div>
          <div>
            <div class="flex items-baseline gap-1.5">
              <span class="font-mono text-sm text-amber-400">⚑</span>
              <span class="text-xl sm:text-2xl font-semibold tabular-nums text-slate-100 font-mono">{{ stats.adminCount }}</span>
            </div>
            <div class="text-[11px] uppercase tracking-wider text-slate-500 mt-1">Admins</div>
          </div>
          <div>
            <div class="flex items-baseline gap-1.5">
              <span class="font-mono text-sm text-blue-400">●</span>
              <span class="text-xl sm:text-2xl font-semibold tabular-nums text-slate-100 font-mono">{{ stats.developerCount }}</span>
            </div>
            <div class="text-[11px] uppercase tracking-wider text-slate-500 mt-1">Developers</div>
          </div>
          <div>
            <div class="flex items-baseline gap-1.5">
              <span class="font-mono text-sm text-amber-400">⋯</span>
              <span class="text-xl sm:text-2xl font-semibold tabular-nums text-slate-100 font-mono">{{ stats.pendingInvitations }}</span>
            </div>
            <div class="text-[11px] uppercase tracking-wider text-slate-500 mt-1">Pending Invitations</div>
          </div>
          <div>
            <div class="flex items-baseline gap-1.5">
              <span class="font-mono text-sm text-violet-400">◐</span>
              <span class="text-xl sm:text-2xl font-semibold tabular-nums text-slate-100 font-mono">{{ stats.allowlistEntries }}</span>
            </div>
            <div class="text-[11px] uppercase tracking-wider text-slate-500 mt-1">Allowlist Entries</div>
          </div>
        </div>
      </div>
    </div>

    <Transition
      enter-active-class="transition ease-out duration-300"
      enter-from-class="translate-y-2 opacity-0"
      enter-to-class="translate-y-0 opacity-100"
      leave-active-class="transition ease-in duration-200"
      leave-from-class="translate-y-0 opacity-100"
      leave-to-class="translate-y-2 opacity-0"
    >
      <div v-if="notification" :class="[
        'fixed bottom-4 right-4 inline-flex items-center gap-2 px-3 py-2 rounded border text-sm z-50',
        notification.type === 'success'
          ? 'bg-slate-900 border-slate-700 text-slate-200'
          : 'bg-slate-900 border-slate-700 text-slate-200'
      ]">
        <span class="font-mono" :class="notification.type === 'success' ? 'text-emerald-400' : 'text-red-400'">{{ notification.type === 'success' ? '✓' : '✕' }}</span>
        {{ notification.message }}
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useAuthStore } from '../../stores/auth';
import { apiService } from '../../services/api';
import type { User, UserRole, Invitation, AllowedUser, AdminStats } from '../../types';

const authStore = useAuthStore();
const currentUserId = computed(() => authStore.user?.id);

const loading = ref(true);
const activeTab = ref('users');
const users = ref<User[]>([]);
const invitations = ref<Invitation[]>([]);
const allowlist = ref<AllowedUser[]>([]);
const stats = ref<AdminStats>({
  totalUsers: 0,
  adminCount: 0,
  developerCount: 0,
  pendingInvitations: 0,
  allowlistEntries: 0
});

const showCreateInvitation = ref(false);
const showAddAllowlist = ref(false);

const newInvitation = ref({
  email: '',
  gitHubUsername: '',
  assignedRole: 'Developer' as UserRole,
  expiresInDays: 7
});

const newAllowlist = ref({
  email: '',
  gitHubUsername: '',
  assignedRole: 'Developer' as UserRole
});

const notification = ref<{ type: string; message: string } | null>(null);

const tabs = computed(() => [
  { id: 'users', label: 'Users', count: users.value.length },
  { id: 'invitations', label: 'Invitations', count: invitations.value.filter(i => i.status === 'Pending').length },
  { id: 'allowlist', label: 'Allowlist', count: allowlist.value.length },
  { id: 'stats', label: 'Statistics' }
]);

function showNotification(type: string, message: string) {
  notification.value = { type, message };
  setTimeout(() => {
    notification.value = null;
  }, 3000);
}

function formatDate(dateString?: string): string {
  if (!dateString) return '-';
  return new Date(dateString).toLocaleDateString();
}

async function loadData() {
  loading.value = true;
  try {
    const [usersData, invitationsData, allowlistData, statsData] = await Promise.all([
      apiService.getUsers(),
      apiService.getInvitations(),
      apiService.getAllowlist(),
      apiService.getAdminStats()
    ]);
    users.value = usersData;
    invitations.value = invitationsData;
    allowlist.value = allowlistData;
    stats.value = statsData;
  } catch (error) {
    showNotification('error', 'Failed to load data');
  } finally {
    loading.value = false;
  }
}

async function updateUserRole(userId: number, role: UserRole) {
  try {
    await apiService.updateUserRole(userId, role);
    showNotification('success', 'Role updated successfully');
    await loadData();
  } catch (error) {
    showNotification('error', 'Failed to update role');
  }
}

async function deleteUser(userId: number) {
  if (!confirm('Are you sure you want to delete this user?')) return;
  try {
    await apiService.deleteUser(userId);
    showNotification('success', 'User deleted successfully');
    await loadData();
  } catch (error) {
    showNotification('error', 'Failed to delete user');
  }
}

async function createInvitation() {
  try {
    await apiService.createInvitation({
      email: newInvitation.value.email,
      gitHubUsername: newInvitation.value.gitHubUsername || undefined,
      assignedRole: newInvitation.value.assignedRole,
      expiresInDays: newInvitation.value.expiresInDays || undefined
    });
    showNotification('success', 'Invitation created successfully');
    showCreateInvitation.value = false;
    newInvitation.value = {
      email: '',
      gitHubUsername: '',
      assignedRole: 'Developer',
      expiresInDays: 7
    };
    await loadData();
  } catch (error) {
    showNotification('error', 'Failed to create invitation');
  }
}

async function revokeInvitation(invitationId: number) {
  if (!confirm('Are you sure you want to revoke this invitation?')) return;
  try {
    await apiService.revokeInvitation(invitationId);
    showNotification('success', 'Invitation revoked successfully');
    await loadData();
  } catch (error) {
    showNotification('error', 'Failed to revoke invitation');
  }
}

function copyInviteLink(url: string) {
  navigator.clipboard.writeText(url);
  showNotification('success', 'Invite link copied to clipboard');
}

async function addToAllowlist() {
  try {
    await apiService.addToAllowlist({
      email: newAllowlist.value.email || undefined,
      gitHubUsername: newAllowlist.value.gitHubUsername || undefined,
      assignedRole: newAllowlist.value.assignedRole
    });
    showNotification('success', 'Added to allowlist successfully');
    showAddAllowlist.value = false;
    newAllowlist.value = {
      email: '',
      gitHubUsername: '',
      assignedRole: 'Developer'
    };
    await loadData();
  } catch (error) {
    showNotification('error', 'Failed to add to allowlist');
  }
}

async function removeFromAllowlist(id: number) {
  if (!confirm('Are you sure you want to remove this entry?')) return;
  try {
    await apiService.removeFromAllowlist(id);
    showNotification('success', 'Removed from allowlist successfully');
    await loadData();
  } catch (error) {
    showNotification('error', 'Failed to remove from allowlist');
  }
}

onMounted(() => {
  loadData();
});
</script>

<style scoped>
table {
  border-spacing: 0;
}
</style>
