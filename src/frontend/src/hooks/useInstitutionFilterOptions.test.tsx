import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, renderHook, waitFor } from '@testing-library/react';
import { ApiClient } from '../services/api';
import { DefaultInstitutionService } from '../services/institutionService';
import { useInstitutionFilterOptions } from './useInstitutionFilterOptions';
import type { InstitutionFilterOptions } from '../types/institution';

function makeService(fetchMock: ReturnType<typeof vi.fn>) {
  const client = new ApiClient(() => null, () => undefined);
  void fetchMock;
  return new DefaultInstitutionService(client);
}

function jsonResponse(body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  });
}

const sampleOptions: InstitutionFilterOptions = {
  categories: ['Ministerio', 'Universidad'],
  statePowers: ['Poder Ejecutivo'],
  sectors: ['Cultura', 'Salud'],
};

describe('useInstitutionFilterOptions', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it('exposes loading state until the options are loaded', async () => {
    let resolveResponse: ((value: Response) => void) | null = null;
    fetchMock.mockImplementation(
      () =>
        new Promise<Response>((resolve) => {
          resolveResponse = resolve;
        }),
    );

    const service = makeService(fetchMock);
    const { result } = renderHook(() => useInstitutionFilterOptions({ service }));

    expect(result.current.loading).toBe(true);
    expect(result.current.options.categories).toHaveLength(0);

    (resolveResponse as unknown as (value: Response) => void)(jsonResponse(sampleOptions));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });
    expect(result.current.options).toEqual(sampleOptions);
    expect(result.current.error).toBeNull();
  });

  it('exposes a user-friendly error when the request fails', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));

    const service = makeService(fetchMock);
    const { result } = renderHook(() => useInstitutionFilterOptions({ service }));

    await waitFor(() => {
      expect(result.current.error).not.toBeNull();
    });
    expect(result.current.error).toMatch(/no se pudieron cargar/i);
    expect(result.current.loading).toBe(false);
  });

  it('refresh() triggers another fetch', async () => {
    fetchMock.mockResolvedValue(jsonResponse(sampleOptions));

    const service = makeService(fetchMock);
    const { result } = renderHook(() => useInstitutionFilterOptions({ service }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });
    expect(fetchMock).toHaveBeenCalledTimes(1);

    await act(async () => {
      result.current.refresh();
    });

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(2);
    });
  });
});
