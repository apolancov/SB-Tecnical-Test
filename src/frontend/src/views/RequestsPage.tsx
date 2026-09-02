'use client';

import Link from 'next/link';
import { Pagination } from '../components/Pagination';
import { RequestFiltersBar } from '../components/RequestFiltersBar';
import { RequestListContainer } from '../components/RequestListContainer';
import { useCatalogAreas, useCatalogRequestTypes } from '../hooks/useCatalogLookups';
import { useRequests } from '../hooks/useRequests';
import { useAuth } from '../hooks/useAuth';
import { useService } from '../hooks/ServiceContext';
import { UserRole } from '../types/auth';
import type { RequestFilters, RequestSortField, RequestSortDirection } from '../types/request';

export function RequestsPage() {
  const { requestService, catalogService } = useService();
  const { user } = useAuth();

  const {
    requests,
    loading,
    error,
    page,
    totalItems,
    totalPages,
    filters,
    sortField,
    sortDirection,
    setFilters,
    setPage,
    setSort,
    refresh,
  } = useRequests({ service: requestService });

  const { areas, loading: areasLoading, error: areasError } = useCatalogAreas({
    service: catalogService,
  });
  const {
    requestTypes,
    loading: requestTypesLoading,
    error: requestTypesError,
  } = useCatalogRequestTypes({ service: catalogService });

  const showRequester = user?.role !== UserRole.Solicitante;

  return (
    <section className="page" data-testid="requests-page">
      <header className="page__header">
        <div className="page__header-row">
          <div>
            <h1 className="page__title">Solicitudes</h1>
            <p className="page__subtitle">
              {loading
                ? 'Cargando resultados...'
                : `${totalItems} resultado${totalItems === 1 ? '' : 's'} encontrado${totalItems === 1 ? '' : 's'}.`}
            </p>
          </div>
          <Link
            href="/requests/new"
            className="button button--primary"
            data-testid="new-request-link"
          >
            Nueva solicitud
          </Link>
        </div>
      </header>

      <RequestFiltersBar
        filters={filters}
        sortField={sortField}
        sortDirection={sortDirection}
        areas={areas}
        requestTypes={requestTypes}
        areasLoading={areasLoading}
        requestTypesLoading={requestTypesLoading}
        disabled={loading}
        onSubmit={(next: {
          filters: RequestFilters;
          sortField: RequestSortField;
          sortDirection: RequestSortDirection;
        }) => {
          setFilters(next.filters);
          setSort(next.sortField, next.sortDirection);
        }}
      />

      {(areasError !== null || requestTypesError !== null) && (
        <p role="status" className="form__error" data-testid="requests-lookups-error">
          {areasError ?? requestTypesError}
        </p>
      )}

      <RequestListContainer
        requests={requests}
        loading={loading}
        error={error}
        onRetry={refresh}
        showRequester={showRequester}
        emptyMessage="No hay solicitudes que coincidan con los filtros."
      />

      <Pagination page={page} totalPages={totalPages} onChange={setPage} />

    </section>
  );
}