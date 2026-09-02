import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { DashboardPage } from '../../views/DashboardPage';
import { renderWithProviders, buildTestServices } from '../testUtils';
import { EmptyDashboardSummary } from '../../types/dashboard';

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
    id: '1',
    username: 'admin',
    email: 'admin@example.local',
    role: 'Admin' as const,
  },
};

describe('DashboardPage', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it('renders summary cards from the dashboard endpoint', async () => {
    const payload = {
      ...EmptyDashboardSummary,
      totalRequests: 5,
      overdueRequests: 1,
      pendingRequests: 2,
      assignedRequests: 2,
      unassignedRequests: 1,
    };
    fetchMock.mockResolvedValue(jsonResponse(payload));

    renderWithProviders(<DashboardPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    expect(await screen.findByTestId('dashboard-card-total')).toHaveTextContent('5');
    expect(screen.getByTestId('dashboard-card-overdue')).toHaveTextContent('1');
  });

  it('shows an error state when the dashboard endpoint fails', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ title: 'Server', detail: 'fail' }, 500),
    );

    renderWithProviders(<DashboardPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    const alert = await screen.findByRole('alert');
    expect(alert).toBeInTheDocument();
  });

  it('keeps the loading state visible until the request resolves', async () => {
    let resolveResponse: ((value: Response) => void) | null = null;
    fetchMock.mockImplementation(
      () =>
        new Promise<Response>((resolve) => {
          resolveResponse = resolve;
        }),
    );

    renderWithProviders(<DashboardPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    expect(screen.getByText(/cargando resumen/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(typeof resolveResponse).toBe('function');
    });
    if (resolveResponse) {
      (resolveResponse as (value: Response) => void)(
        jsonResponse({
          ...EmptyDashboardSummary,
          totalRequests: 1,
        }),
      );
    }

    await waitFor(() => {
      expect(screen.queryByText(/cargando resumen/i)).not.toBeInTheDocument();
    });
  });
});