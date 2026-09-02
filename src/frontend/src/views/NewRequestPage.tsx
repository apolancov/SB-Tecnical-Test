'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { RequestForm, type RequestFormErrors } from '../components/RequestForm';
import { useCatalogAreas, useCatalogRequestTypes } from '../hooks/useCatalogLookups';
import { useRequestMutation } from '../hooks/useRequestMutation';
import { useService } from '../hooks/ServiceContext';
import type { CreateRequestInput } from '../types/request';

function parseFieldErrors(message: string): RequestFormErrors | undefined {
  const lowered = message.toLowerCase();
  const result: Record<string, string> = {};
  if (lowered.includes('título') || lowered.includes('titulo')) {
    result.title = message;
  }
  if (lowered.includes('descrip')) {
    result.description = message;
  }
  if (lowered.includes('prioridad')) {
    result.priority = message;
  }
  if (lowered.includes('área') || lowered.includes('area')) {
    result.areaId = message;
  }
  if (lowered.includes('tipo de solicitud')) {
    result.requestTypeId = message;
  }
  if (lowered.includes('fecha')) {
    result.dueDate = message;
  }
  if (lowered.includes('evidencia') || lowered.includes('url')) {
    result.evidenceUrl = message;
  }
  return Object.keys(result).length === 0 ? undefined : (result as RequestFormErrors);
}

export function NewRequestPage() {
  const router = useRouter();
  const { requestService, catalogService } = useService();

  const mutation = useRequestMutation({
    service: requestService,
    callbacks: {
      onCreated: (created) => {
        router.replace(`/requests/detail?id=${encodeURIComponent(created.id)}`);
      },
    },
  });

  const { areas, loading: areasLoading, error: areasError } = useCatalogAreas({
    service: catalogService,
  });
  const {
    requestTypes,
    loading: requestTypesLoading,
    error: requestTypesError,
  } = useCatalogRequestTypes({ service: catalogService });

  const fieldErrors = mutation.error !== null ? parseFieldErrors(mutation.error) : undefined;

  return (
    <section className="page" data-testid="new-request-page">
      <header className="page__header">
        <div className="page__header-row">
          <div>
            <h1 className="page__title">Nueva solicitud</h1>
            <p className="page__subtitle">
              Registra una nueva solicitud en el sistema.
            </p>
          </div>
          <Link href="/requests" className="button" data-testid="new-request-back">
            Volver al listado
          </Link>
        </div>
      </header>

      <div className="card institution-form-card">
        <RequestForm
          mode="create"
          initialTitle=""
          initialDescription=""
          initialPriority={'Medium' as CreateRequestInput['priority']}
          initialDueDate=""
          initialEvidenceUrl=""
          areas={areas}
          requestTypes={requestTypes}
          areasLoading={areasLoading}
          requestTypesLoading={requestTypesLoading}
          errors={fieldErrors}
          disabled={mutation.submitting}
          submitLabel={mutation.submitting ? 'Guardando...' : 'Crear solicitud'}
          testIdPrefix="new-request-form"
          onSubmit={(value) => {
            if ('areaId' in value && 'requestTypeId' in value) {
              void mutation.create(value);
            }
          }}
          onCancel={() => router.replace('/requests')}
        />
        {(areasError !== null || requestTypesError !== null) && (
          <p role="status" className="form__error" data-testid="new-request-lookups-error">
            {areasError ?? requestTypesError}
          </p>
        )}
        {mutation.error !== null && fieldErrors === undefined && (
          <p role="alert" className="form__error" data-testid="new-request-error">
            {mutation.error}
          </p>
        )}
        {mutation.lastCreated !== null && (
          <p
            role="status"
            className="field__hint"
            data-testid="new-request-success"
          >
            Solicitud creada correctamente. Código: {mutation.lastCreated.code}
          </p>
        )}
      </div>
    </section>
  );
}