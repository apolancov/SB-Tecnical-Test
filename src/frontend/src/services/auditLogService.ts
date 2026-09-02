import type { ApiClient } from './api';
import type {
  AuditLogEntry,
  AuditLogFilters,
  AuditLogQuery,
} from '../types/audit';
import type { PaginatedResponse } from '../types/pagination';

export interface AuditLogService {
  search(
    query: AuditLogQuery,
    signal?: AbortSignal,
  ): Promise<PaginatedResponse<AuditLogEntry>>;
  getById(id: string, signal?: AbortSignal): Promise<AuditLogEntry>;
}

function buildFilters(
  filters: AuditLogFilters,
): Record<string, string | number | undefined> {
  const trimmedSearch = filters.search.trim();
  const trimmedEntityType = filters.entityType.trim();
  const trimmedActorId = filters.actorUserId.trim();
  const trimmedFromDate = filters.fromDate.trim();
  const trimmedToDate = filters.toDate.trim();

  return {
    actorUserId:
      trimmedActorId.length > 0 && trimmedActorId !== '00000000-0000-0000-0000-000000000000'
        ? trimmedActorId
        : undefined,
    action: filters.action === '' ? undefined : filters.action,
    outcome: filters.outcome === '' ? undefined : filters.outcome,
    entityType: trimmedEntityType.length > 0 ? trimmedEntityType : undefined,
    fromDate: trimmedFromDate.length > 0 ? trimmedFromDate : undefined,
    toDate: trimmedToDate.length > 0 ? trimmedToDate : undefined,
    search: trimmedSearch.length > 0 ? trimmedSearch : undefined,
  };
}

export class DefaultAuditLogService implements AuditLogService {
  private readonly apiClient: ApiClient;

  constructor(apiClient: ApiClient) {
    this.apiClient = apiClient;
  }

  search(
    query: AuditLogQuery,
    signal?: AbortSignal,
  ): Promise<PaginatedResponse<AuditLogEntry>> {
    return this.apiClient.get<PaginatedResponse<AuditLogEntry>>(
      '/api/auditoria',
      {
        page: query.page,
        pageSize: query.pageSize,
        sortBy: query.sortField,
        sortDirection: query.sortDirection,
        ...buildFilters(query.filters),
      },
      signal,
    );
  }

  getById(id: string, signal?: AbortSignal): Promise<AuditLogEntry> {
    return this.apiClient.get<AuditLogEntry>(
      `/api/auditoria/${encodeURIComponent(id)}`,
      undefined,
      signal,
    );
  }
}