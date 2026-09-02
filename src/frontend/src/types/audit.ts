export const AuditAction = {
  LoginSucceeded: 'LoginSucceeded',
  LoginFailed: 'LoginFailed',
  RequestCreated: 'RequestCreated',
  RequestUpdated: 'RequestUpdated',
  RequestAssigned: 'RequestAssigned',
  RequestStatusChanged: 'RequestStatusChanged',
  RequestReopened: 'RequestReopened',
  RequestCommentAdded: 'RequestCommentAdded',
  InstitutionCreated: 'InstitutionCreated',
  InstitutionUpdated: 'InstitutionUpdated',
  InstitutionDeleted: 'InstitutionDeleted',
  AuthorizationDenied: 'AuthorizationDenied',
} as const;

export type AuditAction = (typeof AuditAction)[keyof typeof AuditAction];

export const AuditOutcome = {
  Success: 'Success',
  Failure: 'Failure',
  Denied: 'Denied',
} as const;

export type AuditOutcome = (typeof AuditOutcome)[keyof typeof AuditOutcome];

export const AuditLogSortField = {
  Timestamp: 'Timestamp',
  Action: 'Action',
  Outcome: 'Outcome',
  EntityType: 'EntityType',
  ActorUserName: 'ActorUserName',
} as const;

export type AuditLogSortField =
  (typeof AuditLogSortField)[keyof typeof AuditLogSortField];

export const AuditLogSortDirection = {
  Ascending: 'Ascending',
  Descending: 'Descending',
} as const;

export type AuditLogSortDirection =
  (typeof AuditLogSortDirection)[keyof typeof AuditLogSortDirection];

export interface AuditLogEntry {
  readonly id: string;
  readonly timestamp: string;
  readonly action: AuditAction;
  readonly outcome: AuditOutcome;
  readonly entityType: string;
  readonly entityId: string | null;
  readonly details: string | null;
  readonly ipAddress: string | null;
  readonly actorUserId: string | null;
  readonly actorUserName: string | null;
}

export interface AuditLogFilters {
  readonly actorUserId: string;
  readonly action: AuditAction | '';
  readonly outcome: AuditOutcome | '';
  readonly entityType: string;
  readonly fromDate: string;
  readonly toDate: string;
  readonly search: string;
}

export interface AuditLogQuery {
  readonly page: number;
  readonly pageSize: number;
  readonly filters: AuditLogFilters;
  readonly sortField: AuditLogSortField;
  readonly sortDirection: AuditLogSortDirection;
}

export const EmptyAuditLogFilters: AuditLogFilters = {
  actorUserId: '',
  action: '',
  outcome: '',
  entityType: '',
  fromDate: '',
  toDate: '',
  search: '',
};

export function describeAuditAction(action: AuditAction): string {
  switch (action) {
    case AuditAction.LoginSucceeded:
      return 'Inicio de sesión exitoso';
    case AuditAction.LoginFailed:
      return 'Inicio de sesión fallido';
    case AuditAction.RequestCreated:
      return 'Solicitud creada';
    case AuditAction.RequestUpdated:
      return 'Solicitud actualizada';
    case AuditAction.RequestAssigned:
      return 'Solicitud asignada';
    case AuditAction.RequestStatusChanged:
      return 'Estado de solicitud cambiado';
    case AuditAction.RequestReopened:
      return 'Solicitud reabierta';
    case AuditAction.RequestCommentAdded:
      return 'Comentario agregado a solicitud';
    case AuditAction.InstitutionCreated:
      return 'Institución creada';
    case AuditAction.InstitutionUpdated:
      return 'Institución actualizada';
    case AuditAction.InstitutionDeleted:
      return 'Institución eliminada';
    case AuditAction.AuthorizationDenied:
      return 'Autorización denegada';
    default:
      return action;
  }
}

export function describeAuditOutcome(outcome: AuditOutcome): string {
  switch (outcome) {
    case AuditOutcome.Success:
      return 'Éxito';
    case AuditOutcome.Failure:
      return 'Fallo';
    case AuditOutcome.Denied:
      return 'Denegado';
    default:
      return outcome;
  }
}