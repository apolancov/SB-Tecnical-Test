import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { NewInstitutionPage } from '../../views/NewInstitutionPage';
import {
  resetNavigationMocks,
} from '../nextNavigationState';
import { renderWithProviders, buildTestServices } from '../testUtils';
import type { Institution } from '../../types/institution';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

const filterOptions = {
  categories: ['Ministerio'],
  statePowers: ['Poder Ejecutivo'],
  sectors: ['Hacienda'],
};

function mockFilterOptions(fetchMock: ReturnType<typeof vi.fn>) {
  fetchMock.mockImplementation((input: RequestInfo | URL) => {
    const url = typeof input === 'string' ? input : input.toString();
    if (url.includes('/api/institutions/filter-options')) {
      return Promise.resolve(jsonResponse(filterOptions));
    }
    return Promise.reject(new TypeError(`Unexpected fetch ${url}`));
  });
}

const authenticatedState = {
  accessToken: 'jwt',
  expiresAt: new Date(Date.now() + 60_000).toISOString(),
  user: { id: '1', username: 'admin', email: 'admin@example.local', role: 'Admin' as const },
};

const createdInstitution: Institution = {
  id: 'new-id',
  name: 'New Institution',
  category: 'Ministerio',
  statePower: 'Poder Ejecutivo',
  sector: 'Hacienda',
};

describe('NewInstitutionPage', () => {
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

  it('submits a valid form, calls POST /api/institutions and navigates back to the list', async () => {
    fetchMock.mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.toString();
      if (url.includes('/api/institutions/filter-options')) {
        return Promise.resolve(jsonResponse(filterOptions));
      }
      if (init?.method === 'POST' && url.includes('/api/institutions')) {
        return Promise.resolve(jsonResponse(createdInstitution, 201));
      }
      return Promise.reject(new TypeError(`Unexpected fetch ${url}`));
    });

    const user = userEvent.setup();
    renderWithProviders(<NewInstitutionPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    await user.type(screen.getByLabelText(/nombre/i), 'New Institution');
    await user.type(screen.getByLabelText(/categor/i), 'Ministerio');
    await user.type(screen.getByLabelText(/poder/i), 'Poder Ejecutivo');
    await user.type(screen.getByLabelText(/sector/i), 'Hacienda');

    await user.click(screen.getByRole('button', { name: /crear institución/i }));

    await waitFor(() => {
      const calls = fetchMock.mock.calls.filter(
        (call) => (call[1] as RequestInit)?.method === 'POST',
      );
      expect(calls.length).toBeGreaterThan(0);
    });

    const postCall = fetchMock.mock.calls.find(
      (call) => (call[1] as RequestInit)?.method === 'POST',
    );
    expect(postCall).toBeDefined();
    const [url, init] = postCall as [string, RequestInit];
    expect(url).toContain('/api/institutions');
    expect(url).not.toContain('filter-options');
    expect(JSON.parse(init.body as string)).toEqual({
      name: 'New Institution',
      category: 'Ministerio',
      statePower: 'Poder Ejecutivo',
      sector: 'Hacienda',
    });
  });

  it('shows the backend validation message when the create call returns 400', async () => {
    fetchMock.mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.toString();
      if (url.includes('/api/institutions/filter-options')) {
        return Promise.resolve(jsonResponse(filterOptions));
      }
      if (init?.method === 'POST' && url.includes('/api/institutions')) {
        return Promise.resolve(
          jsonResponse(
            {
              type: 'institutions.create.name.required',
              title: 'Invalid institution payload.',
              detail: 'El nombre es obligatorio.',
              status: 400,
            },
            400,
          ),
        );
      }
      return Promise.reject(new TypeError(`Unexpected fetch ${url}`));
    });

    const user = userEvent.setup();
    renderWithProviders(<NewInstitutionPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    await user.type(screen.getByLabelText(/nombre/i), 'Anything');
    await user.type(screen.getByLabelText(/categor/i), 'Cat');
    await user.type(screen.getByLabelText(/poder/i), 'Power');
    await user.type(screen.getByLabelText(/sector/i), 'Sector');

    await user.click(screen.getByRole('button', { name: /crear institución/i }));

    await waitFor(() => {
      expect(
        screen.getByTestId('new-institution-form-name-error'),
      ).toHaveTextContent('El nombre es obligatorio.');
    });
  });

  it('requests the filter options endpoint so the autocomplete dropdowns are populated', async () => {
    mockFilterOptions(fetchMock);

    renderWithProviders(<NewInstitutionPage />, {
      services: buildTestServices(authenticatedState),
      initialAuthentication: authenticatedState,
    });

    await waitFor(() => {
      const calls = fetchMock.mock.calls.filter(
        (call) =>
          typeof call[0] === 'string' &&
          (call[0] as string).includes('/api/institutions/filter-options'),
      );
      expect(calls.length).toBeGreaterThan(0);
    });
  });
});