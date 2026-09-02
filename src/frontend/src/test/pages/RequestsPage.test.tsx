import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RequestsPage } from '../../views/RequestsPage';
import { renderWithProviders, buildTestServices } from '../testUtils';
import {
  RequestPriority,
  RequestStatus,
  type RequestRecord,
} from '../../types/request';
import type { PaginatedResponse } from '../../types/pagination';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

const sample: RequestRecord = {
  id: '11111111-1111-1111-1111-111111111111',
  code: 'SOL-2026-0001',
  title: 'Reparación',
  description: 'Detalle',
  status: RequestStatus.Submitted,
  priority: RequestPriority.High,
  createdAt: '2026-09-01T10:00:00Z',
  dueDate: null,
  evidenceUrl: null,
  closedAt: null,
  areaId: '22222222-2222-2222-2222-222222222222',
  area: 'Mantenimiento',
  requestTypeId: '33333333-3333-3333-3333-333333333333',
  requestType: 'Incidente',
  requesterId: '44444444-4444-4444-4444-444444444444',
  requesterUsername: 'juan',
  requesterEmail: 'juan@example.local',
  responsibleId: null,
  responsibleUsername: null,
  responsibleEmail: null,
};

const authenticatedState = {
  accessToken: 'jwt',
  expiresAt: new Date(Date.now() + 60_000).toISOString(),
  user: {
    id: '1',
    username: 'admin',
    email: 'admin@example.local',
    role: 'Admin' as const,
  },
};

function setupFetchSequence(
  fetchMock: ReturnType<typeof vi.fn>,
  sequence: PaginatedResponse<RequestRecord>[],
) {
  let index = 0;
  fetchMock.mockImplementation((input: RequestInfo | URL) => {
    const url = typeof input === 'string' ? input : input.toString();
    if (url.includes('/api/catalogos/areas')) {
      return Promise.resolve(jsonResponse([{ id: 'a1', name: 'Mantenimiento' }]));
    }
    if (url.includes('/api/catalogos/tipos-solicitud')) {
      return Promise.resolve(
        jsonResponse([{ id: 't1', name: 'Incidente', description: 'Reporte' }]),
      );
    }
    const next = sequence[Math.min(index, sequence.length - 1)];
    index += 1;
    return Promise.resolve(jsonResponse(next));
  });
}

describe('RequestsPage', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it('lists the requests returned by the backend', async () => {
    setupFetchSequence(fetchMock, [
      {
        items: [sample],
        page: 1,
        pageSize: 20,
        totalItems: 1,
        totalPages: 1,
      },
    ]);

    renderWithProviders(<RequestsPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    await screen.findByText('SOL-2026-0001');
    expect(screen.getByText('Reparación')).toBeInTheDocument();
    expect(screen.getByText(/Página 1 de 1/)).toBeInTheDocument();
  });

  it('renders the empty state when no requests are returned', async () => {
    setupFetchSequence(fetchMock, [
      { items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 },
    ]);

    renderWithProviders(<RequestsPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    await waitFor(() => {
      expect(
        screen.getByText(/no hay solicitudes que coincidan con los filtros/i),
      ).toBeInTheDocument();
    });
  });

  it('re-fetches when a filter is submitted', async () => {
    setupFetchSequence(fetchMock, [
      {
        items: [sample],
        page: 1,
        pageSize: 20,
        totalItems: 1,
        totalPages: 1,
      },
      {
        items: [],
        page: 1,
        pageSize: 20,
        totalItems: 0,
        totalPages: 0,
      },
    ]);

    const user = userEvent.setup();
    renderWithProviders(<RequestsPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    await screen.findByText('SOL-2026-0001');

    await user.type(screen.getByTestId('filter-request-search'), 'urgente');
    await user.click(screen.getByTestId('filter-request-submit'));

    await waitFor(() => {
      expect(
        screen.getByText(/no hay solicitudes que coincidan con los filtros/i),
      ).toBeInTheDocument();
    });

    const calls = fetchMock.mock.calls.map(
      (call) => (call as [string, RequestInit])[0],
    );
    const lastSearch = calls.filter((url) => url.includes('/api/solicitudes')).pop();
    expect(lastSearch).toContain('search=urgente');
  });
});