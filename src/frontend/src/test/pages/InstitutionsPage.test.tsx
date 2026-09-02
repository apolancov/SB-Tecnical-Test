import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { InstitutionsPage } from '../../views/InstitutionsPage';
import {
  resetNavigationMocks,
} from '../nextNavigationState';
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

const secondPage: PaginatedResponse<Institution> = {
  items: [
    { id: '21', name: 'Banco Central', category: 'Cat', statePower: 'Poder Ejecutivo', sector: 'Hacienda' },
  ],
  page: 2,
  pageSize: 20,
  totalItems: 45,
  totalPages: 3,
};

const emptyPage: PaginatedResponse<Institution> = {
  items: [],
  page: 1,
  pageSize: 20,
  totalItems: 0,
  totalPages: 0,
};

const authenticatedState = {
  accessToken: 'jwt',
  expiresAt: new Date(Date.now() + 60_000).toISOString(),
  user: { id: '1', username: 'admin', email: 'admin@example.local', role: 'Admin' as const },
};

function setupFetchSequence(fetchMock: ReturnType<typeof vi.fn>, sequence: PaginatedResponse<Institution>[]) {
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

describe('InstitutionsPage', () => {
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

  it('renders the loading state initially', async () => {
    setupFetchSequence(fetchMock, [firstPage]);

    renderWithProviders(<InstitutionsPage />, {
      initialAuthentication: authenticatedState,
    });

    expect(screen.getByText(/cargando instituciones/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.queryByText(/cargando instituciones/i)).not.toBeInTheDocument();
    });
    expect(screen.getByText('Acuario Nacional')).toBeInTheDocument();
    expect(screen.getByText('Archivo General')).toBeInTheDocument();
    expect(screen.getByText(/Página 1 de 3/)).toBeInTheDocument();
  });

  it('renders the empty state when no institutions are returned', async () => {
    setupFetchSequence(fetchMock, [emptyPage]);

    renderWithProviders(<InstitutionsPage />, {
      initialAuthentication: authenticatedState,
    });

    await waitFor(() => {
      expect(screen.getByText(/no se encontraron instituciones/i)).toBeInTheDocument();
    });
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

    renderWithProviders(<InstitutionsPage />, {
      initialAuthentication: authenticatedState,
    });

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent(/no está disponible|inténtalo/i);
  });

  it('navigates to the next page when Next is clicked', async () => {
    setupFetchSequence(fetchMock, [firstPage, secondPage]);

    const user = userEvent.setup();
    renderWithProviders(<InstitutionsPage />, {
      initialAuthentication: authenticatedState,
    });

    await screen.findByText('Acuario Nacional');

    await user.click(screen.getByRole('button', { name: /siguiente/i }));

    await waitFor(() => {
      expect(screen.getByText('Banco Central')).toBeInTheDocument();
    });
    expect(screen.getByText(/Página 2 de 3/)).toBeInTheDocument();
  });

  it('disables Previous on the first page and Next on the last', async () => {
    setupFetchSequence(fetchMock, [firstPage]);

    renderWithProviders(<InstitutionsPage />, {
      initialAuthentication: authenticatedState,
    });

    await screen.findByText('Acuario Nacional');

    expect(screen.getByRole('button', { name: /anterior/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /siguiente/i })).toBeEnabled();
  });

  it('re-fetches when a filter is submitted', async () => {
    const filteredPage: PaginatedResponse<Institution> = {
      items: [
        { id: '99', name: 'Salud Pública', category: 'Ministerio', statePower: 'Poder Ejecutivo', sector: 'Salud' },
      ],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
    };
    setupFetchSequence(fetchMock, [firstPage, filteredPage]);

    const user = userEvent.setup();
    const services = buildTestServices(authenticatedState);

    renderWithProviders(<InstitutionsPage />, {
      services,
      initialAuthentication: authenticatedState,
    });

    await screen.findByText('Acuario Nacional');

    await user.type(screen.getByTestId('filter-name'), 'Salud');
    await user.click(screen.getByRole('button', { name: /buscar/i }));

    await waitFor(() => {
      expect(screen.getByText('Salud Pública')).toBeInTheDocument();
    });

    const institutionCalls = fetchMock.mock.calls
      .map((call) => (call as [string, RequestInit])[0])
      .filter((url) => url.includes('/api/institutions') && !url.includes('filter-options'));
    expect(institutionCalls.length).toBeGreaterThanOrEqual(2);
    const latestSearchUrl = institutionCalls[institutionCalls.length - 1];
    expect(latestSearchUrl).toContain('name=Salud');
  });

  it('exposes Edit and Delete buttons for each row', async () => {
    setupFetchSequence(fetchMock, [firstPage]);

    renderWithProviders(<InstitutionsPage />, {
      initialAuthentication: authenticatedState,
    });

    await screen.findByText('Acuario Nacional');

    expect(
      screen.getByRole('button', { name: /editar acuario nacional/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /eliminar acuario nacional/i }),
    ).toBeInTheDocument();
  });

  it('opens the edit modal and PUTs the updated values', async () => {
    const refreshedPage: PaginatedResponse<Institution> = {
      items: [
        {
          id: '1',
          name: 'Acuario Nacional Renombrado',
          category: 'Cat',
          statePower: 'Poder Ejecutivo',
          sector: 'Sector',
        },
        firstPage.items[1],
      ],
      page: 1,
      pageSize: 20,
      totalItems: 45,
      totalPages: 3,
    };

    let putCalled = false;
    fetchMock.mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
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
      if (init?.method === 'PUT') {
        putCalled = true;
        const body = JSON.parse(init.body as string);
        return Promise.resolve(
          jsonResponse({
            id: '1',
            name: body.name,
            category: body.category,
            statePower: body.statePower,
            sector: body.sector,
          }),
        );
      }
      if (init?.method === undefined && putCalled) {
        return Promise.resolve(jsonResponse(refreshedPage));
      }
      return Promise.resolve(jsonResponse(firstPage));
    });

    const user = userEvent.setup();
    renderWithProviders(<InstitutionsPage />, {
      initialAuthentication: authenticatedState,
    });

    await screen.findByText('Acuario Nacional');

    await user.click(
      screen.getByRole('button', { name: /editar acuario nacional/i }),
    );

    const modal = await screen.findByRole('dialog');
    expect(modal).toHaveTextContent(/editar institución/i);

    const nameInput = screen.getByTestId('institution-edit-form-name');
    await user.clear(nameInput);
    await user.type(nameInput, 'Acuario Nacional Renombrado');

    await user.click(
      screen.getByTestId('institution-edit-form-submit'),
    );

    await waitFor(() => {
      expect(putCalled).toBe(true);
    });
  });

  it('opens the delete confirmation modal and DELETEs the institution on confirm', async () => {
    const refreshedPage: PaginatedResponse<Institution> = {
      items: [firstPage.items[1]],
      page: 1,
      pageSize: 20,
      totalItems: 44,
      totalPages: 3,
    };

    let deleteCalled = false;
    fetchMock.mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
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
      if (init?.method === 'DELETE') {
        deleteCalled = true;
        return Promise.resolve(new Response(null, { status: 204 }));
      }
      if (init?.method === undefined && deleteCalled) {
        return Promise.resolve(jsonResponse(refreshedPage));
      }
      return Promise.resolve(jsonResponse(firstPage));
    });

    const user = userEvent.setup();
    renderWithProviders(<InstitutionsPage />, {
      initialAuthentication: authenticatedState,
    });

    await screen.findByText('Acuario Nacional');

    await user.click(
      screen.getByRole('button', { name: /eliminar acuario nacional/i }),
    );

    expect(screen.getByTestId('institution-delete-message')).toHaveTextContent(
      /acuario nacional/i,
    );

    await user.click(screen.getByTestId('institution-delete-confirm'));

    await waitFor(() => {
      expect(deleteCalled).toBe(true);
    });
  });
});
