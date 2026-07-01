import { ref, type Ref } from 'vue';
import * as signalR from '@microsoft/signalr';
import type { Review, Comment, BranchWithoutPR } from '../types';

export type ConnectionState = 'connected' | 'reconnecting' | 'disconnected';

export interface PRListUpdate {
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
  checksStatus?: string;
  reviews: Review[];
  reviewThreads: Array<{
    id: number;
    gitHubId: string;
    path: string;
    line?: number;
    diffSide?: string;
    state: string;
    isResolved: boolean;
    isOutdated: boolean;
    createdAt: string;
    updatedAt?: string;
    firstCommentAuthor: string;
    firstCommentBody: string;
    commentCount: number;
  }>;
}

export interface ReviewNotification {
  pullRequestId: number;
  newStatus: string;
  review: Review;
}

export interface CommentNotification {
  pullRequestId: number;
  action: 'added' | 'updated' | 'deleted';
  comment: Comment;
}

export interface ThreadNotification {
  pullRequestId: number;
  threadId: number;
  isResolved: boolean;
}

export interface PRClosedNotification {
  pullRequestId: number;
  gitHubId: number;
  repository: string;
  wasMerged: boolean;
}

export interface CheckRunsNotification {
  pullRequestId: number;
  checksStatus: string;
  checkRuns: Array<{
    id: number;
    gitHubId: string;
    name: string;
    status: string;
    conclusion?: string;
    url?: string;
    startedAt: string;
    completedAt?: string;
  }>;
}

export interface PendingBranchResolvedNotification {
  repository: string;
  branchName: string;
}

let connection: signalR.HubConnection | null = null;
let connectionCount = 0;
const connectionState: Ref<ConnectionState> = ref('disconnected');

// These two are module-level so any composable can register handlers for them,
// regardless of which useSignalR() invocation ran setupEventHandlers. The other
// event handlers are scoped to the usePullRequests composable that owns the
// connection lifecycle.
const onPendingBranchResolved: Ref<((notification: PendingBranchResolvedNotification) => void) | null> = ref(null);
const onPendingBranchAdded: Ref<((branch: BranchWithoutPR) => void) | null> = ref(null);

export function useSignalR() {
  const error: Ref<string | null> = ref(null);

  const onPRCreated: Ref<((pr: PRListUpdate) => void) | null> = ref(null);
  const onPRUpdated: Ref<((pr: PRListUpdate) => void) | null> = ref(null);
  const onPRClosed: Ref<((notification: PRClosedNotification) => void) | null> = ref(null);
  const onReviewAdded: Ref<((notification: ReviewNotification) => void) | null> = ref(null);
  const onCommentChanged: Ref<((notification: CommentNotification) => void) | null> = ref(null);
  const onThreadChanged: Ref<((notification: ThreadNotification) => void) | null> = ref(null);
  const onCheckRunsUpdated: Ref<((notification: CheckRunsNotification) => void) | null> = ref(null);

  const connect = async (token: string): Promise<boolean> => {
    if (connection && connectionState.value === 'connected') {
      connectionCount++;
      return true;
    }

    if (connection) {
      connectionCount++;
      return true;
    }

    try {
      const hubUrl = '/hubs/pr';
      
      connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => token,
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            if (retryContext.elapsedMilliseconds < 60000) {
              return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
            }
            return null;
          }
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      setupEventHandlers();
      setupConnectionLifecycleHandlers();

      await connection.start();
      connectionCount++;
      connectionState.value = 'connected';
      error.value = null;
      
      return true;
    } catch (err) {
      console.error('SignalR connection error:', err);
      error.value = 'Failed to connect to real-time updates';
      connectionState.value = 'disconnected';
      return false;
    }
  };

  const setupEventHandlers = () => {
    if (!connection) return;

    connection.on('PRCreated', (pr: PRListUpdate) => {
      if (onPRCreated.value) {
        onPRCreated.value(pr);
      }
    });

    connection.on('PRUpdated', (pr: PRListUpdate) => {
      if (onPRUpdated.value) {
        onPRUpdated.value(pr);
      }
    });

    connection.on('PRClosed', (notification: PRClosedNotification) => {
      if (onPRClosed.value) {
        onPRClosed.value(notification);
      }
    });

    connection.on('ReviewAdded', (notification: ReviewNotification) => {
      if (onReviewAdded.value) {
        onReviewAdded.value(notification);
      }
    });

    connection.on('ReviewAddedDetail', (notification: ReviewNotification) => {
      if (onReviewAdded.value) {
        onReviewAdded.value(notification);
      }
    });

    connection.on('CommentChanged', (notification: CommentNotification) => {
      if (onCommentChanged.value) {
        onCommentChanged.value(notification);
      }
    });

    connection.on('CommentChangedDetail', (notification: CommentNotification) => {
      if (onCommentChanged.value) {
        onCommentChanged.value(notification);
      }
    });

    connection.on('ThreadChanged', (notification: ThreadNotification) => {
      if (onThreadChanged.value) {
        onThreadChanged.value(notification);
      }
    });

    connection.on('ThreadChangedDetail', (notification: ThreadNotification) => {
      if (onThreadChanged.value) {
        onThreadChanged.value(notification);
      }
    });

    connection.on('CheckRunsUpdated', (notification: CheckRunsNotification) => {
      if (onCheckRunsUpdated.value) {
        onCheckRunsUpdated.value(notification);
      }
    });

    connection.on('CheckRunsUpdatedDetail', (notification: CheckRunsNotification) => {
      if (onCheckRunsUpdated.value) {
        onCheckRunsUpdated.value(notification);
      }
    });

    connection.on('PendingBranchResolved', (notification: PendingBranchResolvedNotification) => {
      if (onPendingBranchResolved.value) {
        onPendingBranchResolved.value(notification);
      }
    });

    connection.on('PendingBranchAdded', (branch: BranchWithoutPR) => {
      if (onPendingBranchAdded.value) {
        onPendingBranchAdded.value(branch);
      }
    });
  };

  const setupConnectionLifecycleHandlers = () => {
    if (!connection) return;

    connection.onreconnecting(() => {
      connectionState.value = 'reconnecting';
      console.log('SignalR reconnecting...');
    });

    connection.onreconnected(() => {
      connectionState.value = 'connected';
      error.value = null;
      console.log('SignalR reconnected');
    });

    connection.onclose((err) => {
      connectionState.value = 'disconnected';
      if (err) {
        console.error('SignalR connection closed with error:', err);
        error.value = 'Connection lost. Real-time updates disabled.';
      }
    });
  };

  const disconnect = async () => {
    connectionCount = Math.max(0, connectionCount - 1);
    
    if (connectionCount === 0 && connection) {
      try {
        await connection.stop();
      } catch (err) {
        console.error('Error stopping SignalR connection:', err);
      }
      connection = null;
      connectionState.value = 'disconnected';
    }
  };

  const joinPRRoom = async (prId: number) => {
    if (connection && connectionState.value === 'connected') {
      try {
        await connection.invoke('JoinPRRoom', prId);
      } catch (err) {
        console.error('Error joining PR room:', err);
      }
    }
  };

  const leavePRRoom = async (prId: number) => {
    if (connection && connectionState.value === 'connected') {
      try {
        await connection.invoke('LeavePRRoom', prId);
      } catch (err) {
        console.error('Error leaving PR room:', err);
      }
    }
  };

  return {
    connectionState,
    error,
    connect,
    disconnect,
    joinPRRoom,
    leavePRRoom,
    onPRCreated,
    onPRUpdated,
    onPRClosed,
    onReviewAdded,
    onCommentChanged,
    onThreadChanged,
    onCheckRunsUpdated,
    onPendingBranchResolved,
    onPendingBranchAdded
  };
}
