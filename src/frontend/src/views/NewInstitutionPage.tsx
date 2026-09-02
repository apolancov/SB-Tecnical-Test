'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { InstitutionForm, type InstitutionFormErrors } from '../components/InstitutionForm';
import { useInstitutionFilterOptions } from '../hooks/useInstitutionFilterOptions';
import { useInstitutionMutation } from '../hooks/useInstitutionMutation';
import { useService } from '../hooks/ServiceContext';
import type { InstitutionInput } from '../types/institution';

const EmptyInstitutionInput: InstitutionInput = {
  name: '',
  category: '',
  statePower: '',
  sector: '',
};

function parseFieldErrors(message: string): InstitutionFormErrors | undefined {
  const lowered = message.toLowerCase();
  if (lowered.includes('nombre')) {
    return { name: message };
  }
  if (lowered.includes('categor')) {
    return { category: message };
  }
  if (lowered.includes('poder')) {
    return { statePower: message };
  }
  if (lowered.includes('sector')) {
    return { sector: message };
  }
  return undefined;
}

export function NewInstitutionPage() {
  const router = useRouter();
  const { institutionService } = useService();
  const { create, submitting, error, lastCreated } = useInstitutionMutation({
    service: institutionService,
    callbacks: {
      onCreated: () => {
        router.replace('/institutions');
      },
    },
  });

  const {
    options,
    loading: optionsLoading,
    error: optionsError,
  } = useInstitutionFilterOptions({ service: institutionService });

  if (lastCreated !== null) {
    return (
      <section className="page">
        <div role="status" aria-live="polite" className="state state--loading">
          Guardando institución...
        </div>
      </section>
    );
  }

  const fieldErrors = error ? parseFieldErrors(error) : undefined;

  return (
    <section className="page">
      <header className="page__header">
        <div className="page__header-row">
          <div>
            <h1 className="page__title">Nueva institución</h1>
            <p className="page__subtitle">
              Registra una nueva institución gubernamental en el catálogo.
            </p>
          </div>
          <Link
            href="/institutions"
            className="button"
            data-testid="new-institution-back"
          >
            Volver al listado
          </Link>
        </div>
      </header>

      <div className="card institution-form-card">
        <InstitutionForm
          initialValue={EmptyInstitutionInput}
          errors={fieldErrors}
          disabled={submitting}
          submitLabel={submitting ? 'Guardando...' : 'Crear institución'}
          onSubmit={(value: InstitutionInput) => {
            void create(value);
          }}
          onCancel={() => router.replace('/institutions')}
          testIdPrefix="new-institution-form"
          filterOptions={options}
          filterOptionsLoading={optionsLoading}
        />
        {optionsError !== null && (
          <p role="status" className="form__warning" data-testid="new-institution-options-error">
            {optionsError}
          </p>
        )}
        {error !== null && fieldErrors === undefined && (
          <p role="alert" className="form__error" data-testid="new-institution-error">
            {error}
          </p>
        )}
      </div>
    </section>
  );
}
