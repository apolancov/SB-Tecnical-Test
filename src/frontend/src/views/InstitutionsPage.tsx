'use client';

import Link from 'next/link';
import { useCallback, useState } from 'react';
import { InstitutionDeleteModal } from '../components/InstitutionDeleteModal';
import { InstitutionEditModal } from '../components/InstitutionEditModal';
import { InstitutionFiltersBar } from '../components/InstitutionFiltersBar';
import { InstitutionListContainer } from '../components/InstitutionListContainer';
import { Pagination } from '../components/Pagination';
import { useInstitutionFilterOptions } from '../hooks/useInstitutionFilterOptions';
import { useInstitutionMutation } from '../hooks/useInstitutionMutation';
import { useInstitutions } from '../hooks/useInstitutions';
import { useService } from '../hooks/ServiceContext';
import type { Institution, InstitutionInput } from '../types/institution';

export function InstitutionsPage() {
  const { institutionService } = useService();

  const {
    institutions,
    loading,
    error,
    page,
    pageSize,
    totalItems,
    totalPages,
    filters,
    setFilters,
    setPage,
    refresh,
  } = useInstitutions({ service: institutionService });

  const {
    options,
    loading: optionsLoading,
    error: optionsError,
  } = useInstitutionFilterOptions({ service: institutionService });

  const [editing, setEditing] = useState<Institution | null>(null);
  const [deleting, setDeleting] = useState<Institution | null>(null);

  const mutation = useInstitutionMutation({
    service: institutionService,
    callbacks: {
      onUpdated: () => {
        setEditing(null);
        refresh();
      },
      onDeleted: () => {
        setDeleting(null);
        refresh();
      },
    },
  });

  const closeEdit = useCallback(() => {
    setEditing(null);
    mutation.reset();
  }, [mutation]);

  const closeDelete = useCallback(() => {
    setDeleting(null);
    mutation.reset();
  }, [mutation]);

  const handleEdit = useCallback(
    (institution: Institution) => {
      mutation.reset();
      setEditing(institution);
    },
    [mutation],
  );

  const handleDelete = useCallback(
    (institution: Institution) => {
      mutation.reset();
      setDeleting(institution);
    },
    [mutation],
  );

  const handleSubmitEdit = useCallback(
    (input: InstitutionInput) => {
      if (editing === null) {
        return;
      }
      void mutation.update(editing.id, input);
    },
    [editing, mutation],
  );

  const handleConfirmDelete = useCallback(() => {
    if (deleting === null) {
      return;
    }
    void mutation.remove(deleting.id);
  }, [deleting, mutation]);

  return (
    <section className="page">
      <header className="page__header">
        <div className="page__header-row">
          <div>
            <h1 className="page__title">Instituciones</h1>
            <p className="page__subtitle">
              {loading
                ? 'Cargando resultados...'
                : `${totalItems} resultado${totalItems === 1 ? '' : 's'} encontrado${totalItems === 1 ? '' : 's'}.`}
            </p>
          </div>
          <Link
            href="/institutions/new"
            className="button button--primary"
            data-testid="new-institution-link"
          >
            Nueva institución
          </Link>
        </div>
      </header>

      <InstitutionFiltersBar
        filters={filters}
        onSubmit={setFilters}
        options={options}
        optionsLoading={optionsLoading}
        optionsError={optionsError}
        disabled={loading}
      />

      <InstitutionListContainer
        institutions={institutions}
        loading={loading}
        error={error}
        onRetry={refresh}
        onEdit={handleEdit}
        onDelete={handleDelete}
      />

      <Pagination page={page} totalPages={totalPages} onChange={setPage} />

      <p className="page__meta">
        Mostrando página {page} ({pageSize} por página).
      </p>

      {editing !== null && (
        <InstitutionEditModal
          institution={editing}
          open
          submitting={mutation.submitting}
          error={mutation.error}
          onSubmit={handleSubmitEdit}
          onCancel={closeEdit}
          filterOptions={options}
          filterOptionsLoading={optionsLoading}
        />
      )}

      {deleting !== null && (
        <InstitutionDeleteModal
          institution={deleting}
          open
          submitting={mutation.submitting}
          error={mutation.error}
          onConfirm={handleConfirmDelete}
          onCancel={closeDelete}
        />
      )}
    </section>
  );
}
