import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { NewRequestPage } from '../../views/NewRequestPage';
import { renderWithProviders, buildTestServices } from '../testUtils';
import {
  RequestPriority,
  RequestStatus,
  type RequestRecord,
} from '../../types/request';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

const sample: RequestRecord = {
  id: '99999999-9999-9999-9999-999999999999',
  code: 'SOL-2026-0042',
  title: 'Nueva solicitud',
  description: 'Detalle de la solicitud',
  status: RequestStatus.Submitted,
  priority: RequestPriority.Medium,
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

describe('NewRequestPage', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  function setupAreasAndTypes() {
    fetchMock.mockImplementation((input: RequestInfo | URL) => {
      const url = typeof input === 'string' ? input : input.toString();
      if (url.includes('/api/catalogos/areas')) {
        return Promise.resolve(jsonResponse([{ id: 'area-1', name: 'Mantenimiento' }]));
      }
      if (url.includes('/api/catalogos/tipos-solicitud')) {
        return Promise.resolve(
          jsonResponse([{ id: 'type-1', name: 'Incidente', description: 'Reporte' }]),
        );
      }
      return Promise.reject(new TypeError(`Unexpected fetch ${url}`));
    });
  }

  it('submits the form and navigates to the new request detail', async () => {
    setupAreasAndTypes();
    fetchMock.mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.toString();
      if (init?.method === 'POST' && url.endsWith('/api/solicitudes')) {
        return Promise.resolve(jsonResponse(sample, 201));
      }
      if (url.includes('/api/catalogos/areas')) {
        return Promise.resolve(jsonResponse([{ id: 'area-1', name: 'Mantenimiento' }]));
      }
      if (url.includes('/api/catalogos/tipos-solicitud')) {
        return Promise.resolve(
          jsonResponse([{ id: 'type-1', name: 'Incidente', description: 'Reporte' }]),
        );
      }
      return Promise.reject(new TypeError(`Unexpected fetch ${url}`));
    });

    const user = userEvent.setup();
    renderWithProviders(<NewRequestPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    await waitFor(() => {
      expect(
        (screen.getByTestId('new-request-form-area') as HTMLSelectElement).options
          .length,
      ).toBeGreaterThan(1);
    });

    await user.type(screen.getByTestId('new-request-form-title'), 'Nueva solicitud');
    await user.type(
      screen.getByTestId('new-request-form-description'),
      'Detalle de la solicitud',
    );
    await user.selectOptions(screen.getByTestId('new-request-form-area'), 'area-1');
    await user.selectOptions(
      screen.getByTestId('new-request-form-request-type'),
      'type-1',
    );

    await user.click(screen.getByTestId('new-request-form-submit'));

    await waitFor(() => {
      const postCall = fetchMock.mock.calls.find(
        (call) => (call[1] as RequestInit)?.method === 'POST',
      );
      expect(postCall).toBeDefined();
    });

    const { getMockRouter } = await import('../nextNavigationState');
    await waitFor(() => {
      expect(getMockRouter().replace).toHaveBeenCalledWith(
        `/requests/detail?id=${encodeURIComponent(sample.id)}`,
      );
    });
  });

  it('shows backend validation errors when present', async () => {
    setupAreasAndTypes();
    fetchMock.mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.toString();
      if (init?.method === 'POST' && url.endsWith('/api/solicitudes')) {
        return Promise.resolve(
          jsonResponse(
            {
              type: 'requests.create.title.required',
              title: 'Invalid payload.',
              detail: 'El título es obligatorio.',
              status: 400,
            },
            400,
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
      return Promise.reject(new TypeError(`Unexpected fetch ${url}`));
    });

    const user = userEvent.setup();
    renderWithProviders(<NewRequestPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    await waitFor(() => {
      expect(
        (screen.getByTestId('new-request-form-area') as HTMLSelectElement).options
          .length,
      ).toBeGreaterThan(1);
    });

    await user.type(screen.getByTestId('new-request-form-description'), 'Detalle');
    await user.selectOptions(screen.getByTestId('new-request-form-area'), 'area-1');
    await user.selectOptions(
      screen.getByTestId('new-request-form-request-type'),
      'type-1',
    );
    await user.click(screen.getByTestId('new-request-form-submit'));

    await waitFor(() => {
      expect(
        screen.getByTestId('new-request-form-title-error'),
      ).toHaveTextContent('El título es obligatorio.');
    });
  });
});