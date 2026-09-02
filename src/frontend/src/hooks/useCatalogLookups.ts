'use client';

import { useEffect, useState } from 'react';
import { ApiErrorKind, isApiError, type ApiError } from '../services/api';
import type { CatalogService, StaffCandidate } from '../services/catalogService';
import {
  EmptyAreas,
  EmptyRequestTypes,
  type AreaRecord,
  type RequestTypeRecord,
} from '../types/catalog';

export interface UseCatalogAreasResult {
  readonly areas: ReadonlyArray<AreaRecord>;
  readonly loading: boolean;
  readonly error: string | null;
  refresh(): void;
}

export interface UseCatalogAreasOptions {
  readonly service: CatalogService;
}

function describeError(error: ApiError): string {
  if (error.kind === ApiErrorKind.Unauthorized) {
    return 'Tu sesión ya no es válida. Por favor, inicia sesión nuevamente.';
  }
  if (error.kind === ApiErrorKind.Forbidden) {
    return 'No tienes permiso para ver las áreas.';
  }
  if (error.kind === ApiErrorKind.Network) {
    return 'El servicio no está disponible. Por favor, verifica tu conexión e inténtalo de nuevo.';
  }
  return 'No se pudieron cargar las áreas.';
}

interface AreasState {
  readonly status: 'idle' | 'loading' | 'success' | 'error';
  readonly data: ReadonlyArray<AreaRecord>;
  readonly error: ApiError | null;
}

const IdleAreasState: AreasState = {
  status: 'idle',
  data: EmptyAreas,
  error: null,
};

export function useCatalogAreas(options: UseCatalogAreasOptions): UseCatalogAreasResult {
  const { service } = options;
  const [state, setState] = useState<AreasState>(IdleAreasState);
  const [reloadToken, setReloadToken] = useState<number>(0);

  useEffect(() => {
    const controller = new AbortController();
    setState((current) => ({ ...current, status: 'loading', error: null }));
    let cancelled = false;

    service
      .listAreas(controller.signal)
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
        setState({ status: 'error', data: EmptyAreas, error });
      });

    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [service, reloadToken]);

  return {
    areas: state.data,
    loading: state.status === 'loading' || state.status === 'idle',
    error: state.error !== null ? describeError(state.error) : null,
    refresh: () => setReloadToken((token) => token + 1),
  };
}

export interface UseCatalogRequestTypesResult {
  readonly requestTypes: ReadonlyArray<RequestTypeRecord>;
  readonly loading: boolean;
  readonly error: string | null;
  refresh(): void;
}

interface RequestTypesState {
  readonly status: 'idle' | 'loading' | 'success' | 'error';
  readonly data: ReadonlyArray<RequestTypeRecord>;
  readonly error: ApiError | null;
}

const IdleRequestTypesState: RequestTypesState = {
  status: 'idle',
  data: EmptyRequestTypes,
  error: null,
};

function describeRequestTypesError(error: ApiError): string {
  if (error.kind === ApiErrorKind.Unauthorized) {
    return 'Tu sesión ya no es válida. Por favor, inicia sesión nuevamente.';
  }
  if (error.kind === ApiErrorKind.Forbidden) {
    return 'No tienes permiso para ver los tipos de solicitud.';
  }
  if (error.kind === ApiErrorKind.Network) {
    return 'El servicio no está disponible. Por favor, verifica tu conexión e inténtalo de nuevo.';
  }
  return 'No se pudieron cargar los tipos de solicitud.';
}

export function useCatalogRequestTypes(
  options: UseCatalogAreasOptions,
): UseCatalogRequestTypesResult {
  const { service } = options;
  const [state, setState] = useState<RequestTypesState>(IdleRequestTypesState);
  const [reloadToken, setReloadToken] = useState<number>(0);

  useEffect(() => {
    const controller = new AbortController();
    setState((current) => ({ ...current, status: 'loading', error: null }));
    let cancelled = false;

    service
      .listRequestTypes(controller.signal)
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
        setState({ status: 'error', data: EmptyRequestTypes, error });
      });

    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [service, reloadToken]);

  return {
    requestTypes: state.data,
    loading: state.status === 'loading' || state.status === 'idle',
    error: state.error !== null ? describeRequestTypesError(state.error) : null,
    refresh: () => setReloadToken((token) => token + 1),
  };
}

export const EmptyStaffCandidates: ReadonlyArray<StaffCandidate> = [];

export interface UseCatalogStaffResult {
  readonly staff: ReadonlyArray<StaffCandidate>;
  readonly loading: boolean;
  readonly error: string | null;
  refresh(): void;
}

interface StaffState {
  readonly status: 'idle' | 'loading' | 'success' | 'error';
  readonly data: ReadonlyArray<StaffCandidate>;
  readonly error: ApiError | null;
}

const IdleStaffState: StaffState = {
  status: 'idle',
  data: EmptyStaffCandidates,
  error: null,
};

function describeStaffError(error: ApiError): string {
  if (error.kind === ApiErrorKind.Unauthorized) {
    return 'Tu sesión ya no es válida. Por favor, inicia sesión nuevamente.';
  }
  if (error.kind === ApiErrorKind.Forbidden) {
    return 'No tienes permiso para asignar solicitudes.';
  }
  if (error.kind === ApiErrorKind.Network) {
    return 'El servicio no está disponible. Por favor, verifica tu conexión e inténtalo de nuevo.';
  }
  return 'No se pudo cargar el catálogo de responsables.';
}

export interface UseCatalogStaffOptions {
  readonly service: CatalogService;
  readonly enabled?: boolean;
}

export function useCatalogStaff(options: UseCatalogStaffOptions): UseCatalogStaffResult {
  const { service, enabled = true } = options;
  const [state, setState] = useState<StaffState>(IdleStaffState);
  const [reloadToken, setReloadToken] = useState<number>(0);

  useEffect(() => {
    if (!enabled) {
      setState(IdleStaffState);
      return () => undefined;
    }

    const controller = new AbortController();
    setState((current) => ({ ...current, status: 'loading', error: null }));
    let cancelled = false;

    service
      .listStaffCandidates(controller.signal)
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
        setState({ status: 'error', data: EmptyStaffCandidates, error });
      });

    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [service, enabled, reloadToken]);

  return {
    staff: state.data,
    loading: state.status === 'loading' || state.status === 'idle',
    error: state.error !== null ? describeStaffError(state.error) : null,
    refresh: () => setReloadToken((token) => token + 1),
  };
}