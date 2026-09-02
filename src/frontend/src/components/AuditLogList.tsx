'use client';

import { describeAuditAction, describeAuditOutcome, type AuditLogEntry } from '../types/audit';
import { formatDateTime } from './dateFormat';

export interface AuditLogListProps {
  readonly entries: ReadonlyArray<AuditLogEntry>;
  readonly onSelect?: (entry: AuditLogEntry) => void;
}

function outcomeBadgeClass(outcome: string): string {
  switch (outcome) {
    case 'Success':
      return 'audit-log-list__outcome audit-log-list__outcome--success';
    case 'Failure':
      return 'audit-log-list__outcome audit-log-list__outcome--failure';
    case 'Denied':
      return 'audit-log-list__outcome audit-log-list__outcome--denied';
    default:
      return 'audit-log-list__outcome';
  }
}

function describeActor(entry: AuditLogEntry): string {
  if (entry.actorUserName !== null && entry.actorUserName.length > 0) {
    return entry.actorUserName;
  }
  if (entry.actorUserId !== null && entry.actorUserId.length > 0) {
    return entry.actorUserId;
  }
  return '—';
}

export function AuditLogList({ entries, onSelect }: AuditLogListProps) {
  const handleSelect = onSelect;

  return (
    <table className="audit-log-list" aria-label="Bitácora de auditoría">
      <thead>
        <tr>
          <th scope="col">Fecha</th>
          <th scope="col">Acción</th>
          <th scope="col">Resultado</th>
          <th scope="col">Entidad</th>
          <th scope="col">Usuario</th>
          <th scope="col">IP</th>
          {handleSelect !== undefined && (
            <th scope="col" className="audit-log-list__actions-column">
              Detalle
            </th>
          )}
        </tr>
      </thead>
      <tbody>
        {entries.map((entry) => (
          <tr
            key={entry.id}
            data-testid={`audit-log-row-${entry.id}`}
            data-audit-action={entry.action}
            data-audit-outcome={entry.outcome}
          >
            <td>{formatDateTime(entry.timestamp)}</td>
            <td>{describeAuditAction(entry.action)}</td>
            <td>
              <span className={outcomeBadgeClass(entry.outcome)}>
                {describeAuditOutcome(entry.outcome)}
              </span>
            </td>
            <td>
              <span className="audit-log-list__entity-type">{entry.entityType}</span>
              {entry.entityId !== null && (
                <span className="audit-log-list__entity-id">{entry.entityId}</span>
              )}
            </td>
            <td>{describeActor(entry)}</td>
            <td>{entry.ipAddress ?? '—'}</td>
            {handleSelect !== undefined && (
              <td className="audit-log-list__actions">
                <button
                  type="button"
                  className="button button--small"
                  onClick={() => handleSelect(entry)}
                  aria-label={`Ver detalle ${entry.id}`}
                  data-testid={`audit-log-detail-${entry.id}`}
                >
                  Ver
                </button>
              </td>
            )}
          </tr>
        ))}
      </tbody>
    </table>
  );
}