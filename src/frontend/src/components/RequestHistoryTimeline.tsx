'use client';

import type { RequestStatusHistoryEntry as RequestStatusHistoryEntryModel } from '../types/request';
import { describeRequestStatus } from '../types/request';
import { formatDateTime } from './dateFormat';

export interface RequestHistoryTimelineProps {
  readonly history: ReadonlyArray<RequestStatusHistoryEntryModel>;
}

export function RequestHistoryTimeline({ history }: RequestHistoryTimelineProps) {
  if (history.length === 0) {
    return (
      <p className="field__hint" data-testid="request-history-empty">
        Aún no hay movimientos registrados.
      </p>
    );
  }

  return (
    <ol className="history-list" data-testid="request-history-list">
      {history.map((entry) => (
        <li key={entry.id} className="history-item" data-testid={`request-history-item-${entry.id}`}>
          <div className="history-item__head">
            <span className="history-item__date">{formatDateTime(entry.date)}</span>
            <span className={`badge badge--status-${entry.previousStatus}`}>
              {describeRequestStatus(entry.previousStatus)}
            </span>
            <span aria-hidden="true">→</span>
            <span className={`badge badge--status-${entry.newStatus}`}>
              {describeRequestStatus(entry.newStatus)}
            </span>
          </div>
          <span className="history-item__date">
            Usuario: <strong>{entry.changedByUsername}</strong>
          </span>
          {entry.comment.length > 0 && <p className="history-item__comment">{entry.comment}</p>}
        </li>
      ))}
    </ol>
  );
}