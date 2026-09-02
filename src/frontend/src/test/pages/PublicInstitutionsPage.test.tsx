import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { PublicInstitutionsPage } from '../../views/PublicInstitutionsPage';
import { resetNavigationMocks } from '../nextNavigationState';
import { renderWithProviders, buildTestServices } from '../testUtils';
import type { PaginatedResponse, Institution } from '../../types/institution';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

const firstPage: PaginatedResponse<Institution> = {
  items: [
    { id: '1', name: 'Acuario Nacional', category: 'Cat', statePower: 'Poder Ejecutivo', sector: 'Sector' },
    { id: '2', name: 'Archivo General', category: 'Cat', statePower: 'Poder Ejecutivo', sector: 'Cultura' },
  ],
  page: 1,
  pageSize: 20,
  totalItems: 45,
  totalPages: 3,
};

function setupFetchSequence(
  fetchMock: ReturnType<typeof vi.fn>,
  sequence: PaginatedResponse<Institution>[],
) {
  let index = 0;
  fetchMock.mockImplementation((input: RequestInfo | URL) => {
    const url = typeof input === 'string' ? input : input.toString();
    if (url.includes('/api/institutions/filter-options')) {
      return Promise.resolve(
        jsonResponse({
          categories: ['Cat'],
          statePowers: ['Poder Ejecutivo'],
          sectors: ['Sector', 'Cultura'],
        }),
      );
    }
    const next = sequence[Math.min(index, sequence.length - 1)];
    index += 1;
    return Promise.resolve(jsonResponse(next));
  });
}

describe('PublicInstitutionsPage', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
    resetNavigationMocks();
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    resetNavigationMocks();
  });

  it('renders the institution catalog without authentication', async () => {
    setupFetchSequence(fetchMock, [firstPage]);

    renderWithProviders(<PublicInstitutionsPage />);

    expect(
      await screen.findByRole('heading', { name: /instituciones gubernamentales/i }),
    ).toBeInTheDocument();
    expect(screen.getByText('Acuario Nacional')).toBeInTheDocument();
    expect(screen.getByText('Archivo General')).toBeInTheDocument();
    expect(screen.getByText(/Página 1 de 3/)).toBeInTheDocument();
  });

  it('does not render the New institution link', async () => {
    setupFetchSequence(fetchMock, [firstPage]);

    renderWithProviders(<PublicInstitutionsPage />);

    await screen.findByText('Acuario Nacional');

    expect(screen.queryByTestId('new-institution-link')).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /nueva institución/i })).not.toBeInTheDocument();
  });

  it('does not render edit or delete buttons for any row', async () => {
    setupFetchSequence(fetchMock, [firstPage]);

    renderWithProviders(<PublicInstitutionsPage />);

    await screen.findByText('Acuario Nacional');
    await screen.findByText('Archivo General');

    expect(
      screen.queryByRole('button', { name: /editar acuario nacional/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /eliminar acuario nacional/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /eliminar archivo general/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /editar archivo general/i }),
    ).not.toBeInTheDocument();
  });

  it('does not render the Acciones column in the table', async () => {
    setupFetchSequence(fetchMock, [firstPage]);

    renderWithProviders(<PublicInstitutionsPage />);

    await screen.findByText('Acuario Nacional');

    const headers = screen.getAllByRole('columnheader').map((header) => header.textContent);
    expect(headers).toEqual([
      'Nombre',
      'Categoría',
      'Poder del Estado',
      'Sector',
    ]);
    expect(headers.some((value) => value === 'Acciones')).toBe(false);
  });

  it('exposes a link to the admin login in the public header', async () => {
    setupFetchSequence(fetchMock, [firstPage]);

    renderWithProviders(<PublicInstitutionsPage />);

    const loginLink = await screen.findByTestId('public-shell-login-link');
    expect(loginLink).toHaveAttribute('href', '/login');
  });

  it('does not attach an Authorization header when no session is active', async () => {
    setupFetchSequence(fetchMock, [firstPage]);

    renderWithProviders(<PublicInstitutionsPage />);

    await screen.findByText('Acuario Nacional');

    const institutionCalls = fetchMock.mock.calls
      .map((call) => call as [string, RequestInit])
      .filter(([url]) => url.includes('/api/institutions'));

    expect(institutionCalls.length).toBeGreaterThan(0);
    for (const [, init] of institutionCalls) {
      const headers = (init.headers ?? {}) as Record<string, string>;
      const authHeader = Object.entries(headers).find(
        ([key]) => key.toLowerCase() === 'authorization',
      );
      expect(authHeader).toBeUndefined();
    }
  });

  it('uses an Authorization header when a session is present', async () => {
    setupFetchSequence(fetchMock, [firstPage]);

    const authenticatedState = {
      accessToken: 'jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { id: '1', username: 'admin', email: 'admin@example.local', role: 'Admin' as const },
    };

    renderWithProviders(<PublicInstitutionsPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    await screen.findByText('Acuario Nacional');

    const institutionCalls = fetchMock.mock.calls
      .map((call) => call as [string, RequestInit])
      .filter(([url]) => url.includes('/api/institutions'));

    expect(institutionCalls.length).toBeGreaterThan(0);
    const headers = institutionCalls[0][1].headers as Record<string, string>;
    expect(headers.Authorization).toBe('Bearer jwt');
  });

  it('renders the empty state when the API returns no institutions', async () => {
    const emptyPage: PaginatedResponse<Institution> = {
      items: [],
      page: 1,
      pageSize: 20,
      totalItems: 0,
      totalPages: 0,
    };
    setupFetchSequence(fetchMock, [emptyPage]);

    renderWithProviders(<PublicInstitutionsPage />);

    expect(
      await screen.findByText(/no se encontraron instituciones/i),
    ).toBeInTheDocument();
  });

  it('renders the error state when the request fails', async () => {
    fetchMock.mockImplementation((input: RequestInfo | URL) => {
      const url = typeof input === 'string' ? input : input.toString();
      if (url.includes('/api/institutions/filter-options')) {
        return Promise.resolve(
          jsonResponse({
            categories: ['Cat'],
            statePowers: ['Poder Ejecutivo'],
            sectors: ['Sector'],
          }),
        );
      }
      return Promise.reject(new TypeError('Failed to fetch'));
    });

    renderWithProviders(<PublicInstitutionsPage />);

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent(/no está disponible|inténtalo/i);
  });

  it('does not expose edit/delete modals even after interacting with rows', async () => {
    setupFetchSequence(fetchMock, [firstPage]);

    const user = userEvent.setup();
    renderWithProviders(<PublicInstitutionsPage />);

    await screen.findByText('Acuario Nacional');

    await user.click(screen.getByText('Acuario Nacional'));

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
});
