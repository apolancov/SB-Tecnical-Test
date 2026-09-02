'use client';

import type { AuditLogEntry } from '../types/audit';
import { AuditLogList } from './AuditLogList';

export interface AuditLogListContainerProps {
  readonly entries: ReadonlyArray<AuditLogEntry>;
  readonly loading: boolean;
  readonly error: string | null;
  readonly onRetry?: () => void;
  readonly onSelect?: (entry: AuditLogEntry) => void;
}

export function AuditLogListContainer(props: AuditLogListContainerProps) {
  const { entries, loading, error, onRetry, onSelect } = props;

  if (loading) {
    return (
      <div role="status" aria-live="polite" className="state state--loading">
        Cargando bitácora de auditoría...
      </div>
    );
  }

  if (error !== null) {
    return (
      <div role="alert" className="state state--error">
        <p>{error}</p>
        {onRetry !== undefined && (
          <button type="button" onClick={onRetry} className="button">
            Reintentar
          </button>
        )}
      </div>
    );
  }

  if (entries.length === 0) {
    return (
      <div role="status" className="state state--empty">
        No hay entradas que coincidan con los filtros.
      </div>
    );
  }

  return <AuditLogList entries={entries} onSelect={onSelect} />;
}