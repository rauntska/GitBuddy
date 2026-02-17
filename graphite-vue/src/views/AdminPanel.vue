<template>
  <div class="admin-panel p-6 max-w-6xl mx-auto">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-xl font-semibold text-white">Admin Panel</h1>
      <router-link to="/" class="px-4 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-sm font-medium border border-slate-700 transition-colors">
        Back to Dashboard
      </router-link>
    </div>

    <div class="flex gap-2 mb-6 border-b border-slate-700 pb-4">
      <button 
        v-for="tab in tabs" 
        :key="tab.id"
        @click="activeTab = tab.id"
        :class="[
          'px-4 py-2 rounded-lg text-sm font-medium transition-colors flex items-center gap-2',
          activeTab === tab.id 
            ? 'bg-blue-600 text-white' 
            : 'text-slate-400 hover:text-slate-300 hover:bg-slate-800'
        ]"
      >
        {{ tab.label }}
        <span v-if="tab.count !== undefined" class="px-2 py-0.5 rounded-full text-xs bg-slate-700 text-slate-300">
          {{ tab.count }}
        </span>
      </button>
    </div>

    <div class="bg-slate-800/50 rounded-xl border border-slate-700 p-6">
      <div v-if="loading" class="text-center py-8 text-slate-400">Loading...</div>
      
      <div v-else-if="activeTab === 'users'">
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-lg font-semibold text-white">Users</h2>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full">
            <thead>
              <tr class="border-b border-slate-700">
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Username</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Email</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Role</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Created</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Last Login</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="user in users" :key="user.id" class="border-b border-slate-700/50 hover:bg-slate-700/30">
                <td class="px-4 py-3">
                  <div class="flex items-center gap-3">
                    <img v-if="user.avatarUrl" :src="user.avatarUrl" :alt="user.username" class="w-8 h-8 rounded-full" />
                    <span class="text-slate-200">{{ user.username }}</span>
                  </div>
                </td>
                <td class="px-4 py-3 text-slate-400">{{ user.email }}</td>
                <td class="px-4 py-3">
                  <select 
                    :value="user.role" 
                    @change="updateUserRole(user.id, ($event.target as HTMLSelectElement).value as UserRole)"
                    :disabled="user.id === currentUserId"
                    class="px-3 py-1.5 rounded-lg border border-slate-600 bg-slate-700 text-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
                  >
                    <option value="Developer">Developer</option>
                    <option value="Admin">Admin</option>
                  </select>
                </td>
                <td class="px-4 py-3 text-slate-400 text-sm">{{ formatDate(user.createdAt) }}</td>
                <td class="px-4 py-3 text-slate-400 text-sm">{{ user.lastLoginAt ? formatDate(user.lastLoginAt) : 'Never' }}</td>
                <td class="px-4 py-3">
                  <button 
                    v-if="user.id !== currentUserId"
                    @click="deleteUser(user.id)"
                    class="px-3 py-1.5 rounded-lg bg-red-600/20 text-red-400 text-sm hover:bg-red-600/30 transition-colors"
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
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-lg font-semibold text-white">Invitations</h2>
          <button @click="showCreateInvitation = true" class="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium transition-colors">
            Create Invitation
          </button>
        </div>
        
        <div v-if="showCreateInvitation" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50" @click.self="showCreateInvitation = false">
          <div class="bg-slate-800 p-6 rounded-xl border border-slate-700 w-full max-w-md">
            <h3 class="text-lg font-semibold text-white mb-4">Create Invitation</h3>
            <form @submit.prevent="createInvitation">
              <div class="mb-4">
                <label class="block text-sm font-medium text-slate-300 mb-2">Email *</label>
                <input v-model="newInvitation.email" type="email" required class="w-full px-4 py-2.5 rounded-lg bg-slate-700 border border-slate-600 text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div class="mb-4">
                <label class="block text-sm font-medium text-slate-300 mb-2">GitHub Username (optional)</label>
                <input v-model="newInvitation.gitHubUsername" type="text" class="w-full px-4 py-2.5 rounded-lg bg-slate-700 border border-slate-600 text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div class="mb-4">
                <label class="block text-sm font-medium text-slate-300 mb-2">Role</label>
                <select v-model="newInvitation.assignedRole" class="w-full px-4 py-2.5 rounded-lg bg-slate-700 border border-slate-600 text-white focus:outline-none focus:ring-2 focus:ring-blue-500">
                  <option value="Developer">Developer</option>
                  <option value="Admin">Admin</option>
                </select>
              </div>
              <div class="mb-4">
                <label class="block text-sm font-medium text-slate-300 mb-2">Expires in (days, 0 = never)</label>
                <input v-model.number="newInvitation.expiresInDays" type="number" min="0" class="w-full px-4 py-2.5 rounded-lg bg-slate-700 border border-slate-600 text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div class="flex gap-3 justify-end">
                <button type="button" @click="showCreateInvitation = false" class="px-4 py-2 rounded-lg bg-slate-700 hover:bg-slate-600 text-slate-300 font-medium transition-colors">Cancel</button>
                <button type="submit" class="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-medium transition-colors">Create</button>
              </div>
            </form>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full">
            <thead>
              <tr class="border-b border-slate-700">
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Email</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">GitHub</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Role</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Status</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Created</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Invite Link</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="invitation in invitations" :key="invitation.id" class="border-b border-slate-700/50 hover:bg-slate-700/30">
                <td class="px-4 py-3 text-slate-200">{{ invitation.email }}</td>
                <td class="px-4 py-3 text-slate-400">{{ invitation.gitHubUsername || '-' }}</td>
                <td class="px-4 py-3 text-slate-300">{{ invitation.assignedRole }}</td>
                <td class="px-4 py-3">
                  <span :class="[
                    'px-2.5 py-1 rounded-full text-xs font-medium',
                    invitation.status === 'Pending' ? 'bg-amber-500/20 text-amber-400' :
                    invitation.status === 'Accepted' ? 'bg-emerald-500/20 text-emerald-400' :
                    'bg-slate-600/50 text-slate-400'
                  ]">
                    {{ invitation.status }}
                  </span>
                </td>
                <td class="px-4 py-3 text-slate-400 text-sm">{{ formatDate(invitation.createdAt) }}</td>
                <td class="px-4 py-3">
                  <div class="flex gap-2 items-center">
                    <input 
                      :value="invitation.inviteUrl" 
                      readonly 
                      class="w-40 px-2 py-1 text-xs rounded bg-slate-700 border border-slate-600 text-slate-300"
                    />
                    <button @click="copyInviteLink(invitation.inviteUrl)" class="px-2 py-1 text-xs rounded bg-slate-700 hover:bg-slate-600 text-slate-300 transition-colors">
                      Copy
                    </button>
                  </div>
                </td>
                <td class="px-4 py-3">
                  <button 
                    v-if="invitation.status === 'Pending'"
                    @click="revokeInvitation(invitation.id)"
                    class="px-3 py-1.5 rounded-lg bg-red-600/20 text-red-400 text-sm hover:bg-red-600/30 transition-colors"
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
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-lg font-semibold text-white">Allowlist</h2>
          <button @click="showAddAllowlist = true" class="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium transition-colors">
            Add Entry
          </button>
        </div>

        <div v-if="showAddAllowlist" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50" @click.self="showAddAllowlist = false">
          <div class="bg-slate-800 p-6 rounded-xl border border-slate-700 w-full max-w-md">
            <h3 class="text-lg font-semibold text-white mb-4">Add to Allowlist</h3>
            <form @submit.prevent="addToAllowlist">
              <div class="mb-4">
                <label class="block text-sm font-medium text-slate-300 mb-2">Email (optional)</label>
                <input v-model="newAllowlist.email" type="email" class="w-full px-4 py-2.5 rounded-lg bg-slate-700 border border-slate-600 text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div class="mb-4">
                <label class="block text-sm font-medium text-slate-300 mb-2">GitHub Username (optional)</label>
                <input v-model="newAllowlist.gitHubUsername" type="text" class="w-full px-4 py-2.5 rounded-lg bg-slate-700 border border-slate-600 text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <p class="text-xs text-slate-400 mb-4">At least one of email or GitHub username is required.</p>
              <div class="mb-4">
                <label class="block text-sm font-medium text-slate-300 mb-2">Role</label>
                <select v-model="newAllowlist.assignedRole" class="w-full px-4 py-2.5 rounded-lg bg-slate-700 border border-slate-600 text-white focus:outline-none focus:ring-2 focus:ring-blue-500">
                  <option value="Developer">Developer</option>
                  <option value="Admin">Admin</option>
                </select>
              </div>
              <div class="flex gap-3 justify-end">
                <button type="button" @click="showAddAllowlist = false" class="px-4 py-2 rounded-lg bg-slate-700 hover:bg-slate-600 text-slate-300 font-medium transition-colors">Cancel</button>
                <button type="submit" class="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-medium transition-colors">Add</button>
              </div>
            </form>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full">
            <thead>
              <tr class="border-b border-slate-700">
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Email</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">GitHub</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Role</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Created</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Created By</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-slate-400 uppercase">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="entry in allowlist" :key="entry.id" class="border-b border-slate-700/50 hover:bg-slate-700/30">
                <td class="px-4 py-3 text-slate-200">{{ entry.email || '-' }}</td>
                <td class="px-4 py-3 text-slate-400">{{ entry.gitHubUsername || '-' }}</td>
                <td class="px-4 py-3 text-slate-300">{{ entry.assignedRole }}</td>
                <td class="px-4 py-3 text-slate-400 text-sm">{{ formatDate(entry.createdAt) }}</td>
                <td class="px-4 py-3 text-slate-400">{{ entry.createdBy?.username || '-' }}</td>
                <td class="px-4 py-3">
                  <button @click="removeFromAllowlist(entry.id)" class="px-3 py-1.5 rounded-lg bg-red-600/20 text-red-400 text-sm hover:bg-red-600/30 transition-colors">
                    Remove
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div v-else-if="activeTab === 'stats'">
        <div class="mb-4">
          <h2 class="text-lg font-semibold text-white">Statistics</h2>
        </div>
        <div class="grid grid-cols-2 md:grid-cols-5 gap-4">
          <div class="bg-slate-700/50 p-6 rounded-xl text-center border border-slate-600">
            <div class="text-3xl font-bold text-blue-400">{{ stats.totalUsers }}</div>
            <div class="text-sm text-slate-400 mt-1">Total Users</div>
          </div>
          <div class="bg-slate-700/50 p-6 rounded-xl text-center border border-slate-600">
            <div class="text-3xl font-bold text-purple-400">{{ stats.adminCount }}</div>
            <div class="text-sm text-slate-400 mt-1">Admins</div>
          </div>
          <div class="bg-slate-700/50 p-6 rounded-xl text-center border border-slate-600">
            <div class="text-3xl font-bold text-emerald-400">{{ stats.developerCount }}</div>
            <div class="text-sm text-slate-400 mt-1">Developers</div>
          </div>
          <div class="bg-slate-700/50 p-6 rounded-xl text-center border border-slate-600">
            <div class="text-3xl font-bold text-amber-400">{{ stats.pendingInvitations }}</div>
            <div class="text-sm text-slate-400 mt-1">Pending Invitations</div>
          </div>
          <div class="bg-slate-700/50 p-6 rounded-xl text-center border border-slate-600">
            <div class="text-3xl font-bold text-cyan-400">{{ stats.allowlistEntries }}</div>
            <div class="text-sm text-slate-400 mt-1">Allowlist Entries</div>
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
        'fixed bottom-4 right-4 px-4 py-3 rounded-lg text-sm font-medium shadow-lg z-50',
        notification.type === 'success' ? 'bg-emerald-600 text-white' : 'bg-red-600 text-white'
      ]">
        {{ notification.message }}
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useAuthStore } from '../stores/auth';
import { apiService } from '../services/api';
import type { User, UserRole, Invitation, AllowedUser, AdminStats } from '../types';

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

table th {
  letter-spacing: 0.05em;
}
</style>
