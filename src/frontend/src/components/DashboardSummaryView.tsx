'use client';

import Link from 'next/link';
import {
  RequestPriority,
  RequestStatus,
  describeRequestPriority,
  describeRequestStatus,
  type RequestPriority as RequestPriorityType,
  type RequestStatus as RequestStatusType,
} from '../types/request';
import { RequestPriorityBadge, RequestStatusBadge } from './RequestBadges';
import { formatDateTime } from './dateFormat';
import type { DashboardSummary } from '../types/dashboard';

export interface DashboardSummaryViewProps {
  readonly summary: DashboardSummary;
  readonly loading: boolean;
  readonly error: string | null;
  readonly onRetry?: () => void;
}

const StatusOrder: ReadonlyArray<RequestStatusType> = [
  RequestStatus.Submitted,
  RequestStatus.InReview,
  RequestStatus.Assigned,
  RequestStatus.InProgress,
  RequestStatus.OnHold,
  RequestStatus.Resolved,
  RequestStatus.Closed,
  RequestStatus.Cancelled,
];

const PriorityOrder: ReadonlyArray<RequestPriorityType> = [
  RequestPriority.Low,
  RequestPriority.Medium,
  RequestPriority.High,
  RequestPriority.Critical,
];

function getStatusValue(
  summary: DashboardSummary,
  status: RequestStatusType,
): number {
  const raw = summary.requestsByStatus[status];
  return typeof raw === 'number' ? raw : 0;
}

function getPriorityValue(
  summary: DashboardSummary,
  priority: RequestPriorityType,
): number {
  const raw = summary.requestsByPriority[priority];
  return typeof raw === 'number' ? raw : 0;
}

export function DashboardSummaryView({
  summary,
  loading,
  error,
  onRetry,
}: DashboardSummaryViewProps) {
  if (loading) {
    return (
      <div role="status" aria-live="polite" className="state state--loading">
        Cargando resumen...
      </div>
    );
  }

  if (error !== null) {
    return (
      <div role="alert" className="state state--error">
        <p>{error}</p>
        {onRetry !== undefined && (
          <button type="button" className="button" onClick={onRetry}>
            Reintentar
          </button>
        )}
      </div>
    );
  }

  return (
    <div data-testid="dashboard-summary">
      <div className="dashboard-grid">
        <article className="dashboard-card" data-testid="dashboard-card-total">
          <h2 className="dashboard-card__title">Total</h2>
          <span className="dashboard-card__value">{summary.totalRequests}</span>
          <span className="field__hint">Solicitudes registradas</span>
        </article>
        <article className="dashboard-card" data-testid="dashboard-card-pending">
          <h2 className="dashboard-card__title">Pendientes</h2>
          <span className="dashboard-card__value">{summary.pendingRequests}</span>
          <span className="field__hint">Sin asignar o en revisión</span>
        </article>
        <article className="dashboard-card" data-testid="dashboard-card-assigned">
          <h2 className="dashboard-card__title">Asignadas</h2>
          <span className="dashboard-card__value">{summary.assignedRequests}</span>
          <span className="field__hint">Con responsable</span>
        </article>
        <article className="dashboard-card" data-testid="dashboard-card-unassigned">
          <h2 className="dashboard-card__title">Sin asignar</h2>
          <span className="dashboard-card__value">{summary.unassignedRequests}</span>
          <span className="field__hint">Requieren asignación</span>
        </article>
        <article
          className="dashboard-card dashboard-card--overdue"
          data-testid="dashboard-card-overdue"
        >
          <h2 className="dashboard-card__title">Vencidas</h2>
          <span className="dashboard-card__value">{summary.overdueRequests}</span>
          <span className="field__hint">Fecha compromiso superada</span>
        </article>
      </div>

      <div className="dashboard-grid">
        <article className="dashboard-card" data-testid="dashboard-card-status">
          <h2 className="dashboard-card__title">Por estado</h2>
          <ul className="dashboard-card__list">
            {StatusOrder.map((status) => {
              const value = getStatusValue(summary, status);
              return (
                <li key={`status-${status}`} className="dashboard-card__list-row">
                  <RequestStatusBadge status={status} />
                  <span>{value}</span>
                </li>
              );
            })}
          </ul>
        </article>
        <article className="dashboard-card" data-testid="dashboard-card-priority">
          <h2 className="dashboard-card__title">Por prioridad</h2>
          <ul className="dashboard-card__list">
            {PriorityOrder.map((priority) => {
              const value = getPriorityValue(summary, priority);
              return (
                <li key={`priority-${priority}`} className="dashboard-card__list-row">
                  <RequestPriorityBadge priority={priority} />
                  <span>{value}</span>
                </li>
              );
            })}
          </ul>
        </article>
      </div>

      <section className="request-detail__section" aria-label="Últimas solicitudes">
        <h2 className="request-detail__section-title">Últimas solicitudes</h2>
        {summary.recentRequests.length === 0 ? (
          <p className="field__hint" data-testid="dashboard-recent-empty">
            Aún no se han registrado solicitudes.
          </p>
        ) : (
          <table
            className="request-table"
            aria-label="Últimas solicitudes"
            data-testid="dashboard-recent-table"
          >
            <thead>
              <tr>
                <th scope="col">Código</th>
                <th scope="col">Título</th>
                <th scope="col">Estado</th>
                <th scope="col">Prioridad</th>
                <th scope="col">Responsable</th>
                <th scope="col">Fecha</th>
              </tr>
            </thead>
            <tbody>
              {summary.recentRequests.map((request) => (
                <tr key={request.id} data-testid={`dashboard-recent-row-${request.id}`}>
                  <td className="request-table__cell-narrow">
                    <Link
                      href={`/requests/detail?id=${request.id}`}
                      className="request-table__code"
                      data-testid={`dashboard-recent-code-${request.id}`}
                    >
                      {request.code}
                    </Link>
                  </td>
                  <td>{request.title}</td>
                  <td>
                    <RequestStatusBadge status={request.status} />
                  </td>
                  <td>
                    <RequestPriorityBadge priority={request.priority} />
                  </td>
                  <td>{request.responsibleUsername ?? '—'}</td>
                  <td>{formatDateTime(request.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        <p className="field__hint" data-testid="dashboard-status-legend">
          Estados: {describeRequestStatus(RequestStatus.Submitted)} · {describeRequestStatus(RequestStatus.InProgress)} · {describeRequestStatus(RequestStatus.Closed)}.
          Prioridades: {describeRequestPriority(RequestPriority.Low)} · {describeRequestPriority(RequestPriority.Critical)}.
        </p>
      </section>
    </div>
  );
}