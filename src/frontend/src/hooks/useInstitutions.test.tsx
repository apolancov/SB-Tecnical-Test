import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { act } from 'react';
import { useInstitutions } from './useInstitutions';
import { ApiClient } from '../services/api';
import { DefaultInstitutionService } from '../services/institutionService';
import type { PaginatedResponse, Institution, InstitutionFilters } from '../types/institution';

function makeService(fetchMock: ReturnType<typeof vi.fn>) {
  const client = new ApiClient(() => null, () => undefined);
  void fetchMock;
  return new DefaultInstitutionService(client);
}

function makePayload(totalItems: number, pageSize: number): PaginatedResponse<Institution> {
  const itemCount = Math.min(totalItems, pageSize);
  return {
    items: Array.from({ length: itemCount }, (_, index) => ({
      id: `id-${index}`,
      name: `Institution ${index}`,
      category: 'Cat',
      statePower: 'Poder Ejecutivo',
      sector: 'Sector',
    })),
    page: 1,
    pageSize,
    totalItems,
    totalPages: Math.ceil(totalItems / pageSize),
  };
}

describe('useInstitutions', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  function jsonResponse(body: unknown): Response {
    return new Response(JSON.stringify(body), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  it('exposes the loading state while fetching the first page', async () => {
    let resolveResponse: ((value: Response) => void) | null = null;
    fetchMock.mockImplementation(
      () =>
        new Promise<Response>((resolve) => {
          resolveResponse = resolve;
        }),
    );

    const service = makeService(fetchMock);
    const { result } = renderHook(() => useInstitutions({ service }));

    expect(result.current.loading).toBe(true);
    expect(result.current.institutions).toHaveLength(0);

    await act(async () => {
      resolveResponse?.(jsonResponse(makePayload(40, 20)));
    });

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });
    expect(result.current.institutions).toHaveLength(20);
    expect(result.current.totalItems).toBe(40);
    expect(result.current.totalPages).toBe(2);
  });

  it('reports an empty state when the API returns no items', async () => {
    fetchMock.mockResolvedValue(jsonResponse(makePayload(0, 20)));

    const service = makeService(fetchMock);
    const { result } = renderHook(() => useInstitutions({ service }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });
    expect(result.current.institutions).toHaveLength(0);
    expect(result.current.totalItems).toBe(0);
    expect(result.current.totalPages).toBe(0);
  });

  it('exposes a user-friendly error when the request fails', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));

    const service = makeService(fetchMock);
    const { result } = renderHook(() => useInstitutions({ service }));

    await waitFor(() => {
      expect(result.current.error).not.toBeNull();
    });
    expect(result.current.error).toMatch(/no está disponible/i);
    expect(result.current.institutions).toHaveLength(0);
  });

  it('re-fetches when the page changes', async () => {
    fetchMock.mockResolvedValue(jsonResponse(makePayload(40, 20)));

    const service = makeService(fetchMock);
    const { result } = renderHook(() => useInstitutions({ service }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(fetchMock).toHaveBeenCalledTimes(1);

    await act(async () => {
      result.current.setPage(2);
    });

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(2);
    });
    const [secondUrl] = fetchMock.mock.calls[1] as [string, RequestInit];
    expect(secondUrl).toContain('page=2');
  });

  it('issues a request with the expected filter parameters', async () => {
    fetchMock.mockResolvedValue(jsonResponse(makePayload(0, 20)));

    const service = makeService(fetchMock);
    const { result } = renderHook(() => useInstitutions({ service }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    const filters: InstitutionFilters = {
      name: 'Salud',
      category: 'Ministerio',
      statePower: 'Poder Ejecutivo',
      sector: 'Salud',
    };

    await act(async () => {
      result.current.setFilters(filters);
    });

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(2);
    });

    const [secondUrl] = fetchMock.mock.calls[1] as [string, RequestInit];
    expect(secondUrl).toContain('name=Salud');
    expect(secondUrl).toContain('category=Ministerio');
    expect(secondUrl).toContain('statePower=Poder+Ejecutivo');
    expect(secondUrl).toContain('sector=Salud');
  });

  it('resets the page to 1 when filters are updated', async () => {
    fetchMock.mockResolvedValue(jsonResponse(makePayload(40, 20)));

    const service = makeService(fetchMock);
    const { result } = renderHook(() => useInstitutions({ service }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      result.current.setPage(3);
    });
    await waitFor(() => expect(result.current.page).toBe(3));

    await act(async () => {
      result.current.setFilters({ ...result.current.filters, name: 'Salud' });
    });

    await waitFor(() => {
      expect(result.current.page).toBe(1);
    });
  });

  it('ignores invalid page transitions (page < 1)', async () => {
    fetchMock.mockResolvedValue(jsonResponse(makePayload(40, 20)));

    const service = makeService(fetchMock);
    const { result } = renderHook(() => useInstitutions({ service }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      result.current.setPage(0);
      result.current.setPage(-1);
    });

    expect(result.current.page).toBe(1);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('refresh() triggers another fetch', async () => {
    fetchMock.mockResolvedValue(jsonResponse(makePayload(40, 20)));

    const service = makeService(fetchMock);
    const { result } = renderHook(() => useInstitutions({ service }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      result.current.refresh();
    });

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(2);
    });
  });

  it('has a usable user-event harness', () => {
    const user = userEvent.setup();
    expect(typeof user.click).toBe('function');
  });
});
