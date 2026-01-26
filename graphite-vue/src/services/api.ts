import axios from 'axios';
import type { GroupedPRs, PRStats, Settings, PRDetail, FileDiff, Comment } from '../types';

const API_BASE_URL = 'http://localhost:5247/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

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
};

export default api;
