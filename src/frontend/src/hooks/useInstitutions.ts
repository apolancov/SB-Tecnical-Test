'use client';

import { useCallback, useEffect, useState } from 'react';
import type { Institution, InstitutionFilters, PaginatedResponse } from '../types/institution';
import type { InstitutionService } from '../services/institutionService';
import { ApiErrorKind, isApiError, type ApiError } from '../services/api';

export interface UseInstitutionsResult {
  readonly institutions: ReadonlyArray<Institution>;
  readonly loading: boolean;
  readonly error: string | null;
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly filters: InstitutionFilters;
  readonly setFilters: (next: InstitutionFilters) => void;
  readonly setPage: (page: number) => void;
  readonly refresh: () => void;
}

const DefaultPageSize = 20;
const DefaultPage = 1;

export const DefaultInstitutionFilters: InstitutionFilters = {
  name: '',
  category: '',
  statePower: '',
  sector: '',
};

export interface UseInstitutionsOptions {
  readonly service: InstitutionService;
  readonly pageSize?: number;
}

interface RequestState {
  readonly status: 'idle' | 'loading' | 'success' | 'error';
  readonly data: PaginatedResponse<Institution> | null;
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
    return 'No tienes permiso para ver las instituciones.';
  }
  if (error.kind === ApiErrorKind.Network) {
    return 'El servicio no está disponible. Por favor, verifica tu conexión e inténtalo de nuevo.';
  }
  if (error.kind === ApiErrorKind.Validation) {
    return error.message || 'La solicitud fue rechazada por el servicio.';
  }
  console.log(error)
  return 'No se pudieron cargar las instituciones. Por favor, inténtalo de nuevo.';
}

export function useInstitutions(options: UseInstitutionsOptions): UseInstitutionsResult {
  const { service, pageSize = DefaultPageSize } = options;

  const [page, setPageState] = useState<number>(DefaultPage);
  const [filters, setFiltersState] = useState<InstitutionFilters>(DefaultInstitutionFilters);
  const [requestState, setRequestState] = useState<RequestState>(IdleRequest);
  const [reloadToken, setReloadToken] = useState<number>(0);

  useEffect(() => {
    const controller = new AbortController();
    setRequestState((current) => ({ ...current, status: 'loading', error: null }));
    let cancelled = false;

    void service
      .search({ page, pageSize, filters }, controller.signal)
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
  }, [service, page, pageSize, filters, reloadToken]);

  const setFilters = useCallback((next: InstitutionFilters) => {
    setFiltersState(next);
    setPageState(DefaultPage);
  }, []);

  const setPage = useCallback((next: number) => {
    if (Number.isInteger(next) && next >= 1) {
      setPageState(next);
    }
  }, []);

  const refresh = useCallback(() => {
    setReloadToken((token) => token + 1);
  }, []);

  const data = requestState.data;

  return {
    institutions: data?.items ?? [],
    loading: requestState.status === 'loading' || requestState.status === 'idle',
    error: requestState.error !== null ? describeError(requestState.error) : null,
    page: data?.page ?? page,
    pageSize: data?.pageSize ?? pageSize,
    totalItems: data?.totalItems ?? 0,
    totalPages: data?.totalPages ?? 0,
    filters,
    setFilters,
    setPage,
    refresh,
  };
}
