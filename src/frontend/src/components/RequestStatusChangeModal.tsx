'use client';

import { useEffect, useState } from 'react';
import {
  RequestStatus,
  type RequestStatus as RequestStatusType,
} from '../types/request';
import { requestStatusOptions } from './RequestBadges';

export interface RequestStatusChangeModalProps {
  readonly open: boolean;
  readonly currentStatus: RequestStatusType;
  readonly allowedNextStatuses: ReadonlyArray<RequestStatusType>;
  readonly submitting: boolean;
  readonly error: string | null;
  readonly requireComment?: boolean;
  readonly title?: string;
  readonly onSubmit: (input: {
    readonly newStatus: RequestStatusType;
    readonly comment: string;
  }) => void;
  readonly onCancel: () => void;
}

function isAllowed(
  next: RequestStatusType,
  allowed: ReadonlyArray<RequestStatusType>,
): boolean {
  return allowed.includes(next);
}

function defaultNextStatus(
  current: RequestStatusType,
  allowed: ReadonlyArray<RequestStatusType>,
): RequestStatusType {
  const candidate = allowed[0] ?? RequestStatus.InReview;
  if (candidate === current && allowed.length > 1) {
    return allowed[1];
  }
  return candidate;
}

export function RequestStatusChangeModal({
  open,
  currentStatus,
  allowedNextStatuses,
  submitting,
  error,
  requireComment = false,
  title = 'Cambiar estado',
  onSubmit,
  onCancel,
}: RequestStatusChangeModalProps) {
  const [newStatus, setNewStatus] = useState<RequestStatusType>(
    defaultNextStatus(currentStatus, allowedNextStatuses),
  );
  const [comment, setComment] = useState<string>('');

  useEffect(() => {
    if (open) {
      setNewStatus(defaultNextStatus(currentStatus, allowedNextStatuses));
      setComment('');
    }
  }, [open, currentStatus, allowedNextStatuses]);

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
    if (requireComment && comment.trim().length === 0) {
      return;
    }
    onSubmit({ newStatus, comment: comment.trim() });
  };

  const availableOptions = requestStatusOptions().filter((option) =>
    isAllowed(option.value, allowedNextStatuses),
  );

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
        aria-labelledby="request-status-modal-title"
        data-testid="request-status-modal"
      >
        <header className="modal__header">
          <h2 id="request-status-modal-title" className="modal__title">
            {title}
          </h2>
        </header>
        <div className="modal__body">
          <p className="modal__message">
            Estado actual: <strong>{currentStatus}</strong>
          </p>
          <label className="field">
            <span className="field__label">Cambiar a</span>
            <select
              value={newStatus}
              onChange={(event) => setNewStatus(event.target.value as RequestStatusType)}
              disabled={submitting || availableOptions.length === 0}
              data-testid="request-status-modal-select"
            >
              {availableOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.name}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span className="field__label">
              Comentario {requireComment ? '*' : '(opcional)'}
            </span>
            <textarea
              value={comment}
              onChange={(event) => setComment(event.target.value)}
              disabled={submitting}
              data-testid="request-status-modal-comment"
            />
            {requireComment && (
              <span className="field__hint" data-testid="request-status-modal-required-hint">
                El comentario es obligatorio.
              </span>
            )}
          </label>
          {error !== null && (
            <p role="alert" className="form__error" data-testid="request-status-modal-error">
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
            data-testid="request-status-modal-cancel"
          >
            Cancelar
          </button>
          <button
            type="button"
            className="button button--primary"
            onClick={handleSubmit}
            disabled={
              submitting ||
              availableOptions.length === 0 ||
              (requireComment && comment.trim().length === 0)
            }
            data-testid="request-status-modal-submit"
          >
            {submitting ? 'Guardando...' : 'Confirmar'}
          </button>
        </footer>
      </div>
    </div>
  );
}