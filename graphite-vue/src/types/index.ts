export interface Review {
  id: number;
  reviewer: string;
  reviewerAvatar?: string;
  state: string;
  submittedAt?: string;
}

export interface ReviewThread {
  id: number;
  gitHubId: string;
  path: string;
  line?: number;
  state: string;
  isResolved: boolean;
  isOutdated: boolean;
  createdAt: string;
  updatedAt?: string;
  firstCommentAuthor: string;
  firstCommentBody: string;
  commentCount: number;
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
  reviewThreads: ReviewThread[];
}

export interface PRStats {
  totalOpen: number;
  draft: number;
  approved: number;
  awaitingReview: number;
  totalComments: number;
  resolvedComments: number;
  pendingComments: number;
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
