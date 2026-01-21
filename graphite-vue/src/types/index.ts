export interface Review {
  id: number;
  reviewer: string;
  reviewerAvatar?: string;
  state: string;
  submittedAt?: string;
}

export interface Comment {
  id: number;
  count: number;
  lastUpdated?: string;
}

export interface PullRequest {
  id: number;
  gitHubId: number;
  title: string;
  repository: string;
  author: string;
  authorAvatar?: string;
  status: string;
  draft: boolean;
  url: string;
  additions: number;
  deletions: number;
  changedFiles: number;
  createdAt: string;
  updatedAt: string;
  lastSyncedAt: string;
  reviews: Review[];
  comment?: Comment;
}

export interface PRStats {
  totalOpen: number;
  draft: number;
  approved: number;
  awaitingReview: number;
}

export interface GroupedPRs {
  [key: string]: PullRequest[];
}

export interface Settings {
  organization: string;
  personalAccessToken: string;
  refreshIntervalMinutes: number;
  lastRefresh?: string;
}
