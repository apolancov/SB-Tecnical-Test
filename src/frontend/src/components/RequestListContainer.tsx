'use client';

import type { RequestRecord } from '../types/request';
import { RequestList } from './RequestList';

export interface RequestListContainerProps {
  readonly requests: ReadonlyArray<RequestRecord>;
  readonly loading: boolean;
  readonly error: string | null;
  readonly onRetry?: () => void;
  readonly emptyMessage?: string;
  readonly showRequester?: boolean;
}

export function RequestListContainer({
  requests,
  loading,
  error,
  onRetry,
  emptyMessage,
  showRequester,
}: RequestListContainerProps) {
  if (loading) {
    return (
      <div role="status" aria-live="polite" className="state state--loading">
        Cargando solicitudes...
      </div>
    );
  }

  if (error !== null) {
    return (
      <div role="alert" className="state state--error">
        <p>{error}</p>
        {onRetry !== undefined && (
          <button type="button" className="button" onClick={onRetry}>
            Reintentar
          </button>
        )}
      </div>
    );
  }

  return (
    <RequestList
      requests={requests}
      emptyMessage={emptyMessage}
      showRequester={showRequester ?? true}
    />
  );
}