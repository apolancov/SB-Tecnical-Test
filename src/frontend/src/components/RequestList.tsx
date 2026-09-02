'use client';

import Link from 'next/link';
import type { RequestRecord } from '../types/request';
import { RequestPriorityBadge } from './RequestBadges';
import { formatDate } from './dateFormat';

export interface RequestListProps {
  readonly requests: ReadonlyArray<RequestRecord>;
  readonly showRequester?: boolean;
  readonly emptyMessage?: string;
}

export function RequestList({
  requests,
  showRequester = true,
  emptyMessage = 'No hay solicitudes que coincidan con los filtros.',
}: RequestListProps) {
  if (requests.length === 0) {
    return (
      <div role="status" className="state state--empty">
        {emptyMessage}
      </div>
    );
  }

  return (
    <table
      className="request-table"
      aria-label="Listado de solicitudes"
      data-testid="request-table"
    >
      <thead>
        <tr>
          <th scope="col">Código</th>
          <th scope="col">Título</th>
          <th scope="col">Estado</th>
          <th scope="col">Prioridad</th>
          <th scope="col">Tipo</th>
          <th scope="col">Área</th>
          {showRequester && <th scope="col">Solicitante</th>}
          <th scope="col">Responsable</th>
          <th scope="col">Creación</th>
          <th scope="col">Compromiso</th>
        </tr>
      </thead>
      <tbody>
        {requests.map((request) => (
          <tr key={request.id} data-testid={`request-row-${request.id}`}>
            <td className="request-table__cell-narrow">
              <Link
              href={`/requests/detail?id=${request.id}`}
              className="request-table__code"
              data-testid={`request-code-${request.id}`}
            >
              {request.code}
            </Link>
          </td>
          <td>
            <Link
              href={`/requests/detail?id=${request.id}`}
                className="request-table__code"
                data-testid={`request-title-${request.id}`}
              >
                {request.title}
              </Link>
            </td>
            <td>{request.status}</td>
            <td>
              <RequestPriorityBadge priority={request.priority} />
            </td>
            <td>{request.requestType}</td>
            <td>{request.area}</td>
            {showRequester && <td>{request.requesterUsername}</td>}
            <td>{request.responsibleUsername ?? '—'}</td>
            <td>{formatDate(request.createdAt)}</td>
            <td>{request.dueDate === null ? '—' : formatDate(request.dueDate)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}