'use client';

import { useCallback, useEffect, useState } from 'react';
import { AuditLogDetailModal } from '../components/AuditLogDetailModal';
import { AuditLogFiltersBar } from '../components/AuditLogFiltersBar';
import { AuditLogListContainer } from '../components/AuditLogListContainer';
import { Pagination } from '../components/Pagination';
import { useAuditLog } from '../hooks/useAuditLog';
import { useService } from '../hooks/ServiceContext';
import {
  type AuditLogEntry,
  type AuditLogFilters,
  type AuditLogSortField,
  type AuditLogSortDirection,
} from '../types/audit';
import { ApiErrorKind, isApiError } from '../services/api';

export function AuditLogPage() {
  const { auditLogService } = useService();

  const {
    entries,
    loading,
    error,
    page,
    pageSize,
    totalItems,
    totalPages,
    filters,
    sortField,
    sortDirection,
    setFilters,
    setPage,
    setSort,
    resetFilters,
    refresh,
  } = useAuditLog({ service: auditLogService });

  const [selectedEntryId, setSelectedEntryId] = useState<string | null>(null);
  const [detailEntry, setDetailEntry] = useState<AuditLogEntry | null>(null);
  const [detailLoading, setDetailLoading] = useState<boolean>(false);
  const [detailError, setDetailError] = useState<string | null>(null);

  useEffect(() => {
    if (selectedEntryId === null) {
      setDetailEntry(null);
      setDetailError(null);
      setDetailLoading(false);
      return;
    }

    const controller = new AbortController();
    setDetailLoading(true);
    setDetailError(null);

    auditLogService
      .getById(selectedEntryId, controller.signal)
      .then((entry) => {
        if (controller.signal.aborted) {
          return;
        }
        setDetailEntry(entry);
        setDetailLoading(false);
      })
      .catch((cause: unknown) => {
        if (controller.signal.aborted) {
          return;
        }
        if (isApiError(cause) && cause.kind === ApiErrorKind.NotFound) {
          setDetailError('La entrada solicitada ya no está disponible.');
        } else if (isApiError(cause) && cause.kind === ApiErrorKind.Unauthorized) {
          setDetailError('Tu sesión ya no es válida. Por favor, inicia sesión nuevamente.');
        } else if (isApiError(cause) && cause.kind === ApiErrorKind.Forbidden) {
          setDetailError('No tienes permiso para consultar el detalle.');
        } else if (isApiError(cause) && cause.kind === ApiErrorKind.Network) {
          setDetailError('El servicio no está disponible. Inténtalo de nuevo más tarde.');
        } else {
          setDetailError('No se pudo cargar el detalle de la entrada.');
        }
        setDetailLoading(false);
      });

    return () => {
      controller.abort();
    };
  }, [auditLogService, selectedEntryId]);

  const handleSelect = useCallback((entry: AuditLogEntry) => {
    setSelectedEntryId(entry.id);
  }, []);

  const handleCloseModal = useCallback(() => {
    setSelectedEntryId(null);
  }, []);

  const handleSubmitFilters = useCallback(
    (next: {
      filters: AuditLogFilters;
      sortField: AuditLogSortField;
      sortDirection: AuditLogSortDirection;
    }) => {
      setSort(next.sortField, next.sortDirection);
      setFilters(next.filters);
    },
    [setFilters, setSort],
  );

  return (
    <section className="page" data-testid="audit-log-page">
      <header className="page__header">
        <div className="page__header-row">
          <div>
            <h1 className="page__title">Bitácora de auditoría</h1>
            <p className="page__subtitle">
              {loading
                ? 'Cargando resultados...'
                : `${totalItems} entrada${totalItems === 1 ? '' : 's'} encontrada${totalItems === 1 ? '' : 's'}.`}
            </p>
          </div>
          <button
            type="button"
            className="button"
            onClick={refresh}
            disabled={loading}
            data-testid="audit-log-refresh"
          >
            Actualizar
          </button>
        </div>
      </header>

      <AuditLogFiltersBar
        filters={filters}
        sortField={sortField}
        sortDirection={sortDirection}
        disabled={loading}
        onSubmit={handleSubmitFilters}
      />

      {/* <div className="filters__actions">
        <button
          type="button"
          className="button button--small"
          onClick={resetFilters}
          disabled={loading}
          data-testid="audit-log-reset-filters"
        >
          Restablecer filtros
        </button>
      </div> */}

      <AuditLogListContainer
        entries={entries}
        loading={loading}
        error={error}
        onRetry={refresh}
        onSelect={handleSelect}
      />

      <Pagination page={page} totalPages={totalPages} onChange={setPage} />

      <p className="page__meta">
        Mostrando página {page} ({pageSize} por página).
      </p>

      {selectedEntryId !== null && (
        <AuditLogDetailModal
          entry={
            detailEntry ?? {
              id: selectedEntryId,
              timestamp: '',
              action: 'LoginSucceeded',
              outcome: 'Success',
              entityType: '',
              entityId: null,
              details: null,
              ipAddress: null,
              actorUserId: null,
              actorUserName: null,
            }
          }
          open
          loading={detailLoading}
          error={detailError}
          onClose={handleCloseModal}
        />
      )}
    </section>
  );
}