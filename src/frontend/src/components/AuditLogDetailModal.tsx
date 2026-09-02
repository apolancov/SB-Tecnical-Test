'use client';

import { useEffect } from 'react';
import {
  describeAuditAction,
  describeAuditOutcome,
  type AuditLogEntry,
} from '../types/audit';
import { formatDateTime } from './dateFormat';

export interface AuditLogDetailModalProps {
  readonly entry: AuditLogEntry;
  readonly open: boolean;
  readonly loading: boolean;
  readonly error: string | null;
  readonly onClose: () => void;
}

interface DetailField {
  readonly label: string;
  readonly value: string;
}

function buildFields(entry: AuditLogEntry): ReadonlyArray<DetailField> {
  const fields: DetailField[] = [
    { label: 'Fecha (UTC)', value: formatDateTime(entry.timestamp, { fallback: '—' }) },
    { label: 'Acción', value: describeAuditAction(entry.action) },
    { label: 'Resultado', value: describeAuditOutcome(entry.outcome) },
    { label: 'Tipo de entidad', value: entry.entityType },
  ];

  if (entry.entityId !== null && entry.entityId.length > 0) {
    fields.push({ label: 'Identificador', value: entry.entityId });
  }

  if (entry.actorUserName !== null && entry.actorUserName.length > 0) {
    fields.push({ label: 'Usuario', value: entry.actorUserName });
  }

  if (entry.actorUserId !== null && entry.actorUserId.length > 0) {
    fields.push({ label: 'ID de usuario', value: entry.actorUserId });
  }

  if (entry.ipAddress !== null && entry.ipAddress.length > 0) {
    fields.push({ label: 'Dirección IP', value: entry.ipAddress });
  }

  if (entry.details !== null && entry.details.length > 0) {
    fields.push({ label: 'Detalles', value: entry.details });
  }

  fields.push({ label: 'Identificador de entrada', value: entry.id });

  return fields;
}

export function AuditLogDetailModal({
  entry,
  open,
  loading,
  error,
  onClose,
}: AuditLogDetailModalProps) {
  useEffect(() => {
    if (!open) {
      return;
    }
    const handleKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose();
      }
    };
    document.addEventListener('keydown', handleKey);
    return () => {
      document.removeEventListener('keydown', handleKey);
    };
  }, [open, onClose]);

  if (!open) {
    return null;
  }

  const fields = buildFields(entry);

  return (
    <div
      className="modal-backdrop"
      role="presentation"
      onClick={(event) => {
        if (event.target === event.currentTarget) {
          onClose();
        }
      }}
    >
      <div
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="audit-log-detail-title"
      >
        <header className="modal__header">
          <h2 id="audit-log-detail-title" className="modal__title">
            Detalle de entrada de auditoría
          </h2>
        </header>
        <div className="modal__body">
          {loading && (
            <p role="status" className="state state--loading" data-testid="audit-log-detail-loading">
              Cargando detalle...
            </p>
          )}
          {!loading && error !== null && (
            <p role="alert" className="form__error" data-testid="audit-log-detail-error">
              {error}
            </p>
          )}
          {!loading && error === null && (
            <dl className="audit-log-detail" data-testid="audit-log-detail">
              {fields.map((field) => (
                <div key={field.label} className="audit-log-detail__row">
                  <dt className="audit-log-detail__label">{field.label}</dt>
                  <dd className="audit-log-detail__value">{field.value}</dd>
                </div>
              ))}
            </dl>
          )}
        </div>
        <footer className="modal__footer">
          <button
            type="button"
            className="button"
            onClick={onClose}
            data-testid="audit-log-detail-close"
          >
            Cerrar
          </button>
        </footer>
      </div>
    </div>
  );
}