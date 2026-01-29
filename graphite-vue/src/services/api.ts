import apiClient from '../utils/api';
import type { GroupedPRs, PRStats, Settings, PRDetail, FileDiff, Comment, UserPreferences } from '../types';

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

  submitReview: async (prId: number, review: { state: 'APPROVED' | 'CHANGES_REQUESTED' | 'COMMENT'; body?: string }): Promise<{ message: string }> => {
    const response = await api.post<{ message: string }>(`/pullrequests/${prId}/review`, review);
    return response.data;
  },

  mergePR: async (prId: number): Promise<{ message: string }> => {
    const response = await api.post<{ message: string }>(`/pullrequests/${prId}/merge`);
    return response.data;
  },

  // Refresh file viewed states from GitHub
  refreshFileViewStates: async (prId: number): Promise<FileDiff[]> => {
    const response = await api.post<FileDiff[]>(`/pullrequests/${prId}/file-diffs/refresh-viewed-states`);
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
};

export default api;
