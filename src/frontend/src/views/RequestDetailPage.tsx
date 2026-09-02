'use client';

import Link from 'next/link';
import { useEffect, useRef, useState } from 'react';
import { RequestAssignModal } from '../components/RequestAssignModal';
import { RequestDetailView } from '../components/RequestDetailView';
import { RequestEditModal } from '../components/RequestEditModal';
import { RequestStatusChangeModal } from '../components/RequestStatusChangeModal';
import { useAuth } from '../hooks/useAuth';
import {
  useCatalogAreas,
  useCatalogRequestTypes,
  useCatalogStaff,
} from '../hooks/useCatalogLookups';
import { useRequestDetail } from '../hooks/useRequestDetail';
import { useRequestMutation } from '../hooks/useRequestMutation';
import { useService } from '../hooks/ServiceContext';
import { UserRole, isStaffRole } from '../types/auth';
import {
  RequestStatus,
  type AddCommentInput,
  type AssignRequestInput,
  type ChangeRequestStatusInput,
  type CommentVisibility,
  type ReopenRequestInput,
  type RequestStatus as RequestStatusType,
  type UpdateRequestInput,
} from '../types/request';

export interface RequestDetailPageProps {
  readonly requestId: string | null;
}

function defaultAllowedTransitions(current: RequestStatusType): RequestStatusType[] {
  switch (current) {
    case RequestStatus.Submitted:
      return [RequestStatus.InReview, RequestStatus.Assigned];
    case RequestStatus.InReview:
      return [RequestStatus.Assigned, RequestStatus.InProgress, RequestStatus.OnHold];
    case RequestStatus.Assigned:
      return [RequestStatus.InProgress, RequestStatus.OnHold];
    case RequestStatus.InProgress:
      return [RequestStatus.OnHold, RequestStatus.Resolved, RequestStatus.Closed];
    case RequestStatus.OnHold:
      return [RequestStatus.InProgress];
    case RequestStatus.Resolved:
      return [RequestStatus.Closed, RequestStatus.InProgress];
    case RequestStatus.Closed:
      return [RequestStatus.InProgress];
    case RequestStatus.Cancelled:
    default:
      return [];
  }
}

export function RequestDetailPage({ requestId }: RequestDetailPageProps) {
  const { user } = useAuth();
  const { requestService, catalogService } = useService();

  const { request, loading, error, refresh } = useRequestDetail({
    service: requestService,
    requestId,
  });

  const mutation = useRequestMutation({
    service: requestService,
    callbacks: {
      onUpdated: () => refresh(),
      onStatusChanged: () => refresh(),
      onAssigned: () => refresh(),
      onReopened: () => refresh(),
      onCommentAdded: () => refresh(),
    },
  });

  const role: UserRole = user?.role ?? UserRole.User;
  const isStaff = isStaffRole(role);

  const { areas } = useCatalogAreas({ service: catalogService });
  const { requestTypes } = useCatalogRequestTypes({ service: catalogService });
  const {
    staff,
    loading: staffLoading,
    error: staffError,
  } = useCatalogStaff({ service: catalogService, enabled: isStaff });

  const [editing, setEditing] = useState<boolean>(false);
  const [changeStatusOpen, setChangeStatusOpen] = useState<boolean>(false);
  const [assignOpen, setAssignOpen] = useState<boolean>(false);
  const [reopenOpen, setReopenOpen] = useState<boolean>(false);

  const isClosed = request?.status === RequestStatus.Closed;
  const allowedNextStatuses =
    request === null ? [] : defaultAllowedTransitions(request.status);

  const mutationRef = useRef(mutation);
  mutationRef.current = mutation;

  useEffect(() => {
    setEditing(false);
    setChangeStatusOpen(false);
    setAssignOpen(false);
    setReopenOpen(false);
    mutationRef.current.reset();
  }, [requestId]);

  if (loading && request === null) {
    return (
      <section className="page" data-testid="request-detail-loading">
        <div role="status" aria-live="polite" className="state state--loading">
          Cargando solicitud...
        </div>
      </section>
    );
  }

  if (error !== null) {
    return (
      <section className="page" data-testid="request-detail-error">
        <header className="page__header">
          <p className="page__meta">
            <Link href="/requests" className="request-table__code">
              ← Volver al listado
            </Link>
          </p>
        </header>
        <div role="alert" className="state state--error">
          <p>{error}</p>
          <button type="button" className="button" onClick={refresh}>
            Reintentar
          </button>
        </div>
      </section>
    );
  }

  if (request === null) {
    return (
      <section className="page" data-testid="request-detail-missing">
        <header className="page__header">
          <p className="page__meta">
            <Link href="/requests" className="request-table__code">
              ← Volver al listado
            </Link>
          </p>
        </header>
        <div role="status" className="state state--empty">
          No se encontró la solicitud.
        </div>
      </section>
    );
  }

  const handleEditSubmit = (input: UpdateRequestInput) => {
    void mutation.update(request.id, input).then((result) => {
      if (result !== null) {
        setEditing(false);
      }
    });
  };

  const handleChangeStatusSubmit = (input: ChangeRequestStatusInput) => {
    void mutation.changeStatus(request.id, input).then((result) => {
      if (result !== null) {
        setChangeStatusOpen(false);
      }
    });
  };

  const handleAssignSubmit = (input: AssignRequestInput) => {
    void mutation.assign(request.id, input).then((result) => {
      if (result !== null) {
        setAssignOpen(false);
      }
    });
  };

  const handleReopenSubmit = (input: ReopenRequestInput) => {
    void mutation.reopen(request.id, input).then((result) => {
      if (result !== null) {
        setReopenOpen(false);
      }
    });
  };

  const handleAddComment = (input: { text: string; visibility: CommentVisibility }) => {
    const addInput: AddCommentInput = input;
    void mutation.addComment(request.id, addInput);
  };

  return (
    <>
      <RequestDetailView
        request={request}
        role={role}
        commentsSubmitting={Boolean(mutation.kind === 'addComment' && mutation.submitting)}
        commentsError={mutation.kind === 'addComment' ? mutation.error : null}
        onAddComment={handleAddComment}
        canEdit={
          isStaff ||
          (request.requesterId === user?.id && request.status === RequestStatus.Submitted)
        }
        canChangeStatus={isStaff && !isClosed && allowedNextStatuses.length > 0}
        canAssign={isStaff && !isClosed}
        canReopen={isStaff && isClosed}
        onChangeStatus={() => {
          mutation.reset();
          setChangeStatusOpen(true);
        }}
        onAssign={() => {
          mutation.reset();
          setAssignOpen(true);
        }}
        onReopen={() => {
          mutation.reset();
          setReopenOpen(true);
        }}
        onEdit={() => {
          mutation.reset();
          setEditing(true);
        }}
      />

      {editing && (
        <RequestEditModal
          open
          request={request}
          submitting={mutation.submitting && mutation.kind === 'update'}
          error={mutation.kind === 'update' ? mutation.error : null}
          areas={areas}
          requestTypes={requestTypes}
          onSubmit={handleEditSubmit}
          onCancel={() => {
            setEditing(false);
            mutation.reset();
          }}
        />
      )}

      {changeStatusOpen && (
        <RequestStatusChangeModal
          open
          currentStatus={request.status}
          allowedNextStatuses={allowedNextStatuses}
          submitting={Boolean(mutation.submitting && mutation.kind === 'changeStatus')}
          error={mutation.kind === 'changeStatus' ? mutation.error : null}
          requireComment
          onSubmit={handleChangeStatusSubmit}
          onCancel={() => {
            setChangeStatusOpen(false);
            mutation.reset();
          }}
        />
      )}

      {assignOpen && (
        <RequestAssignModal
          open
          currentResponsible={request.responsible?.username ?? null}
          candidates={staff}
          candidatesLoading={staffLoading}
          candidatesError={staffError}
          submitting={mutation.submitting && mutation.kind === 'assign'}
          error={mutation.kind === 'assign' ? mutation.error : null}
          onSubmit={handleAssignSubmit}
          onCancel={() => {
            setAssignOpen(false);
            mutation.reset();
          }}
        />
      )}

      {reopenOpen && (
        <RequestStatusChangeModal
          open
          currentStatus={request.status}
          allowedNextStatuses={[RequestStatus.InProgress]}
          submitting={mutation.submitting && mutation.kind === 'reopen'}
          error={mutation.kind === 'reopen' ? mutation.error : null}
          title="Reabrir solicitud"
          requireComment={false}
          onSubmit={({ newStatus, comment }) =>
            handleReopenSubmit({ targetStatus: newStatus, comment })
          }
          onCancel={() => {
            setReopenOpen(false);
            mutation.reset();
          }}
        />
      )}
    </>
  );
}