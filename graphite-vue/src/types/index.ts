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
  isMergeReady?: boolean;
  requiredApprovingReviews?: number;
  currentApprovingReviews?: number;
  hasUnresolvedThreads?: boolean;
  mergeBlockReason?: string;
}

export interface PRStats {
  totalOpen: number;
  draft: number;
  approved: number;
  readyToMerge: number;
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
  refreshIntervalMinutes: number;
  lastRefresh?: string;
  appId: string;
  privateKey: string;
  installationId: string;
  useGitHubApp: boolean;
  deleteOldPRs: boolean;
}

export interface UserSettings {
  personalAccessToken?: string;
  hasPersonalAccessToken: boolean;
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
  pendingReview?: PendingReview;
  requiredApprovingReviews?: number;
  currentApprovingReviews: number;
  hasUnresolvedThreads: boolean;
  isMergeReady: boolean;
  mergeBlockReason?: string;
}

export interface PendingReview {
  gitHubId: string;
  state: string;
  comments: PendingReviewComment[];
}

export interface PendingReviewComment {
  gitHubId: string;
  path: string;
  line?: number;
  body: string;
  author: string;
  authorAvatar?: string;
  createdAt: string;
  updatedAt?: string;
  threadId?: string;
}

export interface UserPreferences {
  diffViewMode: 'split' | 'unified';
  showContext: boolean;
  fileTreeWidth: number;
  commentsPanelWidth: number;
  fileTreeVisible: boolean;
  listViewMode: 'compact' | 'normal';
  viewedFilesByPr?: Record<number, string[]>;
  keyboardShortcuts: KeyboardShortcuts;
  notificationPreferences?: NotificationPreferences;
}

export interface NotificationPreferences {
  enabled: boolean;
  events: {
    prCreated: boolean;
    reviewAdded: boolean;
    commentAdded: boolean;
    threadResolved: boolean;
    prMerged: boolean;
    checkFailed: boolean;
  };
  quietHours: {
    enabled: boolean;
    start: string;
    end: string;
  };
}

export interface KeyboardShortcuts {
  toggleComments: string;
  toggleFileTree: string;
  nextFile: string;
  previousFile: string;
  nextComment: string;
  previousComment: string;
}

export interface FileLineContent {
  lineNumber: number;
  content: string;
}

export interface FileContentResponse {
  oldLines: FileLineContent[];
  newLines: FileLineContent[];
}

export interface ExpandRange {
  oldStart: number;
  oldEnd: number;
  newStart: number;
  newEnd: number;
}

export type ExpandPosition = 'before' | 'after' | 'between';

export interface ReviewerStatus {
  username: string;
  avatar?: string;
  reviewState?: 'APPROVED' | 'CHANGES_REQUESTED' | 'COMMENTED' | 'PENDING';
  reviewedAt?: string;
  isRequested: boolean;
  requestedAt?: string;
}

export interface ReviewEvent {
  id: number;
  type: 'REVIEW_SUBMITTED' | 'THREAD_CREATED' | 'THREAD_RESOLVED';
  actor: string;
  actorAvatar?: string;
  timestamp: string;
  state?: string;
  summary?: string;
  threadId?: number;
  filePath?: string;
}

export interface ReviewTimeline {
  events: ReviewEvent[];
}

export interface PotentialReviewer {
  name: string;
  avatar?: string;
  type: 'User' | 'Team';
}

export interface Repository {
  id: number;
  owner: string;
  name: string;
  fullName: string;
  description?: string;
  private: boolean;
  defaultBranch?: string;
  url: string;
}

export interface Branch {
  name: string;
  sha: string;
  protected: boolean;
}

export interface BranchComparison {
  status: string;
  aheadBy: number;
  behindBy: number;
  totalCommits: number;
  commits: Commit[];
  files: FileDiff[];
}

export interface Commit {
  sha: string;
  message: string;
  author: string;
  authoredAt: string;
}

export interface CreatePullRequestRequest {
  owner: string;
  repository: string;
  title: string;
  body?: string;
  head: string;
  base: string;
  draft: boolean;
}

export interface CreatePullRequestResult {
  success: boolean;
  message: string;
  pullRequest?: PRDetail;
  error?: string;
}

export interface BranchWithoutPR {
  owner: string;
  repo: string;
  repoFullName: string;
  branchName: string;
  defaultBranch: string;
  lastActivityAt?: string | null;
}
