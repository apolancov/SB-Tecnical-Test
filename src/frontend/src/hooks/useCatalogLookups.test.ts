import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import {
  useCatalogAreas,
  useCatalogRequestTypes,
  useCatalogStaff,
} from '../hooks/useCatalogLookups';
import type { DefaultCatalogService } from '../services/catalogService';

describe('useCatalogAreas / useCatalogRequestTypes / useCatalogStaff', () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
  });

  function buildService(): DefaultCatalogService {
    return {
      listAreas: fetchMock,
      listRequestTypes: fetchMock,
      listStaffCandidates: fetchMock,
    } as unknown as DefaultCatalogService;
  }

  it('loads active areas', async () => {
    const payload = [{ id: 'a1', name: 'Atención al Ciudadano' }];
    fetchMock.mockResolvedValue(payload);

    const service = buildService();
    const { result } = renderHook(() => useCatalogAreas({ service }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.areas).toEqual(payload);
  });

  it('loads active request types', async () => {
    const payload = [
      { id: 't1', name: 'Incidente', description: 'Reporte' },
    ];
    fetchMock.mockResolvedValue(payload);

    const service = buildService();
    const { result } = renderHook(() => useCatalogRequestTypes({ service }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.requestTypes).toEqual(payload);
  });

  it('loads staff candidates when enabled', async () => {
    const payload = [
      { id: 'admin-1', username: 'admin', email: 'admin@example.local', role: 'Admin' },
    ];
    fetchMock.mockResolvedValue(payload);

    const service = buildService();
    const { result } = renderHook(() => useCatalogStaff({ service, enabled: true }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.staff).toEqual(payload);
    expect(result.current.error).toBeNull();
  });

  it('does not fetch staff candidates when disabled', async () => {
    const service = buildService();
    const { result } = renderHook(() => useCatalogStaff({ service, enabled: false }));

    expect(fetchMock).not.toHaveBeenCalled();
    expect(result.current.staff).toEqual([]);
  });

  it('captures thrown errors', async () => {
    fetchMock.mockRejectedValue(new Error('boom'));
    const service = buildService();
    const { result } = renderHook(() => useCatalogAreas({ service }));

    await waitFor(() => {
      expect(result.current.error).not.toBeNull();
    });
    expect(result.current.error).toMatch(/no se pudieron cargar las áreas/i);
  });
});