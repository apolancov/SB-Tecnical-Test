'use client';

import Link from 'next/link';
import {
  RequestStatus,
  describeRequestPriority,
  describeRequestStatus,
  type CommentVisibility,
  type RequestDetail,
  type RequestStatus as RequestStatusType,
} from '../types/request';
import { isStaffRole, type UserRole } from '../types/auth';
import { RequestPriorityBadge, RequestStatusBadge } from './RequestBadges';
import { formatDate, formatDateTime } from './dateFormat';
import { RequestHistoryTimeline } from './RequestHistoryTimeline';
import { RequestComments } from './RequestComments';

export interface RequestAllowedTransitions {
  readonly statuses: ReadonlyArray<RequestStatusType>;
}

export interface RequestDetailViewProps {
  readonly request: RequestDetail;
  readonly role: UserRole;
  readonly commentsSubmitting: boolean;
  readonly commentsError: string | null;
  readonly onAddComment: (input: { text: string; visibility: CommentVisibility }) => void;
  readonly canEdit: boolean;
  readonly canChangeStatus: boolean;
  readonly canAssign: boolean;
  readonly canReopen: boolean;
  readonly onChangeStatus: () => void;
  readonly onAssign: () => void;
  readonly onReopen: () => void;
  readonly onEdit: () => void;
}

function defaultAllowedTransitions(current: RequestStatusType): RequestAllowedTransitions {
  switch (current) {
    case RequestStatus.Submitted:
      return { statuses: [RequestStatus.InReview, RequestStatus.Assigned] };
    case RequestStatus.InReview:
      return {
        statuses: [RequestStatus.Assigned, RequestStatus.InProgress, RequestStatus.OnHold],
      };
    case RequestStatus.Assigned:
      return { statuses: [RequestStatus.InProgress, RequestStatus.OnHold] };
    case RequestStatus.InProgress:
      return {
        statuses: [RequestStatus.OnHold, RequestStatus.Resolved, RequestStatus.Closed],
      };
    case RequestStatus.OnHold:
      return { statuses: [RequestStatus.InProgress] };
    case RequestStatus.Resolved:
      return { statuses: [RequestStatus.Closed, RequestStatus.InProgress] };
    case RequestStatus.Closed:
      return { statuses: [RequestStatus.InProgress] };
    case RequestStatus.Cancelled:
      return { statuses: [] };
    default:
      return { statuses: [] };
  }
}

export function RequestDetailView({
  request,
  role,
  commentsSubmitting,
  commentsError,
  onAddComment,
  canEdit,
  canChangeStatus,
  canAssign,
  canReopen,
  onChangeStatus,
  onAssign,
  onReopen,
  onEdit,
}: RequestDetailViewProps) {
  const isClosed = request.status === RequestStatus.Closed;
  const isStaff = isStaffRole(role);
  const showInternalComments = isStaff;
  const allowedTransitions = defaultAllowedTransitions(request.status);

  return (
    <section className="page" data-testid="request-detail-view">
      <header className="page__header">
        <div className="page__header-row">
          <div>
            <p className="page__meta">
              <Link href="/requests" className="request-table__code">
                ← Volver al listado
              </Link>
            </p>
            <h1 className="page__title">
              {request.code} · {request.title}
            </h1>
            <p className="page__subtitle">{request.area.name} · {request.requestType.name}</p>
            <div className="request-detail__actions">
              <RequestStatusBadge status={request.status} />
              <RequestPriorityBadge priority={request.priority} />
              <span className="badge">{describeRequestStatus(request.status)}</span>
              <span className="badge">Prioridad: {describeRequestPriority(request.priority)}</span>
            </div>
          </div>
          <div className="request-detail__actions">
            {canEdit && (
              <button
                type="button"
                className="button"
                onClick={onEdit}
                data-testid="request-detail-edit"
              >
                Editar
              </button>
            )}
            {canChangeStatus && !isClosed && allowedTransitions.statuses.length > 0 && (
              <button
                type="button"
                className="button button--primary"
                onClick={onChangeStatus}
                data-testid="request-detail-change-status"
              >
                Cambiar estado
              </button>
            )}
            {canAssign && !isClosed && (
              <button
                type="button"
                className="button"
                onClick={onAssign}
                data-testid="request-detail-assign"
              >
                {request.responsible === null ? 'Asignar' : 'Reasignar'}
              </button>
            )}
            {canReopen && isClosed && (
              <button
                type="button"
                className="button button--primary"
                onClick={onReopen}
                data-testid="request-detail-reopen"
              >
                Reabrir
              </button>
            )}
          </div>
        </div>
      </header>

      <div className="request-detail">
        <div>
          <section className="request-detail__section" aria-label="Información de la solicitud">
            <h2 className="request-detail__section-title">Información</h2>
            <dl className="request-detail__meta">
              <dt>Código</dt>
              <dd data-testid="request-detail-code">{request.code}</dd>
              <dt>Título</dt>
              <dd data-testid="request-detail-title">{request.title}</dd>
              <dt>Descripción</dt>
              <dd data-testid="request-detail-description">{request.description}</dd>
              <dt>Estado</dt>
              <dd>
                <RequestStatusBadge status={request.status} />
              </dd>
              <dt>Prioridad</dt>
              <dd>
                <RequestPriorityBadge priority={request.priority} />
              </dd>
              <dt>Área</dt>
              <dd data-testid="request-detail-area">{request.area.name}</dd>
              <dt>Tipo</dt>
              <dd data-testid="request-detail-type">{request.requestType.name}</dd>
              <dt>Solicitante</dt>
              <dd data-testid="request-detail-requester">
                {request.requester.username} ({request.requester.email})
              </dd>
              <dt>Responsable</dt>
              <dd data-testid="request-detail-responsible">
                {request.responsible === null
                  ? '—'
                  : `${request.responsible.username} (${request.responsible.email})`}
              </dd>
              <dt>Fecha de creación</dt>
              <dd>{formatDateTime(request.createdAt)}</dd>
              <dt>Fecha compromiso</dt>
              <dd>{request.dueDate === null ? '—' : formatDate(request.dueDate)}</dd>
              <dt>Cerrada el</dt>
              <dd>{request.closedAt === null ? '—' : formatDateTime(request.closedAt)}</dd>
              <dt>Evidencia</dt>
              <dd>
                {request.evidenceUrl === null ? (
                  '—'
                ) : (
                  <a href={request.evidenceUrl} target="_blank" rel="noopener noreferrer">
                    {request.evidenceUrl}
                  </a>
                )}
              </dd>
            </dl>
          </section>

          <section
            className="request-detail__section"
            aria-label="Historial de la solicitud"
            data-testid="request-detail-history"
          >
            <h2 className="request-detail__section-title">Historial</h2>
            <RequestHistoryTimeline history={request.statusHistory} />
          </section>
        </div>
        <div>
          <section
            className="request-detail__section"
            aria-label="Comentarios"
            data-testid="request-detail-comments"
          >
            <h2 className="request-detail__section-title">Comentarios</h2>
            <RequestComments
              comments={request.comments}
              allowInternal={showInternalComments}
              submitting={commentsSubmitting}
              error={commentsError}
              onAdd={onAddComment}
            />
          </section>
        </div>
      </div>
    </section>
  );
}