export const RequestStatus = {
  Draft: 'Draft',
  Submitted: 'Submitted',
  InReview: 'InReview',
  Assigned: 'Assigned',
  InProgress: 'InProgress',
  OnHold: 'OnHold',
  Resolved: 'Resolved',
  Closed: 'Closed',
  Cancelled: 'Cancelled',
} as const;

export type RequestStatus = (typeof RequestStatus)[keyof typeof RequestStatus];

export const RequestPriority = {
  Low: 'Low',
  Medium: 'Medium',
  High: 'High',
  Critical: 'Critical',
} as const;

export type RequestPriority = (typeof RequestPriority)[keyof typeof RequestPriority];

export const CommentVisibility = {
  Requester: 'Requester',
  Internal: 'Internal',
} as const;

export type CommentVisibility = (typeof CommentVisibility)[keyof typeof CommentVisibility];

export const RequestSortField = {
  CreatedAt: 'CreatedAt',
  Code: 'Code',
  Title: 'Title',
  Priority: 'Priority',
  Status: 'Status',
  DueDate: 'DueDate',
} as const;

export type RequestSortField = (typeof RequestSortField)[keyof typeof RequestSortField];

export const RequestSortDirection = {
  Ascending: 'Ascending',
  Descending: 'Descending',
} as const;

export type RequestSortDirection = (typeof RequestSortDirection)[keyof typeof RequestSortDirection];

export interface RequestUserSummary {
  readonly id: string;
  readonly username: string;
  readonly email: string;
  readonly role: string;
}

export interface RequestLookupSummary {
  readonly id: string;
  readonly name: string;
}

export interface RequestSummary {
  readonly id: string;
  readonly code: string;
  readonly title: string;
  readonly priority: RequestPriority;
  readonly status: RequestStatus;
  readonly createdAt: string;
  readonly dueDate: string | null;
  readonly requesterId: string;
  readonly requesterUsername: string;
  readonly responsibleId: string | null;
  readonly responsibleUsername: string | null;
}

export interface RequestRecord extends RequestSummary {
  readonly description: string;
  readonly evidenceUrl: string | null;
  readonly closedAt: string | null;
  readonly areaId: string;
  readonly area: string;
  readonly requestTypeId: string;
  readonly requestType: string;
  readonly requesterEmail: string;
  readonly responsibleEmail: string | null;
}

export interface RequestStatusHistoryEntry {
  readonly id: string;
  readonly previousStatus: RequestStatus;
  readonly newStatus: RequestStatus;
  readonly date: string;
  readonly comment: string;
  readonly changedById: string;
  readonly changedByUsername: string;
}

export interface RequestComment {
  readonly id: string;
  readonly text: string;
  readonly visibility: CommentVisibility;
  readonly date: string;
  readonly authorId: string;
  readonly authorUsername: string;
}

export interface RequestDetail extends Omit<RequestRecord, 'area' | 'requestType'> {
  readonly requester: RequestUserSummary;
  readonly responsible: RequestUserSummary | null;
  readonly area: RequestLookupSummary;
  readonly requestType: RequestLookupSummary;
  readonly statusHistory: ReadonlyArray<RequestStatusHistoryEntry>;
  readonly comments: ReadonlyArray<RequestComment>;
}

export interface RequestFilters {
  readonly status: RequestStatus | null;
  readonly priority: RequestPriority | null;
  readonly areaId: string | null;
  readonly requestTypeId: string | null;
  readonly requesterId: string | null;
  readonly responsibleId: string | null;
  readonly fromDate: string | null;
  readonly toDate: string | null;
  readonly code: string;
  readonly search: string;
}

export interface RequestQuery {
  readonly page: number;
  readonly pageSize: number;
  readonly filters: RequestFilters;
  readonly sortField: RequestSortField;
  readonly sortDirection: RequestSortDirection;
}

export const EmptyRequestFilters: RequestFilters = {
  status: null,
  priority: null,
  areaId: null,
  requestTypeId: null,
  requesterId: null,
  responsibleId: null,
  fromDate: null,
  toDate: null,
  code: '',
  search: '',
};

export interface CreateRequestInput {
  readonly title: string;
  readonly description: string;
  readonly priority: RequestPriority;
  readonly areaId: string;
  readonly requestTypeId: string;
  readonly dueDate: string | null;
  readonly evidenceUrl: string | null;
}

export interface UpdateRequestInput {
  readonly title: string;
  readonly description: string;
  readonly priority: RequestPriority;
  readonly dueDate: string | null;
  readonly evidenceUrl: string | null;
}

export interface ChangeRequestStatusInput {
  readonly newStatus: RequestStatus;
  readonly comment: string;
}

export interface AssignRequestInput {
  readonly responsibleUserId: string;
  readonly comment: string;
}

export interface ReopenRequestInput {
  readonly targetStatus: RequestStatus;
  readonly comment: string;
}

export interface AddCommentInput {
  readonly text: string;
  readonly visibility: CommentVisibility;
}

export const RequestTitleMaximumLength = 256;
export const RequestDescriptionMaximumLength = 4096;
export const RequestCommentMaximumLength = 1024;
export const RequestEvidenceUrlMaximumLength = 2048;

export interface RequestOptionList<T extends string | number> {
  readonly value: T;
  readonly name: string;
}

export function describeRequestStatus(status: RequestStatus): string {
  switch (status) {
    case RequestStatus.Draft:
      return 'Borrador';
    case RequestStatus.Submitted:
      return 'Registrada';
    case RequestStatus.InReview:
      return 'En análisis';
    case RequestStatus.Assigned:
      return 'Asignada';
    case RequestStatus.InProgress:
      return 'En progreso';
    case RequestStatus.OnHold:
      return 'En espera del solicitante';
    case RequestStatus.Resolved:
      return 'Resuelta';
    case RequestStatus.Closed:
      return 'Cerrada';
    case RequestStatus.Cancelled:
      return 'Cancelada';
    default:
      return status;
  }
}

export function describeRequestPriority(priority: RequestPriority): string {
  switch (priority) {
    case RequestPriority.Low:
      return 'Baja';
    case RequestPriority.Medium:
      return 'Media';
    case RequestPriority.High:
      return 'Alta';
    case RequestPriority.Critical:
      return 'Crítica';
    default:
      return priority;
  }
}

export function describeCommentVisibility(visibility: CommentVisibility): string {
  switch (visibility) {
    case CommentVisibility.Requester:
      return 'Público';
    case CommentVisibility.Internal:
      return 'Interno';
    default:
      return visibility;
  }
}