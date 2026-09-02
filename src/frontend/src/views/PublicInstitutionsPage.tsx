'use client';

import Link from 'next/link';
import { InstitutionFiltersBar } from '../components/InstitutionFiltersBar';
import { InstitutionListContainer } from '../components/InstitutionListContainer';
import { Logo } from '../components/Logo';
import { Pagination } from '../components/Pagination';
import { useInstitutionFilterOptions } from '../hooks/useInstitutionFilterOptions';
import { useInstitutions } from '../hooks/useInstitutions';
import { useService } from '../hooks/ServiceContext';

export function PublicInstitutionsPage() {
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

  return (
    <div className="public-shell">
      <header className="public-shell__header">
        <Link href="/directorio" className="public-shell__brand">
          <Logo className="public-shell__logo" size="medium" alt="SB Client" />
          <span className="public-shell__title">Directorio de Instituciones</span>
        </Link>
        <nav aria-label="Cuenta" className="public-shell__nav">
          <Link
            href="/login"
            className="public-shell__link"
            data-testid="public-shell-login-link"
          >
            Iniciar sesión
          </Link>
        </nav>
      </header>

      <main className="public-shell__main">
        <section className="page">
          <header className="page__header">
            <div>
              <h1 className="page__title">Instituciones gubernamentales</h1>
              <p className="page__subtitle">
                {loading
                  ? 'Cargando resultados...'
                  : `${totalItems} resultado${totalItems === 1 ? '' : 's'} encontrado${totalItems === 1 ? '' : 's'}.`}
              </p>
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
          />

          <Pagination page={page} totalPages={totalPages} onChange={setPage} />

          <p className="page__meta">
            Mostrando página {page} ({pageSize} por página).
          </p>
        </section>
      </main>
    </div>
  );
}
