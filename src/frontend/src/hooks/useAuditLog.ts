'use client';

import { useCallback, useEffect, useState } from 'react';
import { ApiErrorKind, isApiError, type ApiError } from '../services/api';
import type { AuditLogService } from '../services/auditLogService';
import type { PaginatedResponse } from '../types/pagination';
import {
  AuditLogSortDirection,
  AuditLogSortField,
  EmptyAuditLogFilters,
  type AuditLogEntry,
  type AuditLogFilters,
  type AuditLogQuery,
} from '../types/audit';

const DefaultPageSize = 20;
const DefaultPage = 1;
const DefaultSortField = AuditLogSortField.Timestamp;
const DefaultSortDirection = AuditLogSortDirection.Descending;

export interface UseAuditLogResult {
  readonly entries: ReadonlyArray<AuditLogEntry>;
  readonly loading: boolean;
  readonly error: string | null;
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly filters: AuditLogFilters;
  readonly sortField: AuditLogSortField;
  readonly sortDirection: AuditLogSortDirection;
  setFilters(next: AuditLogFilters): void;
  setPage(next: number): void;
  setSort(field: AuditLogSortField, direction: AuditLogSortDirection): void;
  resetFilters(): void;
  refresh(): void;
}

export interface UseAuditLogOptions {
  readonly service: AuditLogService;
  readonly pageSize?: number;
  readonly initialFilters?: Partial<AuditLogFilters>;
  readonly initialSortField?: AuditLogSortField;
  readonly initialSortDirection?: AuditLogSortDirection;
}

interface AuditLogState {
  readonly status: 'idle' | 'loading' | 'success' | 'error';
  readonly data: PaginatedResponse<AuditLogEntry> | null;
  readonly error: ApiError | null;
}

const IdleState: AuditLogState = {
  status: 'idle',
  data: null,
  error: null,
};

function describeError(error: ApiError): string {
  if (error.kind === ApiErrorKind.Unauthorized) {
    return 'Tu sesión ya no es válida. Por favor, inicia sesión nuevamente.';
  }
  if (error.kind === ApiErrorKind.Forbidden) {
    return 'No tienes permiso para consultar la bitácora de auditoría.';
  }
  if (error.kind === ApiErrorKind.Network) {
    return 'El servicio no está disponible. Por favor, verifica tu conexión e inténtalo de nuevo.';
  }
  if (error.kind === ApiErrorKind.Validation) {
    return error.message || 'La consulta fue rechazada por el servicio.';
  }
  return 'No se pudo cargar la bitácora de auditoría. Por favor, inténtalo de nuevo.';
}

function normalizeFilters(filters: AuditLogFilters): AuditLogFilters {
  return {
    actorUserId: filters.actorUserId.trim(),
    action: filters.action === '' ? '' : filters.action,
    outcome: filters.outcome === '' ? '' : filters.outcome,
    entityType: filters.entityType.trim(),
    fromDate: filters.fromDate.trim(),
    toDate: filters.toDate.trim(),
    search: filters.search.trim(),
  };
}

export function useAuditLog(options: UseAuditLogOptions): UseAuditLogResult {
  const {
    service,
    pageSize = DefaultPageSize,
    initialFilters,
    initialSortField = DefaultSortField,
    initialSortDirection = DefaultSortDirection,
  } = options;

  const [page, setPageState] = useState<number>(DefaultPage);
  const [filters, setFiltersState] = useState<AuditLogFilters>(() => ({
    ...EmptyAuditLogFilters,
    ...initialFilters,
  }));
  const [sortField, setSortFieldState] = useState<AuditLogSortField>(initialSortField);
  const [sortDirection, setSortDirectionState] = useState<AuditLogSortDirection>(
    initialSortDirection,
  );
  const [state, setState] = useState<AuditLogState>(IdleState);
  const [reloadToken, setReloadToken] = useState<number>(0);

  const normalizedFilters = normalizeFilters(filters);

  useEffect(() => {
    const controller = new AbortController();
    setState((current) => ({ ...current, status: 'loading', error: null }));
    let cancelled = false;

    const query: AuditLogQuery = {
      page,
      pageSize,
      filters: normalizedFilters,
      sortField,
      sortDirection,
    };

    void service
      .search(query, controller.signal)
      .then((data) => {
        if (cancelled || controller.signal.aborted) {
          return;
        }
        setState({ status: 'success', data, error: null });
      })
      .catch((cause: unknown) => {
        if (cancelled || controller.signal.aborted) {
          return;
        }
        const error: ApiError = isApiError(cause)
          ? cause
          : {
              kind: ApiErrorKind.Unknown,
              status: 0,
              message: cause instanceof Error ? cause.message : 'Unknown error',
              code: null,
            };
        setState({ status: 'error', data: null, error });
      });

    return () => {
      cancelled = true;
      controller.abort();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    service,
    page,
    pageSize,
    normalizedFilters.actorUserId,
    normalizedFilters.action,
    normalizedFilters.outcome,
    normalizedFilters.entityType,
    normalizedFilters.fromDate,
    normalizedFilters.toDate,
    normalizedFilters.search,
    sortField,
    sortDirection,
    reloadToken,
  ]);

  const setFilters = useCallback((next: AuditLogFilters) => {
    setFiltersState(next);
    setPageState(DefaultPage);
  }, []);

  const setPage = useCallback((next: number) => {
    if (Number.isInteger(next) && next >= 1) {
      setPageState(next);
    }
  }, []);

  const setSort = useCallback(
    (field: AuditLogSortField, direction: AuditLogSortDirection) => {
      setSortFieldState(field);
      setSortDirectionState(direction);
      setPageState(DefaultPage);
    },
    [],
  );

  const resetFilters = useCallback(() => {
    setFiltersState({ ...EmptyAuditLogFilters, ...initialFilters });
    setPageState(DefaultPage);
  }, [initialFilters]);

  const refresh = useCallback(() => {
    setReloadToken((token) => token + 1);
  }, []);

  const data = state.data;

  return {
    entries: data?.items ?? [],
    loading: state.status === 'loading' || state.status === 'idle',
    error: state.error !== null ? describeError(state.error) : null,
    page: data?.page ?? page,
    pageSize: data?.pageSize ?? pageSize,
    totalItems: data?.totalItems ?? 0,
    totalPages: data?.totalPages ?? 0,
    filters: normalizedFilters,
    sortField,
    sortDirection,
    setFilters,
    setPage,
    setSort,
    resetFilters,
    refresh,
  };
}