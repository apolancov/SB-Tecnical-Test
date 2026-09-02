import { describe, expect, it, beforeEach, vi } from 'vitest';
import { ApiClient } from './api';
import { DefaultCatalogService } from './catalogService';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

describe('catalogService', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  function teardown() {
    globalThis.fetch = originalFetch;
  }

  it('listAreas hits /api/catalogos/areas and returns the payload', async () => {
    const payload = [
      { id: 'area-1', name: 'Atención al Ciudadano' },
      { id: 'area-2', name: 'Soporte Técnico' },
    ];
    fetchMock.mockResolvedValue(jsonResponse(payload));

    const client = new ApiClient(() => null, () => undefined);
    const service = new DefaultCatalogService(client);
    const result = await service.listAreas();

    expect(result).toEqual(payload);
    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/catalogos/areas');
    teardown();
  });

  it('listRequestTypes hits /api/catalogos/tipos-solicitud and returns the payload', async () => {
    const payload = [
      { id: 'type-1', name: 'Incidente', description: 'Reporte' },
    ];
    fetchMock.mockResolvedValue(jsonResponse(payload));

    const client = new ApiClient(() => null, () => undefined);
    const service = new DefaultCatalogService(client);
    const result = await service.listRequestTypes();

    expect(result).toEqual(payload);
    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/catalogos/tipos-solicitud');
    teardown();
  });

  it('listStaffCandidates hits /api/catalogos/responsables and returns the payload', async () => {
    const payload = [
      { id: 'admin-1', username: 'admin', email: 'admin@example.local', role: 'Admin' },
      { id: 'ana-1', username: 'ana', email: 'ana@example.local', role: 'Analista' },
    ];
    fetchMock.mockResolvedValue(jsonResponse(payload));

    const client = new ApiClient(() => null, () => undefined);
    const service = new DefaultCatalogService(client);
    const result = await service.listStaffCandidates();

    expect(result).toEqual(payload);
    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/catalogos/responsables');
    teardown();
  });

  it('propagates network failures', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    const client = new ApiClient(() => null, () => undefined);
    const service = new DefaultCatalogService(client);
    await expect(service.listAreas()).rejects.toMatchObject({ kind: 'Network' });
    await expect(service.listRequestTypes()).rejects.toMatchObject({ kind: 'Network' });
    teardown();
  });
});