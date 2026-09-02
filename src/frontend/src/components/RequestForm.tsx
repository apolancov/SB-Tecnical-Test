'use client';

import type { FormEvent } from 'react';
import { useState } from 'react';
import type { AreaRecord, RequestTypeRecord } from '../types/catalog';
import {
  RequestCommentMaximumLength,
  RequestDescriptionMaximumLength,
  RequestEvidenceUrlMaximumLength,
  RequestPriority,
  RequestTitleMaximumLength,
  describeRequestPriority,
  type CreateRequestInput,
  type RequestPriority as RequestPriorityType,
  type UpdateRequestInput,
} from '../types/request';

export interface RequestFormErrors {
  readonly title?: string;
  readonly description?: string;
  readonly priority?: string;
  readonly areaId?: string;
  readonly requestTypeId?: string;
  readonly dueDate?: string;
  readonly evidenceUrl?: string;
}

export interface RequestFormProps {
  readonly mode: 'create' | 'edit';
  readonly initialTitle: string;
  readonly initialDescription: string;
  readonly initialPriority: RequestPriorityType;
  readonly initialDueDate: string;
  readonly initialEvidenceUrl: string;
  readonly initialAreaId?: string;
  readonly initialRequestTypeId?: string;
  readonly areas?: ReadonlyArray<AreaRecord>;
  readonly requestTypes?: ReadonlyArray<RequestTypeRecord>;
  readonly areasLoading?: boolean;
  readonly requestTypesLoading?: boolean;
  readonly errors?: RequestFormErrors;
  readonly disabled?: boolean;
  readonly submitLabel: string;
  readonly testIdPrefix?: string;
  readonly onSubmit: (input: CreateRequestInput | UpdateRequestInput) => void;
  readonly onCancel?: () => void;
}

function isBlank(value: string): boolean {
  return value.trim().length === 0;
}

function isAbsoluteHttpUrl(value: string): boolean {
  try {
    const url = new URL(value);
    return url.protocol === 'http:' || url.protocol === 'https:';
  } catch {
    return false;
  }
}

function dateInputValue(value: string): string {
  if (value.length === 0) {
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

function parseDateInput(value: string): string | null {
  if (value.length === 0) {
    return null;
  }
  const date = new Date(`${value}T00:00:00.000Z`);
  if (Number.isNaN(date.getTime())) {
    return null;
  }
  return date.toISOString();
}

export function RequestForm({
  mode,
  initialTitle,
  initialDescription,
  initialPriority,
  initialDueDate,
  initialEvidenceUrl,
  initialAreaId,
  initialRequestTypeId,
  areas,
  requestTypes,
  areasLoading = false,
  requestTypesLoading = false,
  errors,
  disabled = false,
  submitLabel,
  testIdPrefix = 'request-form',
  onSubmit,
  onCancel,
}: RequestFormProps) {
  const isCreate = mode === 'create';

  const [title, setTitle] = useState<string>(initialTitle);
  const [description, setDescription] = useState<string>(initialDescription);
  const [priority, setPriority] = useState<RequestPriorityType>(initialPriority);
  const [areaId, setAreaId] = useState<string>(
    initialAreaId ?? (areas !== undefined && areas.length > 0 ? areas[0].id : ''),
  );
  const [requestTypeId, setRequestTypeId] = useState<string>(
    initialRequestTypeId ?? (requestTypes !== undefined && requestTypes.length > 0 ? requestTypes[0].id : ''),
  );
  const [dueDate, setDueDate] = useState<string>(dateInputValue(initialDueDate));
  const [evidenceUrl, setEvidenceUrl] = useState<string>(initialEvidenceUrl);
  const [showLocalErrors, setShowLocalErrors] = useState<boolean>(false);

  const trimmedTitle = title.trim();
  const trimmedDescription = description.trim();
  const trimmedEvidenceUrl = evidenceUrl.trim();

  const localErrors: RequestFormErrors = {
    title: isBlank(title) ? 'El título es obligatorio.' : undefined,
    description: isBlank(description)
      ? 'La descripción es obligatoria.'
      : undefined,
    priority: undefined,
    areaId: isCreate
      ? isBlank(areaId)
        ? 'El área es obligatoria.'
        : undefined
      : undefined,
    requestTypeId: isCreate
      ? isBlank(requestTypeId)
        ? 'El tipo de solicitud es obligatorio.'
        : undefined
      : undefined,
    dueDate: undefined,
    evidenceUrl:
      trimmedEvidenceUrl.length > 0 && !isAbsoluteHttpUrl(trimmedEvidenceUrl)
        ? 'La URL de evidencia debe ser un enlace http o https válido.'
        : undefined,
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const invalidCreate =
      isCreate &&
      (isBlank(areaId) || isBlank(requestTypeId));
    const invalidCommon =
      isBlank(title) ||
      isBlank(description) ||
      (trimmedEvidenceUrl.length > 0 && !isAbsoluteHttpUrl(trimmedEvidenceUrl));

    if (invalidCreate || invalidCommon) {
      setShowLocalErrors(true);
      return;
    }

    if (isCreate) {
      const createInput: CreateRequestInput = {
        title: trimmedTitle,
        description: trimmedDescription,
        priority,
        areaId,
        requestTypeId,
        dueDate: parseDateInput(dueDate),
        evidenceUrl: trimmedEvidenceUrl.length === 0 ? null : trimmedEvidenceUrl,
      };
      onSubmit(createInput);
    } else {
      const updateInput: UpdateRequestInput = {
        title: trimmedTitle,
        description: trimmedDescription,
        priority,
        dueDate: parseDateInput(dueDate),
        evidenceUrl: trimmedEvidenceUrl.length === 0 ? null : trimmedEvidenceUrl,
      };
      onSubmit(updateInput);
    }
  };

  const showError = (field: keyof RequestFormErrors): string | undefined => {
    if (showLocalErrors && localErrors[field]) {
      return localErrors[field];
    }
    return errors?.[field];
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="institution-form"
      aria-label="Formulario de solicitud"
      noValidate
    >
      <div className="institution-form__row">
        <label className="field">
          <span className="field__label">Título</span>
          <input
            type="text"
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            disabled={disabled}
            autoComplete="off"
            maxLength={RequestTitleMaximumLength}
            aria-invalid={showError('title') ? 'true' : 'false'}
            data-testid={`${testIdPrefix}-title`}
          />
          <span className="field__hint">
            {trimmedTitle.length}/{RequestTitleMaximumLength}
          </span>
          {showError('title') && (
            <span className="field__error" role="alert" data-testid={`${testIdPrefix}-title-error`}>
              {showError('title')}
            </span>
          )}
        </label>
      </div>
      <div className="institution-form__row">
        <label className="field">
          <span className="field__label">Descripción</span>
          <textarea
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            disabled={disabled}
            maxLength={RequestDescriptionMaximumLength}
            aria-invalid={showError('description') ? 'true' : 'false'}
            data-testid={`${testIdPrefix}-description`}
          />
          <span className="field__hint">
            {trimmedDescription.length}/{RequestDescriptionMaximumLength}
          </span>
          {showError('description') && (
            <span
              className="field__error"
              role="alert"
              data-testid={`${testIdPrefix}-description-error`}
            >
              {showError('description')}
            </span>
          )}
        </label>
      </div>
      <div className="institution-form__row">
        <label className="field">
          <span className="field__label">Prioridad</span>
          <select
            value={priority}
            onChange={(event) => setPriority(event.target.value as RequestPriorityType)}
            disabled={disabled}
            aria-invalid={showError('priority') ? 'true' : 'false'}
            data-testid={`${testIdPrefix}-priority`}
          >
            {Object.values(RequestPriority).map((value) => (
              <option key={value} value={value}>
                {describeRequestPriority(value)}
              </option>
            ))}
          </select>
          {showError('priority') && (
            <span className="field__error" role="alert" data-testid={`${testIdPrefix}-priority-error`}>
              {showError('priority')}
            </span>
          )}
        </label>
        {isCreate && areas !== undefined && (
          <label className="field">
            <span className="field__label">Área</span>
            <select
              value={areaId}
              onChange={(event) => setAreaId(event.target.value)}
              disabled={disabled || areasLoading || areas.length === 0}
              aria-invalid={showError('areaId') ? 'true' : 'false'}
              data-testid={`${testIdPrefix}-area`}
            >
              <option value="">Selecciona un área</option>
              {areas.map((area) => (
                <option key={area.id} value={area.id}>
                  {area.name}
                </option>
              ))}
            </select>
            {showError('areaId') && (
              <span className="field__error" role="alert" data-testid={`${testIdPrefix}-area-error`}>
                {showError('areaId')}
              </span>
            )}
          </label>
        )}
        {isCreate && requestTypes !== undefined && (
          <label className="field">
            <span className="field__label">Tipo de solicitud</span>
            <select
              value={requestTypeId}
              onChange={(event) => setRequestTypeId(event.target.value)}
              disabled={disabled || requestTypesLoading || requestTypes.length === 0}
              aria-invalid={showError('requestTypeId') ? 'true' : 'false'}
              data-testid={`${testIdPrefix}-request-type`}
            >
              <option value="">Selecciona un tipo</option>
              {requestTypes.map((type) => (
                <option key={type.id} value={type.id}>
                  {type.name}
                </option>
              ))}
            </select>
            {showError('requestTypeId') && (
              <span
                className="field__error"
                role="alert"
                data-testid={`${testIdPrefix}-request-type-error`}
              >
                {showError('requestTypeId')}
              </span>
            )}
          </label>
        )}
      </div>
      <div className="institution-form__row">
        <label className="field">
          <span className="field__label">Fecha compromiso</span>
          <input
            type="date"
            value={dueDate}
            onChange={(event) => setDueDate(event.target.value)}
            disabled={disabled}
            aria-invalid={showError('dueDate') ? 'true' : 'false'}
            data-testid={`${testIdPrefix}-due-date`}
          />
          {showError('dueDate') && (
            <span className="field__error" role="alert" data-testid={`${testIdPrefix}-due-date-error`}>
              {showError('dueDate')}
            </span>
          )}
        </label>
        <label className="field">
          <span className="field__label">Evidencia (URL)</span>
          <input
            type="url"
            value={evidenceUrl}
            onChange={(event) => setEvidenceUrl(event.target.value)}
            disabled={disabled}
            autoComplete="off"
            maxLength={RequestEvidenceUrlMaximumLength}
            aria-invalid={showError('evidenceUrl') ? 'true' : 'false'}
            placeholder="https://..."
            data-testid={`${testIdPrefix}-evidence-url`}
          />
          <span className="field__hint">
            Opcional. Enlace a un documento o referencia.
          </span>
          {showError('evidenceUrl') && (
            <span
              className="field__error"
              role="alert"
              data-testid={`${testIdPrefix}-evidence-url-error`}
            >
              {showError('evidenceUrl')}
            </span>
          )}
        </label>
      </div>
      <p className="field__hint" data-testid={`${testIdPrefix}-comment-hint`}>
        Comentario máximo {RequestCommentMaximumLength} caracteres.
      </p>
      <div className="institution-form__actions">
        <button
          type="submit"
          className="button button--primary"
          disabled={disabled}
          data-testid={`${testIdPrefix}-submit`}
        >
          {submitLabel}
        </button>
        {onCancel !== undefined && (
          <button
            type="button"
            className="button"
            onClick={onCancel}
            disabled={disabled}
            data-testid={`${testIdPrefix}-cancel`}
          >
            Cancelar
          </button>
        )}
      </div>
    </form>
  );
}