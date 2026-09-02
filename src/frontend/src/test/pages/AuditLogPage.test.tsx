import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AuditLogPage } from '../../views/AuditLogPage';
import { buildTestServices, renderWithProviders } from '../testUtils';
import {
  AuditAction,
  AuditOutcome,
  type AuditLogEntry,
} from '../../types/audit';
import type { PaginatedResponse } from '../../types/pagination';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

const authenticatedState = {
  accessToken: 'jwt',
  expiresAt: new Date(Date.now() + 60_000).toISOString(),
  user: {
    id: '11111111-1111-1111-1111-111111111111',
    username: 'admin',
    email: 'admin@example.local',
    role: 'Admin' as const,
  },
};

describe('AuditLogPage', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  const sampleEntry: AuditLogEntry = {
    id: '11111111-1111-1111-1111-111111111111',
    timestamp: '2026-09-02T10:00:00.000Z',
    action: AuditAction.LoginSucceeded,
    outcome: AuditOutcome.Success,
    entityType: 'User',
    entityId: 'user-1',
    details: 'Login OK',
    ipAddress: '127.0.0.1',
    actorUserId: 'user-1',
    actorUserName: 'admin',
  };

  function mockListResponse(payload: PaginatedResponse<AuditLogEntry>): void {
    fetchMock.mockResolvedValueOnce(jsonResponse(payload));
  }

  function mockDetailResponse(entry: AuditLogEntry): void {
    fetchMock.mockResolvedValueOnce(jsonResponse(entry));
  }

  it('renders the entries returned by the service', async () => {
    mockListResponse({
      items: [sampleEntry],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
    });

    renderWithProviders(<AuditLogPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    const table = await screen.findByRole('table', {
      name: /bitácora de auditoría/i,
    });
    expect(table).toBeInTheDocument();
    expect(table).toHaveTextContent('Inicio de sesión exitoso');
    expect(screen.getByTestId(`audit-log-row-${sampleEntry.id}`)).toBeInTheDocument();
  });

  it('opens the detail modal when the detail button is clicked', async () => {
    mockListResponse({
      items: [sampleEntry],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
    });
    mockDetailResponse(sampleEntry);

    const user = userEvent.setup();

    renderWithProviders(<AuditLogPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    await user.click(
      await screen.findByTestId(`audit-log-detail-${sampleEntry.id}`),
    );

    const dialog = await screen.findByRole('dialog');
    expect(dialog).toHaveTextContent(/Detalle de entrada de auditoría/);
    expect(dialog).toHaveTextContent(/admin/);
    expect(dialog).toHaveTextContent(/127\.0\.0\.1/);
  });

  it('shows an error state when the search endpoint returns 500', async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse({ title: 'Server', detail: 'fail' }, 500),
    );

    renderWithProviders(<AuditLogPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    const alert = await screen.findByRole('alert');
    expect(alert).toBeInTheDocument();
  });

  it('shows an empty state when the service returns no entries', async () => {
    mockListResponse({
      items: [],
      page: 1,
      pageSize: 20,
      totalItems: 0,
      totalPages: 0,
    });

    renderWithProviders(<AuditLogPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    expect(
      await screen.findByText(/no hay entradas que coincidan/i),
    ).toBeInTheDocument();
  });

  it('refetches when the refresh button is clicked', async () => {
    mockListResponse({
      items: [sampleEntry],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
    });
    mockListResponse({
      items: [],
      page: 1,
      pageSize: 20,
      totalItems: 0,
      totalPages: 0,
    });

    const user = userEvent.setup();

    renderWithProviders(<AuditLogPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(1);
    });

    await user.click(screen.getByTestId('audit-log-refresh'));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(2);
    });
  });
});