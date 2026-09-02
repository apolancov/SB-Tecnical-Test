'use client';

import { useEffect } from 'react';
import type { Institution } from '../types/institution';

export interface InstitutionDeleteModalProps {
  readonly institution: Institution;
  readonly open: boolean;
  readonly submitting: boolean;
  readonly error: string | null;
  readonly onConfirm: () => void;
  readonly onCancel: () => void;
}

export function InstitutionDeleteModal({
  institution,
  open,
  submitting,
  error,
  onConfirm,
  onCancel,
}: InstitutionDeleteModalProps) {
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
        className="modal modal--narrow"
        role="dialog"
        aria-modal="true"
        aria-labelledby="institution-delete-title"
      >
        <header className="modal__header">
          <h2 id="institution-delete-title" className="modal__title">
            Eliminar institución
          </h2>
        </header>
        <div className="modal__body">
          <p className="modal__message" data-testid="institution-delete-message">
            ¿Estás seguro de que deseas eliminar
            <strong> {institution.name}</strong>?
            Esta acción no se puede deshacer.
          </p>
          {error !== null && (
            <p role="alert" className="form__error" data-testid="institution-delete-error">
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
            data-testid="institution-delete-cancel"
          >
            Cancelar
          </button>
          <button
            type="button"
            className="button button--danger"
            onClick={onConfirm}
            disabled={submitting}
            data-testid="institution-delete-confirm"
          >
            {submitting ? 'Eliminando...' : 'Eliminar'}
          </button>
        </footer>
      </div>
    </div>
  );
}
