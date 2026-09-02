import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RequestDetailPage } from '../../views/RequestDetailPage';
import { renderWithProviders, buildTestServices } from '../testUtils';
import {
  CommentVisibility,
  RequestPriority,
  RequestStatus,
  type RequestDetail,
} from '../../types/request';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

const sample: RequestDetail = {
  id: '11111111-1111-1111-1111-111111111111',
  code: 'SOL-2026-0001',
  title: 'Reparación',
  description: 'Detalle',
  status: RequestStatus.InProgress,
  priority: RequestPriority.High,
  createdAt: '2026-09-01T10:00:00Z',
  dueDate: null,
  evidenceUrl: null,
  closedAt: null,
  requester: {
    id: 'requester-1',
    username: 'juan',
    email: 'juan@example.local',
    role: 'Solicitante',
  },
  responsible: {
    id: 'analista-1',
    username: 'ana',
    email: 'ana@example.local',
    role: 'Analista',
  },
  areaId: 'area-1',
  requestTypeId: 'type-1',
  requesterId: 'requester-1',
  requesterUsername: 'juan',
  requesterEmail: 'juan@example.local',
  responsibleId: 'analista-1',
  responsibleUsername: 'ana',
  responsibleEmail: 'ana@example.local',
  area: { id: 'area-1', name: 'Mantenimiento' },
  requestType: { id: 'type-1', name: 'Incidente' },
  statusHistory: [
    {
      id: 'h1',
      previousStatus: RequestStatus.Submitted,
      newStatus: RequestStatus.InProgress,
      date: '2026-09-01T10:00:00Z',
      comment: '',
      changedById: 'requester-1',
      changedByUsername: 'juan',
    },
  ],
  comments: [
    {
      id: 'c1',
      text: 'Hola',
      visibility: CommentVisibility.Requester,
      date: '2026-09-01T11:00:00Z',
      authorId: 'requester-1',
      authorUsername: 'juan',
    },
  ],
};

const authenticatedState = {
  accessToken: 'jwt',
  expiresAt: new Date(Date.now() + 60_000).toISOString(),
  user: {
    id: 'admin-1',
    username: 'admin',
    email: 'admin@example.local',
    role: 'Admin' as const,
  },
};

function detailMock() {
  return (input: RequestInfo | URL, init?: RequestInit) => {
    const url = typeof input === 'string' ? input : input.toString();
    if (url.includes('/api/catalogos/areas')) {
      return Promise.resolve(jsonResponse([{ id: 'area-1', name: 'Mantenimiento' }]));
    }
    if (url.includes('/api/catalogos/tipos-solicitud')) {
      return Promise.resolve(
        jsonResponse([{ id: 'type-1', name: 'Incidente', description: 'Reporte' }]),
      );
    }
    if (url.includes('/api/catalogos/responsables')) {
      return Promise.resolve(
        jsonResponse([
          { id: 'admin-1', username: 'admin', email: 'admin@example.local', role: 'Admin' },
          { id: 'ana-1', username: 'ana', email: 'ana@example.local', role: 'Analista' },
        ]),
      );
    }
    if (url.includes(`/api/solicitudes/${sample.id}`)) {
      return Promise.resolve(jsonResponse(sample));
    }
    void init;
    return Promise.reject(new TypeError(`Unexpected fetch ${url}`));
  };
}

describe('RequestDetailPage', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it('renders the detail returned by the backend', async () => {
    fetchMock.mockImplementation(detailMock());

    renderWithProviders(<RequestDetailPage requestId={sample.id} />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    await waitFor(() => {
      expect(screen.getByTestId('request-detail-code')).toHaveTextContent('SOL-2026-0001');
    });
    expect(screen.getByTestId('request-detail-title')).toHaveTextContent('Reparación');
  });

  it('submits a new comment via POST /comentarios', async () => {
    fetchMock.mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.toString();
      if (
        init?.method === 'POST' &&
        url.includes(`/api/solicitudes/${sample.id}`) &&
        url.endsWith('/comentarios')
      ) {
        return Promise.resolve(
          jsonResponse(
            {
              id: 'c-new',
              text: 'Listo',
              visibility: CommentVisibility.Requester,
              date: '2026-09-01T13:00:00Z',
              authorId: 'admin-1',
              authorUsername: 'admin',
            },
            201,
          ),
        );
      }
      if (url.includes('/api/catalogos/areas')) {
        return Promise.resolve(jsonResponse([{ id: 'area-1', name: 'Mantenimiento' }]));
      }
      if (url.includes('/api/catalogos/tipos-solicitud')) {
        return Promise.resolve(
          jsonResponse([{ id: 'type-1', name: 'Incidente', description: 'Reporte' }]),
        );
      }
      if (url.includes('/api/catalogos/responsables')) {
        return Promise.resolve(
          jsonResponse([
            { id: 'admin-1', username: 'admin', email: 'admin@example.local', role: 'Admin' },
          ]),
        );
      }
      if (url.includes(`/api/solicitudes/${sample.id}`)) {
        return Promise.resolve(jsonResponse({ ...sample, comments: [] }));
      }
      return Promise.reject(new TypeError(`Unexpected fetch ${url}`));
    });

    const user = userEvent.setup();
    renderWithProviders(<RequestDetailPage requestId={sample.id} />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    await screen.findByTestId('request-detail-code');

    await user.type(screen.getByTestId('comment-form-text'), 'Listo');
    await user.click(screen.getByTestId('comment-form-submit'));

    await waitFor(() => {
      const postCalls = fetchMock.mock.calls.filter(
        (call) => (call[1] as RequestInit)?.method === 'POST',
      );
      expect(postCalls.length).toBeGreaterThan(0);
    });
  });

  it('opens the status-change modal and PATCHes /estado', async () => {
    fetchMock.mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.toString();
      if (
        init?.method === 'PATCH' &&
        url.includes(`/api/solicitudes/${sample.id}/estado`)
      ) {
        return Promise.resolve(
          jsonResponse({ ...sample, status: RequestStatus.OnHold }),
        );
      }
      if (url.includes('/api/catalogos/areas')) {
        return Promise.resolve(jsonResponse([{ id: 'area-1', name: 'Mantenimiento' }]));
      }
      if (url.includes('/api/catalogos/tipos-solicitud')) {
        return Promise.resolve(
          jsonResponse([{ id: 'type-1', name: 'Incidente', description: 'Reporte' }]),
        );
      }
      if (url.includes('/api/catalogos/responsables')) {
        return Promise.resolve(
          jsonResponse([
            { id: 'admin-1', username: 'admin', email: 'admin@example.local', role: 'Admin' },
          ]),
        );
      }
      if (url.includes(`/api/solicitudes/${sample.id}`)) {
        return Promise.resolve(jsonResponse(sample));
      }
      return Promise.reject(new TypeError(`Unexpected fetch ${url}`));
    });

    const user = userEvent.setup();
    renderWithProviders(<RequestDetailPage requestId={sample.id} />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    await screen.findByTestId('request-detail-code');

    await waitFor(() => {
      expect(
        screen.queryByTestId('request-detail-change-status'),
      ).toBeInTheDocument();
    });

    const button = screen.getByTestId('request-detail-change-status');
    await user.click(button);

    await waitFor(() => {
      expect(screen.queryByTestId('request-status-modal')).toBeInTheDocument();
    });

    await user.type(
      screen.getByTestId('request-status-modal-comment'),
      'Iniciando revisión',
    );
    await user.click(screen.getByTestId('request-status-modal-submit'));

    await waitFor(() => {
      const patchCalls = fetchMock.mock.calls.filter(
        (call) => (call[1] as RequestInit)?.method === 'PATCH',
      );
      expect(patchCalls.length).toBeGreaterThan(0);
    });
  });

  it('hides internal comments for Solicitante users', async () => {
    const detail: RequestDetail = {
      ...sample,
      comments: [
        {
          id: 'c-public',
          text: 'público',
          visibility: CommentVisibility.Requester,
          date: '2026-09-01T11:00:00Z',
          authorId: 'requester-1',
          authorUsername: 'juan',
        },
        {
          id: 'c-internal',
          text: 'interno',
          visibility: CommentVisibility.Internal,
          date: '2026-09-01T11:30:00Z',
          authorId: 'analista-1',
          authorUsername: 'ana',
        },
      ],
    };

    fetchMock.mockImplementation((input: RequestInfo | URL) => {
      const url = typeof input === 'string' ? input : input.toString();
      if (url.includes(`/api/solicitudes/${sample.id}`)) {
        return Promise.resolve(jsonResponse(detail));
      }
      return Promise.resolve(jsonResponse([]));
    });

    const solicitanteState = {
      accessToken: 'jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: {
        id: 'requester-1',
        username: 'juan',
        email: 'juan@example.local',
        role: 'Solicitante' as const,
      },
    };

    renderWithProviders(<RequestDetailPage requestId={sample.id} />, {
      services: buildTestServices(solicitanteState),
      initialAuthentication: solicitanteState,
    });

    await screen.findByTestId('request-detail-code');

    expect(screen.queryByTestId('request-comments-tab-internal')).not.toBeInTheDocument();
    expect(screen.getByTestId('comment-form-visibility-Requester')).toBeInTheDocument();
    expect(screen.queryByTestId('comment-form-visibility-Internal')).not.toBeInTheDocument();
  });
});