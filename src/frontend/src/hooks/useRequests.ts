'use client';

import { useCallback, useEffect, useState } from 'react';
import { ApiErrorKind, isApiError, type ApiError } from '../services/api';
import type { RequestService } from '../services/requestService';
import type { PaginatedResponse } from '../types/pagination';
import {
  RequestSortDirection,
  RequestSortField,
  type RequestFilters,
  type RequestQuery,
  type RequestRecord,
  EmptyRequestFilters,
} from '../types/request';

const DefaultPageSize = 20;
const DefaultPage = 1;
const DefaultSortField = RequestSortField.CreatedAt;
const DefaultSortDirection = RequestSortDirection.Descending;

export interface UseRequestsResult {
  readonly requests: ReadonlyArray<RequestRecord>;
  readonly loading: boolean;
  readonly error: string | null;
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly filters: RequestFilters;
  readonly sortField: RequestSortField;
  readonly sortDirection: RequestSortDirection;
  setSearch(next: string): void;
  setCode(next: string): void;
  setFilters(next: Partial<RequestFilters>): void;
  setPage(next: number): void;
  setSort(field: RequestSortField, direction: RequestSortDirection): void;
  resetFilters(): void;
  refresh(): void;
}

export interface UseRequestsOptions {
  readonly service: RequestService;
  readonly pageSize?: number;
  readonly initialFilters?: Partial<RequestFilters>;
  readonly initialSortField?: RequestSortField;
  readonly initialSortDirection?: RequestSortDirection;
}

interface RequestState {
  readonly status: 'idle' | 'loading' | 'success' | 'error';
  readonly data: PaginatedResponse<RequestRecord> | null;
  readonly error: ApiError | null;
}

const IdleRequest: RequestState = {
  status: 'idle',
  data: null,
  error: null,
};

function describeError(error: ApiError): string {
  if (error.kind === ApiErrorKind.Unauthorized) {
    return 'Tu sesión ya no es válida. Por favor, inicia sesión nuevamente.';
  }
  if (error.kind === ApiErrorKind.Forbidden) {
    return 'No tienes permiso para ver las solicitudes.';
  }
  if (error.kind === ApiErrorKind.Network) {
    return 'El servicio no está disponible. Por favor, verifica tu conexión e inténtalo de nuevo.';
  }
  if (error.kind === ApiErrorKind.Validation) {
    return error.message || 'La solicitud fue rechazada por el servicio.';
  }
  return 'No se pudieron cargar las solicitudes. Por favor, inténtalo de nuevo.';
}

function normalizeFilters(filters: RequestFilters): RequestFilters {
  return {
    status: filters.status ?? null,
    priority: filters.priority ?? null,
    areaId: filters.areaId && filters.areaId.length > 0 ? filters.areaId : null,
    requestTypeId:
      filters.requestTypeId && filters.requestTypeId.length > 0 ? filters.requestTypeId : null,
    requesterId:
      filters.requesterId && filters.requesterId.length > 0 ? filters.requesterId : null,
    responsibleId:
      filters.responsibleId && filters.responsibleId.length > 0
        ? filters.responsibleId
        : null,
    fromDate: filters.fromDate && filters.fromDate.length > 0 ? filters.fromDate : null,
    toDate: filters.toDate && filters.toDate.length > 0 ? filters.toDate : null,
    code: filters.code.trim(),
    search: filters.search.trim(),
  };
}

function mergeFilters(
  base: RequestFilters,
  patch: Partial<RequestFilters>,
): RequestFilters {
  return {
    ...base,
    ...patch,
  };
}

export function useRequests(options: UseRequestsOptions): UseRequestsResult {
  const {
    service,
    pageSize = DefaultPageSize,
    initialFilters,
    initialSortField = DefaultSortField,
    initialSortDirection = DefaultSortDirection,
  } = options;

  const [page, setPageState] = useState<number>(DefaultPage);
  const [filters, setFiltersState] = useState<RequestFilters>(() => ({
    ...EmptyRequestFilters,
    ...initialFilters,
  }));
  const [sortField, setSortFieldState] = useState<RequestSortField>(initialSortField);
  const [sortDirection, setSortDirectionState] = useState<RequestSortDirection>(
    initialSortDirection,
  );
  const [requestState, setRequestState] = useState<RequestState>(IdleRequest);
  const [reloadToken, setReloadToken] = useState<number>(0);

  const normalizedFilters = normalizeFilters(filters);

  useEffect(() => {
    const controller = new AbortController();
    setRequestState((current) => ({ ...current, status: 'loading', error: null }));
    let cancelled = false;

    const query: RequestQuery = {
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
        setRequestState({ status: 'success', data, error: null });
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
        setRequestState({ status: 'error', data: null, error });
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
    normalizedFilters.status,
    normalizedFilters.priority,
    normalizedFilters.areaId,
    normalizedFilters.requestTypeId,
    normalizedFilters.requesterId,
    normalizedFilters.responsibleId,
    normalizedFilters.fromDate,
    normalizedFilters.toDate,
    normalizedFilters.code,
    normalizedFilters.search,
    sortField,
    sortDirection,
    reloadToken,
  ]);

  const setSearch = useCallback((next: string) => {
    setFiltersState((current) => ({ ...current, search: next }));
    setPageState(DefaultPage);
  }, []);

  const setCode = useCallback((next: string) => {
    setFiltersState((current) => ({ ...current, code: next }));
    setPageState(DefaultPage);
  }, []);

  const setFilters = useCallback((next: Partial<RequestFilters>) => {
    setFiltersState((current) => mergeFilters(current, next));
    setPageState(DefaultPage);
  }, []);

  const resetFilters = useCallback(() => {
    setFiltersState({ ...EmptyRequestFilters, ...initialFilters });
    setPageState(DefaultPage);
  }, [initialFilters]);

  const setPage = useCallback((next: number) => {
    if (Number.isInteger(next) && next >= 1) {
      setPageState(next);
    }
  }, []);

  const setSort = useCallback((field: RequestSortField, direction: RequestSortDirection) => {
    setSortFieldState(field);
    setSortDirectionState(direction);
    setPageState(DefaultPage);
  }, []);

  const refresh = useCallback(() => {
    setReloadToken((token) => token + 1);
  }, []);

  const data = requestState.data;

  return {
    requests: data?.items ?? [],
    loading: requestState.status === 'loading' || requestState.status === 'idle',
    error: requestState.error !== null ? describeError(requestState.error) : null,
    page: data?.page ?? page,
    pageSize: data?.pageSize ?? pageSize,
    totalItems: data?.totalItems ?? 0,
    totalPages: data?.totalPages ?? 0,
    filters: normalizedFilters,
    sortField,
    sortDirection,
    setSearch,
    setCode,
    setFilters,
    setPage,
    setSort,
    resetFilters,
    refresh,
  };
}