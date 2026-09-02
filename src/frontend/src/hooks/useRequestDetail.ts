'use client';

import { useCallback, useEffect, useState } from 'react';
import { ApiErrorKind, isApiError, type ApiError } from '../services/api';
import type { RequestService } from '../services/requestService';
import type { RequestDetail } from '../types/request';

export interface UseRequestDetailResult {
  readonly request: RequestDetail | null;
  readonly loading: boolean;
  readonly error: string | null;
  refresh(): void;
}

export interface UseRequestDetailOptions {
  readonly service: RequestService;
  readonly requestId: string | null | undefined;
}

function describeError(error: ApiError): string {
  if (error.kind === ApiErrorKind.Unauthorized) {
    return 'Tu sesión ya no es válida. Por favor, inicia sesión nuevamente.';
  }
  if (error.kind === ApiErrorKind.Forbidden) {
    return 'No tienes permiso para ver esta solicitud.';
  }
  if (error.kind === ApiErrorKind.NotFound) {
    return 'La solicitud solicitada no existe o fue eliminada.';
  }
  if (error.kind === ApiErrorKind.Network) {
    return 'El servicio no está disponible. Por favor, verifica tu conexión e inténtalo de nuevo.';
  }
  if (error.kind === ApiErrorKind.Validation) {
    return error.message || 'La solicitud fue rechazada por el servicio.';
  }
  return 'No se pudo cargar la solicitud. Por favor, inténtalo de nuevo.';
}

interface DetailState {
  readonly status: 'idle' | 'loading' | 'success' | 'error';
  readonly data: RequestDetail | null;
  readonly error: ApiError | null;
}

const IdleState: DetailState = { status: 'idle', data: null, error: null };

export function useRequestDetail(options: UseRequestDetailOptions): UseRequestDetailResult {
  const { service, requestId } = options;
  const [state, setState] = useState<DetailState>(IdleState);
  const [reloadToken, setReloadToken] = useState<number>(0);

  useEffect(() => {
    if (requestId === null || requestId === undefined || requestId.length === 0) {
      setState(IdleState);
      return;
    }

    const controller = new AbortController();
    setState((current) => ({ ...current, status: 'loading', error: null }));
    let cancelled = false;

    void service
      .getById(requestId, controller.signal)
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
  }, [service, requestId, reloadToken]);

  const refresh = useCallback(() => {
    setReloadToken((token) => token + 1);
  }, []);

  return {
    request: state.data,
    loading: state.status === 'loading' || state.status === 'idle',
    error: state.error !== null ? describeError(state.error) : null,
    refresh,
  };
}