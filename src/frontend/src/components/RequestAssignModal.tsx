'use client';

import { useEffect, useMemo, useState } from 'react';

export interface RequestAssignModalProps {
  readonly open: boolean;
  readonly currentResponsible: string | null;
  readonly candidates?: ReadonlyArray<{
    readonly id: string;
    readonly username: string;
    readonly email: string;
  }>;
  readonly candidatesLoading?: boolean;
  readonly candidatesError?: string | null;
  readonly submitting: boolean;
  readonly error: string | null;
  readonly onSubmit: (input: {
    readonly responsibleUserId: string;
    readonly comment: string;
  }) => void;
  readonly onCancel: () => void;
}

export function RequestAssignModal({
  open,
  currentResponsible,
  candidates,
  candidatesLoading = false,
  candidatesError = null,
  submitting,
  error,
  onSubmit,
  onCancel,
}: RequestAssignModalProps) {
  const candidateList = useMemo(() => candidates ?? [], [candidates]);
  const [responsibleUserId, setResponsibleUserId] = useState<string>(
    candidateList[0]?.id ?? '',
  );
  const [comment, setComment] = useState<string>('');

  useEffect(() => {
    if (open) {
      setResponsibleUserId(candidateList[0]?.id ?? '');
      setComment('');
    }
  }, [open, candidateList]);

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

  const handleSubmit = () => {
    if (responsibleUserId.length === 0) {
      return;
    }
    onSubmit({ responsibleUserId, comment: comment.trim() });
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
        aria-labelledby="request-assign-modal-title"
        data-testid="request-assign-modal"
      >
        <header className="modal__header">
          <h2 id="request-assign-modal-title" className="modal__title">
            {currentResponsible === null ? 'Asignar responsable' : 'Reasignar responsable'}
          </h2>
        </header>
        <div className="modal__body">
          {currentResponsible !== null && (
            <p className="modal__message">
              Responsable actual: <strong>{currentResponsible}</strong>
            </p>
          )}
          {candidateList.length > 0 ? (
            <label className="field">
              <span className="field__label">Responsable</span>
              <select
                value={responsibleUserId}
                onChange={(event) => setResponsibleUserId(event.target.value)}
                disabled={submitting || candidatesLoading}
                data-testid="request-assign-modal-select"
              >
                {candidateList.map((candidate) => (
                  <option key={candidate.id} value={candidate.id}>
                    {candidate.username}
                    {candidate.email.length > 0 ? ` (${candidate.email})` : ''}
                  </option>
                ))}
              </select>
              {candidatesLoading && (
                <span className="field__hint">Cargando candidatos...</span>
              )}
            </label>
          ) : (
            <label className="field">
              <span className="field__label">Identificador del responsable</span>
              <input
                type="text"
                value={responsibleUserId}
                onChange={(event) => setResponsibleUserId(event.target.value)}
                disabled={submitting || candidatesLoading}
                placeholder="Identificador del usuario"
                data-testid="request-assign-modal-input"
              />
              <span className="field__hint">
                Ingresa el identificador del usuario responsable.
              </span>
            </label>
          )}
          <label className="field">
            <span className="field__label">Comentario (opcional)</span>
            <textarea
              value={comment}
              onChange={(event) => setComment(event.target.value)}
              disabled={submitting}
              data-testid="request-assign-modal-comment"
            />
          </label>
          {candidatesError !== null && (
            <p role="alert" className="form__error" data-testid="request-assign-modal-candidates-error">
              {candidatesError}
            </p>
          )}
          {error !== null && (
            <p role="alert" className="form__error" data-testid="request-assign-modal-error">
              {error}
            </p>
          )}
        </div>
        <footer className="modal__actions">
          <button
            type="button"
            className="button"
            onClick={onCancel}
            disabled={submitting}
            data-testid="request-assign-modal-cancel"
          >
            Cancelar
          </button>
          <button
            type="button"
            className="button button--primary"
            onClick={handleSubmit}
            disabled={
              submitting ||
              responsibleUserId.trim().length === 0 ||
              (candidateList.length > 0 && candidatesLoading)
            }
            data-testid="request-assign-modal-submit"
          >
            {submitting ? 'Guardando...' : 'Confirmar'}
          </button>
        </footer>
      </div>
    </div>
  );
}