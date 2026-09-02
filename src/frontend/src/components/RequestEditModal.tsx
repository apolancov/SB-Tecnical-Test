'use client';

import { useEffect } from 'react';
import type { AreaRecord, RequestTypeRecord } from '../types/catalog';
import type { RequestDetail, UpdateRequestInput } from '../types/request';
import { RequestForm, type RequestFormErrors } from './RequestForm';

export interface RequestEditModalProps {
  readonly open: boolean;
  readonly request: RequestDetail;
  readonly submitting: boolean;
  readonly error: string | null;
  readonly areas?: ReadonlyArray<AreaRecord>;
  readonly requestTypes?: ReadonlyArray<RequestTypeRecord>;
  readonly onSubmit: (input: UpdateRequestInput) => void;
  readonly onCancel: () => void;
}

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

function dateInputValue(value: string | null): string {
  if (value === null || value.length === 0) {
    return '';
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '';
  }
  const year = date.getUTCFullYear();
  const month = String(date.getUTCMonth() + 1).padStart(2, '0');
  const day = String(date.getUTCDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function RequestEditModal({
  open,
  request,
  submitting,
  error,
  areas,
  requestTypes,
  onSubmit,
  onCancel,
}: RequestEditModalProps) {
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

  const fieldErrors = error !== null ? parseFieldErrors(error) : undefined;

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
        aria-labelledby="request-edit-modal-title"
        data-testid="request-edit-modal"
      >
        <header className="modal__header">
          <h2 id="request-edit-modal-title" className="modal__title">
            Editar solicitud
          </h2>
        </header>
        <div className="modal__body">
          <RequestForm
            mode="edit"
            initialTitle={request.title}
            initialDescription={request.description}
            initialPriority={request.priority}
            initialDueDate={dateInputValue(request.dueDate)}
            initialEvidenceUrl={request.evidenceUrl ?? ''}
            initialAreaId={request.areaId}
            initialRequestTypeId={request.requestTypeId}
            areas={areas ?? []}
            requestTypes={requestTypes ?? []}
            errors={fieldErrors}
            disabled={submitting}
            submitLabel={submitting ? 'Guardando...' : 'Guardar cambios'}
            testIdPrefix="request-edit-form"
            onSubmit={onSubmit}
            onCancel={onCancel}
          />
          {error !== null && fieldErrors === undefined && (
            <p role="alert" className="form__error" data-testid="request-edit-modal-error">
              {error}
            </p>
          )}
        </div>
      </div>
    </div>
  );
}