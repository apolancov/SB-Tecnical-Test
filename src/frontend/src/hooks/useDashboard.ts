'use client';

import { useCallback, useEffect, useState } from 'react';
import { ApiErrorKind, isApiError, type ApiError } from '../services/api';
import type { DashboardService } from '../services/dashboardService';
import {
  EmptyDashboardSummary,
  type DashboardSummary,
} from '../types/dashboard';

export interface UseDashboardResult {
  readonly summary: DashboardSummary;
  readonly loading: boolean;
  readonly error: string | null;
  refresh(): void;
}

export interface UseDashboardOptions {
  readonly service: DashboardService;
}

function describeError(error: ApiError): string {
  if (error.kind === ApiErrorKind.Unauthorized) {
    return 'Tu sesión ya no es válida. Por favor, inicia sesión nuevamente.';
  }
  if (error.kind === ApiErrorKind.Forbidden) {
    return 'No tienes permiso para ver el resumen.';
  }
  if (error.kind === ApiErrorKind.Network) {
    return 'El servicio no está disponible. Por favor, verifica tu conexión e inténtalo de nuevo.';
  }
  if (error.kind === ApiErrorKind.Validation) {
    return error.message || 'La solicitud fue rechazada por el servicio.';
  }
  return 'No se pudo cargar el resumen. Por favor, inténtalo de nuevo.';
}

interface DashboardState {
  readonly status: 'idle' | 'loading' | 'success' | 'error';
  readonly data: DashboardSummary;
  readonly error: ApiError | null;
}

const IdleState: DashboardState = {
  status: 'idle',
  data: EmptyDashboardSummary,
  error: null,
};

export function useDashboard(options: UseDashboardOptions): UseDashboardResult {
  const { service } = options;
  const [state, setState] = useState<DashboardState>(IdleState);
  const [reloadToken, setReloadToken] = useState<number>(0);

  useEffect(() => {
    const controller = new AbortController();
    setState((current) => ({ ...current, status: 'loading', error: null }));
    let cancelled = false;

    service
      .getSummary(controller.signal)
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
        setState({ status: 'error', data: EmptyDashboardSummary, error });
      });

    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [service, reloadToken]);

  const refresh = useCallback(() => {
    setReloadToken((token) => token + 1);
  }, []);

  return {
    summary: state.data,
    loading: state.status === 'loading' || state.status === 'idle',
    error: state.error !== null ? describeError(state.error) : null,
    refresh,
  };
}