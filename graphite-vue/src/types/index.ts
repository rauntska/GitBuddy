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
  appId: string;
  privateKey: string;
  installationId: string;
  useGitHubApp: boolean;
}

export interface Comment {
  id: number;
  gitHubId: string;
  reviewThreadId?: number;
  author: string;
  authorAvatar?: string;
  body: string;
  createdAt: string;
  updatedAt?: string;
  path?: string;
  line?: number;
  isOutdated: boolean;
}

export interface FileDiff {
  path?: string;
  oldPath?: string;
  status?: 'added' | 'modified' | 'deleted' | 'renamed';
  additions?: number;
  deletions?: number;
  changes?: number;
  patch?: string;
  language?: string;
  viewed?: boolean; // Legacy, keeping for compatibility
  viewedState?: 'VIEWED' | 'UNVIEWED' | 'DISMISSED' | null;
  viewedAt?: string | null;
}

export interface DiffHunk {
  oldStart: number;
  oldLines: number;
  newStart: number;
  newLines: number;
  lines: DiffLine[];
}

export interface DiffLine {
  type: 'add' | 'delete' | 'context';
  content: string;
  oldLineNumber?: number;
  newLineNumber?: number;
  hasComment?: boolean;
}

export interface PRDetail extends PullRequest {
  description: string;
  sourceBranch: string;
  targetBranch: string;
  files: FileDiff[];
  allComments: Comment[];
  mergeableState?: string;
  checksStatus?: 'pending' | 'success' | 'failure';
  viewedFiles?: string[];
}

export interface UserPreferences {
  diffViewMode: 'split' | 'unified';
  showContext: boolean;
  fileTreeWidth: number;
  commentsPanelWidth: number;
  fileTreeVisible: boolean;
  viewedFilesByPr?: Record<number, string[]>;
}
