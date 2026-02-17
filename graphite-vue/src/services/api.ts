import apiClient from '../utils/api';
import type { GroupedPRs, PRStats, Settings, PRDetail, FileDiff, Comment, UserPreferences, PullRequest, CommentTemplate, CommentDraft, MentionableUser, ReactionGroup, User, UserRole, Invitation, AllowedUser, AdminStats } from '../types';

const api = apiClient;

export const apiService = {
  getPullRequests: async (): Promise<GroupedPRs> => {
    const response = await api.get<GroupedPRs>('/pullrequests');
    return response.data;
  },

  getStats: async (): Promise<PRStats> => {
    const response = await api.get<PRStats>('/pullrequests/stats');
    return response.data;
  },

  getMergedPRs: async (skip = 0, take = 10): Promise<{ pullRequests: PullRequest[]; total: number; hasMore: boolean }> => {
    const response = await api.get<PullRequest[]>('/pullrequests/merged', {
      params: { skip, take }
    });
    const totalCount = parseInt(response.headers['x-total-count'] || '0', 10);
    const hasMore = response.headers['x-has-more'] === 'true';

    return {
      pullRequests: response.data,
      total: totalCount,
      hasMore
    };
  },

  refreshPullRequests: async (): Promise<{ message: string }> => {
    const response = await api.post<{ message: string }>('/pullrequests/refresh');
    return response.data;
  },

  getSettings: async (): Promise<Settings> => {
    const response = await api.get<Settings>('/settings');
    return response.data;
  },

  saveSettings: async (settings: Omit<Settings, 'lastRefresh'>): Promise<{ message: string }> => {
    const response = await api.post<{ message: string }>('/settings', settings);
    return response.data;
  },

  // PR Detail endpoints
  getPRDetail: async (id: number): Promise<PRDetail> => {
    const response = await api.get<PRDetail>(`/pullrequests/${id}`);
    return response.data;
  },

  getFileDiff: async (prId: number, filePath: string): Promise<FileDiff> => {
    const response = await api.get<FileDiff>(`/pullrequests/${prId}/files/${encodeURIComponent(filePath)}`);
    return response.data;
  },

  getComments: async (prId: number): Promise<Comment[]> => {
    const response = await api.get<Comment[]>(`/pullrequests/${prId}/comments`);
    return response.data;
  },

  addComment: async (prId: number, comment: { body: string; path?: string; line?: number }): Promise<Comment> => {
    const response = await api.post<Comment>(`/pullrequests/${prId}/comments`, comment);
    return response.data;
  },

  addCommentReply: async (prId: number, reply: { reviewThreadId: string; body: string }): Promise<Comment> => {
    const response = await api.post<Comment>(`/pullrequests/${prId}/comments/reply`, reply);
    return response.data;
  },

  resolveReviewThread: async (prId: number, threadId: string, resolved: boolean): Promise<{ message: string }> => {
    const response = await api.post<{ message: string }>(`/pullrequests/${prId}/threads/${threadId}/resolve`, { resolved });
    return response.data;
  },

  unresolveReviewThread: async (prId: number, threadId: string): Promise<{ message: string }> => {
    const response = await api.post<{ message: string }>(`/pullrequests/${prId}/threads/${threadId}/unresolve`, {});
    return response.data;
  },

  submitReview: async (prId: number, review: { state: 'APPROVED' | 'CHANGES_REQUESTED' | 'COMMENT'; body?: string }): Promise<{ message: string }> => {
    const response = await api.post<{ message: string }>(`/pullrequests/${prId}/review`, review);
    return response.data;
  },

  mergePR: async (prId: number, options?: { 
    mergeMethod?: 'merge' | 'squash' | 'rebase';
    commitTitle?: string;
    commitMessage?: string;
  }): Promise<{ message: string; isMerged: boolean; mergedAt?: string }> => {
    const response = await api.post<{ message: string; isMerged: boolean; mergedAt?: string }>(
      `/pullrequests/${prId}/merge`, 
      options || {}
    );
    return response.data;
  },

  getMergeOptions: async (prId: number): Promise<{
    mergeCommitAllowed: boolean;
    squashMergeAllowed: boolean;
    rebaseMergeAllowed: boolean;
    defaultMergeMethod: string;
    mergeableState?: string;
    isMerged: boolean;
    isDraft: boolean;
  }> => {
    const response = await api.get(`/pullrequests/${prId}/merge-options`);
    return response.data;
  },

  publishDraftPR: async (prId: number): Promise<{ message: string; draft: boolean; status: string }> => {
    const response = await api.post<{ message: string; draft: boolean; status: string }>(`/pullrequests/${prId}/publish`);
    return response.data;
  },

  // Refresh file viewed states from GitHub
  refreshFileViewStates: async (prId: number): Promise<FileDiff[]> => {
    const response = await api.post<FileDiff[]>(`/pullrequests/${prId}/file-diffs/refresh-viewed-states`);
    return response.data;
  },

  updateFileViewedState: async (prId: number, path: string, viewed: boolean): Promise<void> => {
    await api.post(`/pullrequests/${prId}/files/viewed`, { path, viewed });
  },

  // User Preferences endpoints
  getUserPreferences: async (): Promise<UserPreferences> => {
    const response = await api.get<UserPreferences>('/userpreferences');
    return response.data;
  },

  updateUserPreferences: async (preferences: Partial<UserPreferences>): Promise<UserPreferences> => {
    const response = await api.patch<UserPreferences>('/userpreferences', preferences);
    return response.data;
  },

  // Enhanced Comment System

  // Comment Reactions
  addCommentReaction: async (commentId: number, reaction: string): Promise<void> => {
    await api.post(`/comments/${commentId}/reactions`, { reaction });
  },

  removeCommentReaction: async (commentId: number, reaction: string): Promise<void> => {
    await api.delete(`/comments/${commentId}/reactions/${reaction}`);
  },

  getCommentReactions: async (commentId: number): Promise<{ groups: ReactionGroup[] }> => {
    const response = await api.get(`/comments/${commentId}/reactions`);
    return response.data;
  },

  // Comment Editing
  updateComment: async (commentId: number, body: string): Promise<Comment> => {
    const response = await api.put<Comment>(`/comments/${commentId}`, { body });
    return response.data;
  },

  deleteComment: async (commentId: number): Promise<void> => {
    await api.delete(`/comments/${commentId}`);
  },

  // Comment Drafts
  getCommentDrafts: async (pullRequestId: number): Promise<CommentDraft[]> => {
    const response = await api.get<CommentDraft[]>(`/comment-drafts/prs/${pullRequestId}`);
    return response.data;
  },

  saveCommentDraft: async (draft: {
    pullRequestId: number;
    reviewThreadId?: number;
    filePath?: string;
    lineNumber?: number;
    body: string;
  }): Promise<CommentDraft> => {
    const response = await api.post<CommentDraft>('/comment-drafts', draft);
    return response.data;
  },

  deleteCommentDraft: async (draftId: number): Promise<void> => {
    await api.delete(`/comment-drafts/${draftId}`);
  },

  // Comment Templates
  getCommentTemplates: async (search?: string, tag?: string): Promise<CommentTemplate[]> => {
    const response = await api.get<CommentTemplate[]>('/comment-templates', {
      params: { search, tag }
    });
    return response.data;
  },

  getCommentTemplateTags: async (): Promise<string[]> => {
    const response = await api.get<string[]>('/comment-templates/tags');
    return response.data;
  },

  createCommentTemplate: async (template: {
    title: string;
    body: string;
    tags?: string;
    isOrganizationTemplate?: boolean;
  }): Promise<CommentTemplate> => {
    const response = await api.post<CommentTemplate>('/comment-templates', template);
    return response.data;
  },

  updateCommentTemplate: async (id: number, template: {
    title: string;
    body: string;
    tags?: string;
  }): Promise<CommentTemplate> => {
    const response = await api.put<CommentTemplate>(`/comment-templates/${id}`, template);
    return response.data;
  },

  deleteCommentTemplate: async (id: number): Promise<void> => {
    await api.delete(`/comment-templates/${id}`);
  },

  recordTemplateUsage: async (id: number): Promise<void> => {
    await api.post(`/comment-templates/${id}/use`);
  },

  // Mentionable Users
  getMentionableUsers: async (pullRequestId: number): Promise<MentionableUser[]> => {
    const response = await api.get<MentionableUser[]>(`/pullrequests/${pullRequestId}/mentionable-users`);
    return response.data;
  },

  // Auth
  getCurrentUser: async (): Promise<User> => {
    const response = await api.get<User>('/auth/me');
    return response.data;
  },

  // Admin - Users
  getUsers: async (): Promise<User[]> => {
    const response = await api.get<User[]>('/admin/users');
    return response.data;
  },

  updateUserRole: async (userId: number, role: UserRole): Promise<{ message: string }> => {
    const response = await api.put<{ message: string }>(`/admin/users/${userId}/role`, { role });
    return response.data;
  },

  deleteUser: async (userId: number): Promise<{ message: string }> => {
    const response = await api.delete<{ message: string }>(`/admin/users/${userId}`);
    return response.data;
  },

  // Admin - Invitations
  getInvitations: async (): Promise<Invitation[]> => {
    const response = await api.get<Invitation[]>('/admin/invitations');
    return response.data;
  },

  createInvitation: async (data: {
    email: string;
    gitHubUsername?: string;
    assignedRole?: UserRole;
    expiresInDays?: number;
  }): Promise<Invitation> => {
    const response = await api.post<Invitation>('/admin/invitations', data);
    return response.data;
  },

  revokeInvitation: async (invitationId: number): Promise<{ message: string }> => {
    const response = await api.delete<{ message: string }>(`/admin/invitations/${invitationId}`);
    return response.data;
  },

  // Admin - Allowlist
  getAllowlist: async (): Promise<AllowedUser[]> => {
    const response = await api.get<AllowedUser[]>('/admin/allowlist');
    return response.data;
  },

  addToAllowlist: async (data: {
    email?: string;
    gitHubUsername?: string;
    assignedRole?: UserRole;
  }): Promise<AllowedUser> => {
    const response = await api.post<AllowedUser>('/admin/allowlist', data);
    return response.data;
  },

  removeFromAllowlist: async (id: number): Promise<{ message: string }> => {
    const response = await api.delete<{ message: string }>(`/admin/allowlist/${id}`);
    return response.data;
  },

  // Admin - Stats
  getAdminStats: async (): Promise<AdminStats> => {
    const response = await api.get<AdminStats>('/admin/stats');
    return response.data;
  },
};

export default api;
