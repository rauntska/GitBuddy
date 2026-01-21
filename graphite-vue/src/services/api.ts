import axios from 'axios';
import type { PullRequest, GroupedPRs, PRStats, Settings } from '../types';

const API_BASE_URL = 'http://localhost:5000/api';

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
};

export default api;
