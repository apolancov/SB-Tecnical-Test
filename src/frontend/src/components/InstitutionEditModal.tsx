'use client';

import { useEffect } from 'react';
import { InstitutionForm, type InstitutionFormErrors } from './InstitutionForm';
import type {
  Institution,
  InstitutionFilterOptions,
  InstitutionInput,
} from '../types/institution';

export interface InstitutionEditModalProps {
  readonly institution: Institution;
  readonly open: boolean;
  readonly submitting: boolean;
  readonly error: string | null;
  readonly onSubmit: (input: InstitutionInput) => void;
  readonly onCancel: () => void;
  readonly filterOptions?: InstitutionFilterOptions;
  readonly filterOptionsLoading?: boolean;
}

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

export function InstitutionEditModal({
  institution,
  open,
  submitting,
  error,
  onSubmit,
  onCancel,
  filterOptions,
  filterOptionsLoading = false,
}: InstitutionEditModalProps) {
  useEffect(() => {
    if (!open) {
      return;
    }

    const handleKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !submitting) {
        onCancel();
      }
    };
    document.addEventListener('keydown', handleKey);
    return () => {
      document.removeEventListener('keydown', handleKey);
    };
  }, [open, submitting, onCancel]);

  if (!open) {
    return null;
  }

  const fieldErrors = error ? parseFieldErrors(error) : undefined;
  const initialValue: InstitutionInput = {
    name: institution.name,
    category: institution.category,
    statePower: institution.statePower,
    sector: institution.sector,
  };

  return (
    <div
      className="modal-backdrop"
      role="presentation"
      onClick={(event) => {
        if (event.target === event.currentTarget && !submitting) {
          onCancel();
        }
      }}
    >
      <div
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="institution-edit-title"
      >
        <header className="modal__header">
          <h2 id="institution-edit-title" className="modal__title">
            Editar institución
          </h2>
        </header>
        <div className="modal__body">
          <InstitutionForm
            initialValue={initialValue}
            errors={fieldErrors}
            disabled={submitting}
            submitLabel={submitting ? 'Guardando...' : 'Guardar cambios'}
            onSubmit={onSubmit}
            onCancel={onCancel}
            testIdPrefix="institution-edit-form"
            filterOptions={filterOptions}
            filterOptionsLoading={filterOptionsLoading}
          />
          {error !== null && fieldErrors === undefined && (
            <p role="alert" className="form__error" data-testid="institution-edit-error">
              {error}
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
