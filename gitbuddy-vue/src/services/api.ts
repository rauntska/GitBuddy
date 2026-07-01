import apiClient from '../utils/api';
import type { GroupedPRs, PRStats, Settings, PRDetail, FileDiff, Comment, UserPreferences, PullRequest, CommentTemplate, CommentDraft, MentionableUser, ReactionGroup, User, UserRole, Invitation, AllowedUser, AdminStats, PendingReview, UserSettings, ReviewerStatus, ReviewTimeline, PotentialReviewer, Repository, Branch, BranchComparison, CreatePullRequestRequest, CreatePullRequestResult, BranchWithoutPR, ThroughputAnalytics, ReviewerAnalytics, HealthAnalytics } from '../types';

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

  getUnreadCount: async (): Promise<{ count: number }> => {
    const response = await api.get<{ count: number }>('/pullrequests/unread-count');
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

  updateGeneralComment: async (prId: number, commentId: number, body: string): Promise<{ message: string }> => {
    const response = await api.put<{ message: string }>(`/pullrequests/${prId}/general-comments/${commentId}`, { body });
    return response.data;
  },

  deleteGeneralComment: async (prId: number, commentId: number): Promise<{ message: string }> => {
    const response = await api.delete<{ message: string }>(`/pullrequests/${prId}/general-comments/${commentId}`);
    return response.data;
  },

  addCommentReply: async (prId: number, reply: { reviewThreadId: string; body: string }): Promise<Comment | { isPending: true; pendingReviewId: string; commentNodeId: string; reviewThreadId: string; author: string; authorAvatar?: string; body: string; createdAt: string; updatedAt?: string }> => {
    const response = await api.post(`/pullrequests/${prId}/comments/reply`, reply);
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

  updatePullRequest: async (prId: number, data: { title?: string; body?: string }): Promise<{ message: string; title?: string; body?: string }> => {
    const response = await api.patch<{ message: string; title?: string; body?: string }>(`/pullrequests/${prId}`, data);
    return response.data;
  },

  publishDraftPR: async (prId: number): Promise<{ message: string; draft: boolean; status: string }> => {
    const response = await api.post<{ message: string; draft: boolean; status: string }>(`/pullrequests/${prId}/publish`);
    return response.data;
  },

  refreshFileViewStates: async (prId: number): Promise<FileDiff[]> => {
    const response = await api.post<FileDiff[]>(`/pullrequests/${prId}/file-diffs/refresh-viewed-states`);
    return response.data;
  },

  updateFileViewedState: async (prId: number, path: string, viewed: boolean): Promise<void> => {
    await api.post(`/pullrequests/${prId}/files/viewed`, { path, viewed });
  },

  getPendingReview: async (prId: number): Promise<PendingReview | null> => {
    const response = await api.get<PendingReview | null>(`/pullrequests/${prId}/pending-review`);
    return response.data;
  },

  addPendingReviewComment: async (prId: number, comment: { body: string; path: string; line: number; side?: string }): Promise<{
    commentId: string;
    reviewId: string;
    path: string;
    line: number;
    body: string;
    author: string;
    authorAvatar?: string;
    createdAt: string;
  }> => {
    const response = await api.post(`/pullrequests/${prId}/pending-review/comments`, comment);
    return response.data;
  },

  deletePendingReviewComment: async (prId: number, commentId: string): Promise<{ message: string }> => {
    const response = await api.delete<{ message: string }>(`/pullrequests/${prId}/pending-review/comments/${commentId}`);
    return response.data;
  },

  submitPendingReview: async (prId: number, review: { state: 'APPROVED' | 'CHANGES_REQUESTED' | 'COMMENT'; body?: string }): Promise<{ message: string }> => {
    const response = await api.post<{ message: string }>(`/pullrequests/${prId}/pending-review/submit`, review);
    return response.data;
  },

  deletePendingReview: async (prId: number): Promise<{ message: string }> => {
    const response = await api.delete<{ message: string }>(`/pullrequests/${prId}/pending-review`);
    return response.data;
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

  // User Settings (PAT)
  getUserSettings: async (): Promise<UserSettings> => {
    const response = await api.get<UserSettings>('/users/me/settings');
    return response.data;
  },

  updateUserSettings: async (settings: { personalAccessToken?: string | null }): Promise<UserSettings> => {
    const response = await api.put<UserSettings>('/users/me/settings', settings);
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

  // Admin - Analytics
  getAnalyticsThroughput: async (from?: string, to?: string, authors?: string[]): Promise<ThroughputAnalytics> => {
    const response = await api.get<ThroughputAnalytics>('/admin/analytics/throughput', {
      params: { from, to, authors },
    });
    return response.data;
  },

  getAnalyticsReviewers: async (from?: string, to?: string, authors?: string[]): Promise<ReviewerAnalytics> => {
    const response = await api.get<ReviewerAnalytics>('/admin/analytics/reviewers', {
      params: { from, to, authors },
    });
    return response.data;
  },

  getAnalyticsHealth: async (from?: string, to?: string, authors?: string[]): Promise<HealthAnalytics> => {
    const response = await api.get<HealthAnalytics>('/admin/analytics/health', {
      params: { from, to, authors },
    });
    return response.data;
  },

  // File content for code snippets
  getFileContentRange: async (
    prId: number,
    path: string,
    line: number,
    contextLines: number = 3
  ): Promise<{ lines: { lineNumber: number; content: string }[] }> => {
    const startLine = Math.max(1, line - contextLines);
    const endLine = line + contextLines;
    
    const response = await api.get(`/pullrequests/${prId}/files/content`, {
      params: {
        path,
        newStartLine: startLine,
        newEndLine: endLine
      }
    });
    
    return response.data;
  },

  getReviewers: async (prId: number): Promise<ReviewerStatus[]> => {
    const response = await api.get<ReviewerStatus[]>(`/pullrequests/${prId}/reviewers`);
    return response.data;
  },

  addReviewers: async (prId: number, reviewers: string[]): Promise<{ message: string }> => {
    const response = await api.post<{ message: string }>(`/pullrequests/${prId}/reviewers`, { reviewers });
    return response.data;
  },

  removeReviewer: async (prId: number, username: string): Promise<{ message: string }> => {
    const response = await api.delete<{ message: string }>(`/pullrequests/${prId}/reviewers/${username}`);
    return response.data;
  },

  getReviewTimeline: async (prId: number): Promise<ReviewTimeline> => {
    const response = await api.get<ReviewTimeline>(`/pullrequests/${prId}/timeline`);
    return response.data;
  },

  getPotentialReviewers: async (prId: number): Promise<PotentialReviewer[]> => {
    const response = await api.get<PotentialReviewer[]>(`/pullrequests/${prId}/potential-reviewers`);
    return response.data;
  },

  getRepositories: async (): Promise<Repository[]> => {
    const response = await api.get<Repository[]>('/repositories');
    return response.data;
  },

  getOrganizationRepositories: async (): Promise<{ organization: string; repositories: Repository[] }> => {
    const response = await api.get<{ organization: string; repositories: Repository[] }>('/repositories/organization');
    return response.data;
  },

  getBranches: async (owner: string, repo: string): Promise<Branch[]> => {
    const response = await api.get<Branch[]>(`/repositories/${owner}/${repo}/branches`);
    return response.data;
  },

  compareBranches: async (owner: string, repo: string, baseBranch: string, headBranch: string): Promise<BranchComparison> => {
    const response = await api.get<BranchComparison>(`/repositories/${owner}/${repo}/compare`, {
      params: { baseBranch, headBranch }
    });
    return response.data;
  },

  createPullRequest: async (data: CreatePullRequestRequest): Promise<CreatePullRequestResult> => {
    const response = await api.post<CreatePullRequestResult>('/pullrequests', data);
    return response.data;
  },

  getBranchesWithoutPR: async (): Promise<BranchWithoutPR[]> => {
    const response = await api.get<BranchWithoutPR[]>('/repositories/branches-without-prs');
    return response.data;
  },

  refreshBranchesWithoutPR: async (): Promise<void> => {
    await api.post('/repositories/branches-without-prs/refresh');
  },

  uploadImage: async (prId: number, file: File): Promise<string> => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('prId', prId.toString());
    const response = await api.post<{ url: string }>('/images/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    const relativeUrl = response.data.url;
    if (relativeUrl.startsWith('/')) {
      const envUrl = import.meta.env.VITE_API_BASE_URL || '/api';
      const apiBase = typeof envUrl === 'string' ? envUrl.replace(/\/api\/?$/, '') : window.location.origin;
      const base = apiBase.startsWith('http') ? apiBase : window.location.origin;
      return `${base}${relativeUrl}`;
    }
    return relativeUrl;
  },
};

export default api;
