'use client';

import { useCallback, useState } from 'react';
import { ApiErrorKind, isApiError, type ApiError } from '../services/api';
import type { InstitutionService } from '../services/institutionService';
import type { Institution, InstitutionInput } from '../types/institution';

export type InstitutionMutationKind = 'create' | 'update' | 'delete';

export interface InstitutionMutationState {
  readonly submitting: boolean;
  readonly error: string | null;
}

export interface InstitutionMutationCallbacks {
  readonly onCreated?: (institution: Institution) => void;
  readonly onUpdated?: (institution: Institution) => void;
  readonly onDeleted?: (id: string) => void;
}

export interface UseInstitutionMutationOptions {
  readonly service: InstitutionService;
  readonly callbacks?: InstitutionMutationCallbacks;
}

export interface UseInstitutionMutationResult extends InstitutionMutationState {
  readonly lastCreated: Institution | null;
  readonly lastUpdated: Institution | null;
  readonly lastDeletedId: string | null;
  create(input: InstitutionInput): Promise<Institution | null>;
  update(id: string, input: InstitutionInput): Promise<Institution | null>;
  remove(id: string): Promise<boolean>;
  reset(): void;
}

function describeError(error: ApiError): string {
  if (error.kind === ApiErrorKind.Unauthorized) {
    return 'Tu sesión ya no es válida. Por favor, inicia sesión nuevamente.';
  }
  if (error.kind === ApiErrorKind.Forbidden) {
    return 'No tienes permiso para realizar esta operación.';
  }
  if (error.kind === ApiErrorKind.NotFound) {
    return 'La institución solicitada no existe o ya fue eliminada.';
  }
  if (error.kind === ApiErrorKind.Validation) {
    return error.message || 'Los datos enviados no son válidos.';
  }
  if (error.kind === ApiErrorKind.Conflict) {
    return error.message || 'Ya existe una institución con el mismo nombre.';
  }
  if (error.kind === ApiErrorKind.Network) {
    return 'El servicio no está disponible. Por favor, verifica tu conexión e inténtalo de nuevo.';
  }
  return 'No se pudo completar la operación. Por favor, inténtalo de nuevo.';
}

function toApiError(cause: unknown): ApiError {
  if (isApiError(cause)) {
    return cause;
  }

  return {
    kind: ApiErrorKind.Unknown,
    status: 0,
    message: cause instanceof Error ? cause.message : 'Unknown error',
    code: null,
  };
}

export function useInstitutionMutation(options: UseInstitutionMutationOptions): UseInstitutionMutationResult {
  const { service, callbacks } = options;

  const [submitting, setSubmitting] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [lastCreated, setLastCreated] = useState<Institution | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Institution | null>(null);
  const [lastDeletedId, setLastDeletedId] = useState<string | null>(null);

  const reset = useCallback(() => {
    setSubmitting(false);
    setError(null);
    setLastCreated(null);
    setLastUpdated(null);
    setLastDeletedId(null);
  }, []);

  const create = useCallback(
    async (input: InstitutionInput): Promise<Institution | null> => {
      setSubmitting(true);
      setError(null);
      try {
        const created = await service.create(input);
        setLastCreated(created);
        callbacks?.onCreated?.(created);
        return created;
      } catch (cause) {
        setError(describeError(toApiError(cause)));
        return null;
      } finally {
        setSubmitting(false);
      }
    },
    [service, callbacks],
  );

  const update = useCallback(
    async (id: string, input: InstitutionInput): Promise<Institution | null> => {
      setSubmitting(true);
      setError(null);
      try {
        const updated = await service.update(id, input);
        setLastUpdated(updated);
        callbacks?.onUpdated?.(updated);
        return updated;
      } catch (cause) {
        setError(describeError(toApiError(cause)));
        return null;
      } finally {
        setSubmitting(false);
      }
    },
    [service, callbacks],
  );

  const remove = useCallback(
    async (id: string): Promise<boolean> => {
      setSubmitting(true);
      setError(null);
      try {
        await service.remove(id);
        setLastDeletedId(id);
        callbacks?.onDeleted?.(id);
        return true;
      } catch (cause) {
        setError(describeError(toApiError(cause)));
        return false;
      } finally {
        setSubmitting(false);
      }
    },
    [service, callbacks],
  );

  return {
    submitting,
    error,
    lastCreated,
    lastUpdated,
    lastDeletedId,
    create,
    update,
    remove,
    reset,
  };
}
