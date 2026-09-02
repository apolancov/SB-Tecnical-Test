import type { RequestPriority, RequestStatus, RequestSummary } from './request';

export interface DashboardSummary {
  readonly totalRequests: number;
  readonly requestsByStatus: Readonly<Record<RequestStatus, number>>;
  readonly requestsByPriority: Readonly<Record<RequestPriority, number>>;
  readonly overdueRequests: number;
  readonly pendingRequests: number;
  readonly assignedRequests: number;
  readonly unassignedRequests: number;
  readonly recentRequests: ReadonlyArray<RequestSummary>;
  readonly generatedAtUtc: string;
}

export const EmptyDashboardSummary: DashboardSummary = {
  totalRequests: 0,
  requestsByStatus: {
    Draft: 0,
    Submitted: 0,
    InReview: 0,
    Assigned: 0,
    InProgress: 0,
    OnHold: 0,
    Resolved: 0,
    Closed: 0,
    Cancelled: 0,
  },
  requestsByPriority: {
    Low: 0,
    Medium: 0,
    High: 0,
    Critical: 0,
  },
  overdueRequests: 0,
  pendingRequests: 0,
  assignedRequests: 0,
  unassignedRequests: 0,
  recentRequests: [],
  generatedAtUtc: '',
};