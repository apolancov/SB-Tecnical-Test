'use client';

import { useCallback, useState } from 'react';
import { ApiErrorKind, isApiError, type ApiError } from '../services/api';
import type { RequestService } from '../services/requestService';
import type {
  AddCommentInput,
  AssignRequestInput,
  ChangeRequestStatusInput,
  CreateRequestInput,
  ReopenRequestInput,
  RequestComment,
  RequestRecord,
  UpdateRequestInput,
} from '../types/request';

export type RequestMutationKind =
  | 'create'
  | 'update'
  | 'changeStatus'
  | 'assign'
  | 'reopen'
  | 'addComment';

export interface RequestMutationState {
  readonly submitting: boolean;
  readonly error: string | null;
  readonly kind: RequestMutationKind | null;
}

export interface RequestMutationCallbacks {
  readonly onCreated?: (request: RequestRecord) => void;
  readonly onUpdated?: (request: RequestRecord) => void;
  readonly onStatusChanged?: (request: RequestRecord) => void;
  readonly onAssigned?: (request: RequestRecord) => void;
  readonly onReopened?: (request: RequestRecord) => void;
  readonly onCommentAdded?: (comment: RequestComment) => void;
}

export interface UseRequestMutationResult extends RequestMutationState {
  readonly lastCreated: RequestRecord | null;
  readonly lastUpdated: RequestRecord | null;
  readonly lastStatusChanged: RequestRecord | null;
  readonly lastAssigned: RequestRecord | null;
  readonly lastReopened: RequestRecord | null;
  readonly lastComment: RequestComment | null;
  create(input: CreateRequestInput): Promise<RequestRecord | null>;
  update(id: string, input: UpdateRequestInput): Promise<RequestRecord | null>;
  changeStatus(id: string, input: ChangeRequestStatusInput): Promise<RequestRecord | null>;
  assign(id: string, input: AssignRequestInput): Promise<RequestRecord | null>;
  reopen(id: string, input: ReopenRequestInput): Promise<RequestRecord | null>;
  addComment(id: string, input: AddCommentInput): Promise<RequestComment | null>;
  reset(): void;
}

export interface UseRequestMutationOptions {
  readonly service: RequestService;
  readonly callbacks?: RequestMutationCallbacks;
}

function describeError(error: ApiError): string {
  if (error.kind === ApiErrorKind.Unauthorized) {
    return 'Tu sesión ya no es válida. Por favor, inicia sesión nuevamente.';
  }
  if (error.kind === ApiErrorKind.Forbidden) {
    return 'No tienes permiso para realizar esta operación.';
  }
  if (error.kind === ApiErrorKind.NotFound) {
    return 'La solicitud solicitada no existe o ya fue eliminada.';
  }
  if (error.kind === ApiErrorKind.Validation) {
    return error.message || 'Los datos enviados no son válidos.';
  }
  if (error.kind === ApiErrorKind.Conflict) {
    return error.message || 'La operación entra en conflicto con el estado actual.';
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

export function useRequestMutation(
  options: UseRequestMutationOptions,
): UseRequestMutationResult {
  const { service, callbacks } = options;

  const [submitting, setSubmitting] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [kind, setKind] = useState<RequestMutationKind | null>(null);
  const [lastCreated, setLastCreated] = useState<RequestRecord | null>(null);
  const [lastUpdated, setLastUpdated] = useState<RequestRecord | null>(null);
  const [lastStatusChanged, setLastStatusChanged] = useState<RequestRecord | null>(null);
  const [lastAssigned, setLastAssigned] = useState<RequestRecord | null>(null);
  const [lastReopened, setLastReopened] = useState<RequestRecord | null>(null);
  const [lastComment, setLastComment] = useState<RequestComment | null>(null);

  const begin = useCallback((nextKind: RequestMutationKind) => {
    setSubmitting(true);
    setKind(nextKind);
    setError(null);
  }, []);

  const create = useCallback(
    (input: CreateRequestInput): Promise<RequestRecord | null> => {
      begin('create');
      return service
        .create(input)
        .then((result) => {
          setLastCreated(result);
          callbacks?.onCreated?.(result);
          return result;
        })
        .catch((cause: unknown) => {
          const apiError = toApiError(cause);
          setError(describeError(apiError));
          return null;
        })
        .finally(() => {
          setSubmitting(false);
          setKind(null);
        });
    },
    [service, callbacks, begin],
  );

  const update = useCallback(
    (id: string, input: UpdateRequestInput): Promise<RequestRecord | null> => {
      begin('update');
      return service
        .update(id, input)
        .then((result) => {
          setLastUpdated(result);
          callbacks?.onUpdated?.(result);
          return result;
        })
        .catch((cause: unknown) => {
          const apiError = toApiError(cause);
          setError(describeError(apiError));
          return null;
        })
        .finally(() => {
          setSubmitting(false);
          setKind(null);
        });
    },
    [service, callbacks, begin],
  );

  const changeStatus = useCallback(
    (id: string, input: ChangeRequestStatusInput): Promise<RequestRecord | null> => {
      begin('changeStatus');
      return service
        .changeStatus(id, input)
        .then((result) => {
          setLastStatusChanged(result);
          callbacks?.onStatusChanged?.(result);
          return result;
        })
        .catch((cause: unknown) => {
          const apiError = toApiError(cause);
          setError(describeError(apiError));
          return null;
        })
        .finally(() => {
          setSubmitting(false);
          setKind(null);
        });
    },
    [service, callbacks, begin],
  );

  const assign = useCallback(
    (id: string, input: AssignRequestInput): Promise<RequestRecord | null> => {
      begin('assign');
      return service
        .assign(id, input)
        .then((result) => {
          setLastAssigned(result);
          callbacks?.onAssigned?.(result);
          return result;
        })
        .catch((cause: unknown) => {
          const apiError = toApiError(cause);
          setError(describeError(apiError));
          return null;
        })
        .finally(() => {
          setSubmitting(false);
          setKind(null);
        });
    },
    [service, callbacks, begin],
  );

  const reopen = useCallback(
    (id: string, input: ReopenRequestInput): Promise<RequestRecord | null> => {
      begin('reopen');
      return service
        .reopen(id, input)
        .then((result) => {
          setLastReopened(result);
          callbacks?.onReopened?.(result);
          return result;
        })
        .catch((cause: unknown) => {
          const apiError = toApiError(cause);
          setError(describeError(apiError));
          return null;
        })
        .finally(() => {
          setSubmitting(false);
          setKind(null);
        });
    },
    [service, callbacks, begin],
  );

  const addComment = useCallback(
    (id: string, input: AddCommentInput): Promise<RequestComment | null> => {
      begin('addComment');
      return service
        .addComment(id, input)
        .then((result) => {
          setLastComment(result);
          callbacks?.onCommentAdded?.(result);
          return result;
        })
        .catch((cause: unknown) => {
          const apiError = toApiError(cause);
          setError(describeError(apiError));
          return null;
        })
        .finally(() => {
          setSubmitting(false);
          setKind(null);
        });
    },
    [service, callbacks, begin],
  );

  const reset = useCallback(() => {
    setSubmitting(false);
    setError(null);
    setKind(null);
    setLastCreated(null);
    setLastUpdated(null);
    setLastStatusChanged(null);
    setLastAssigned(null);
    setLastReopened(null);
    setLastComment(null);
  }, []);

  return {
    submitting,
    error,
    kind,
    lastCreated,
    lastUpdated,
    lastStatusChanged,
    lastAssigned,
    lastReopened,
    lastComment,
    create,
    update,
    changeStatus,
    assign,
    reopen,
    addComment,
    reset,
  };
}