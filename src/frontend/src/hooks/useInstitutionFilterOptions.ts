'use client';

import { useEffect, useState } from 'react';
import { ApiErrorKind, isApiError, type ApiError } from '../services/api';
import type { InstitutionService } from '../services/institutionService';
import type { InstitutionFilterOptions } from '../types/institution';

export interface UseInstitutionFilterOptionsResult {
  readonly options: InstitutionFilterOptions;
  readonly loading: boolean;
  readonly error: string | null;
  readonly refresh: () => void;
}

export const EmptyInstitutionFilterOptions: InstitutionFilterOptions = {
  categories: [],
  statePowers: [],
  sectors: [],
};

function describeError(error: ApiError): string {
  if (error.kind === ApiErrorKind.Unauthorized) {
    return 'Tu sesión ya no es válida. Por favor, inicia sesión nuevamente.';
  }
  if (error.kind === ApiErrorKind.Network) {
    return 'No se pudieron cargar las opciones de filtro. Por favor, verifica tu conexión.';
  }
  return 'No se pudieron cargar las opciones de filtro.';
}

export interface UseInstitutionFilterOptionsOptions {
  readonly service: InstitutionService;
}

export function useInstitutionFilterOptions(
  options: UseInstitutionFilterOptionsOptions,
): UseInstitutionFilterOptionsResult {
  const { service } = options;
  const [data, setData] = useState<InstitutionFilterOptions>(EmptyInstitutionFilterOptions);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<ApiError | null>(null);
  const [reloadToken, setReloadToken] = useState<number>(0);

  useEffect(() => {
    const controller = new AbortController();
    let cancelled = false;
    setLoading(true);
    setError(null);

    service
      .getFilterOptions(controller.signal)
      .then((result) => {
        if (cancelled || controller.signal.aborted) {
          return;
        }
        setData(result);
        setLoading(false);
      })
      .catch((cause: unknown) => {
        if (cancelled || controller.signal.aborted) {
          return;
        }
        const next: ApiError = isApiError(cause)
          ? cause
          : {
              kind: ApiErrorKind.Unknown,
              status: 0,
              message: cause instanceof Error ? cause.message : 'Unknown error',
              code: null,
            };
        setError(next);
        setLoading(false);
      });

    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [service, reloadToken]);

  const refresh = () => {
    setReloadToken((token) => token + 1);
  };

  return {
    options: data,
    loading,
    error: error !== null ? describeError(error) : null,
    refresh,
  };
}
