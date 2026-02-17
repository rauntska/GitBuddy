export interface Review {
  id: number;
  gitHubId: string;
  reviewer: string;
  reviewerAvatar?: string;
  state: string;
  submittedAt?: string;
}

export type UserRole = 'Developer' | 'Admin';

export interface User {
  id: number;
  username: string;
  email: string;
  avatarUrl?: string;
  role: UserRole;
  name?: string;
  createdAt?: string;
  lastLoginAt?: string;
  provider?: string;
}

export interface Invitation {
  id: number;
  email: string;
  gitHubUsername?: string;
  token: string;
  assignedRole: UserRole;
  createdAt: string;
  expiresAt?: string;
  acceptedAt?: string;
  acceptedByUserId?: number;
  inviteUrl: string;
  createdBy?: {
    id: number;
    username: string;
  };
  status: 'Pending' | 'Accepted' | 'Expired';
}

export interface AllowedUser {
  id: number;
  email?: string;
  gitHubUsername?: string;
  assignedRole: UserRole;
  createdAt: string;
  createdBy?: {
    id: number;
    username: string;
  };
}

export interface AdminStats {
  totalUsers: number;
  adminCount: number;
  developerCount: number;
  pendingInvitations: number;
  allowlistEntries: number;
}

export interface ReviewThread {
  id: number;
  gitHubId: string;
  path: string;
  line?: number;
  diffSide?: 'LEFT' | 'RIGHT';
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
  checksStatus?: 'SUCCESS' | 'FAILURE' | 'PENDING' | 'NEUTRAL' | null;
  totalCheckRuns?: number;
  isMerged?: boolean;
  mergedAt?: string;
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
  deleteOldPRs: boolean;
  keyboardShortcuts?: KeyboardShortcuts;
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
  // Enhanced features
  editedAt?: string;
  editCount?: number;
  replyToCommentId?: number;
  reactions?: CommentReaction[];
}

export interface CommentReaction {
  id: number;
  username: string;
  reaction: string;
  createdAt: string;
}

export interface ReactionGroup {
  reaction: string;
  emoji: string;
  count: number;
  usernames: string[];
  hasReacted: boolean;
}

export interface CommentTemplate {
  id: number;
  title: string;
  body: string;
  tags?: string;
  usageCount: number;
  createdAt: string;
  lastUsedAt?: string;
  isOrganizationTemplate: boolean;
}

export interface CommentDraft {
  id: number;
  pullRequestId: number;
  reviewThreadId?: number;
  filePath?: string;
  lineNumber?: number;
  body: string;
  updatedAt: string;
}

export interface MentionableUser {
  username: string;
  avatarUrl?: string;
  name?: string;
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

export interface InlineDiffSegment {
  type: 'add' | 'delete' | 'equal';
  content: string;
}

export interface AlignedLine {
  type: 'delete' | 'add' | 'context' | 'spacer';
  content: string;
  lineNumber?: number;
  inlineDiff?: InlineDiffSegment[];
}

export interface AlignedRow {
  leftLine?: AlignedLine;
  rightLine?: AlignedLine;
}

export interface CheckRun {
  id: number;
  gitHubId: string;
  name: string;
  status: string;
  conclusion?: string;
  url?: string;
  startedAt: string;
  completedAt?: string;
}

export interface PRDetail extends PullRequest {
  description: string;
  sourceBranch: string;
  targetBranch: string;
  files: FileDiff[];
  allComments: Comment[];
  mergeableState?: string;
  checkRuns?: CheckRun[];
  viewedFiles?: string[];
  isMerged: boolean;
  mergedAt?: string;
}

export interface UserPreferences {
  diffViewMode: 'split' | 'unified';
  showContext: boolean;
  fileTreeWidth: number;
  commentsPanelWidth: number;
  fileTreeVisible: boolean;
  viewedFilesByPr?: Record<number, string[]>;
  keyboardShortcuts: KeyboardShortcuts;
}

export interface KeyboardShortcuts {
  toggleComments: string;
  toggleFileTree: string;
  nextFile: string;
  previousFile: string;
  nextComment: string;
  previousComment: string;
}
