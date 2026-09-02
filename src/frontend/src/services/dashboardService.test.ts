import { describe, expect, it, beforeEach, vi } from 'vitest';
import { ApiClient } from './api';
import { DefaultDashboardService } from './dashboardService';
import { EmptyDashboardSummary } from '../types/dashboard';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

describe('dashboardService', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  function teardown() {
    globalThis.fetch = originalFetch;
  }

  it('hits /api/dashboard/resumen and returns the payload', async () => {
    const payload = {
      ...EmptyDashboardSummary,
      totalRequests: 10,
      overdueRequests: 2,
    };
    fetchMock.mockResolvedValue(jsonResponse(payload));

    const client = new ApiClient(() => null, () => undefined);
    const service = new DefaultDashboardService(client);
    const result = await service.getSummary();

    expect(result.totalRequests).toBe(10);
    expect(result.overdueRequests).toBe(2);
    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/dashboard/resumen');
    teardown();
  });

  it('propagates network failures', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    const client = new ApiClient(() => null, () => undefined);
    const service = new DefaultDashboardService(client);
    await expect(service.getSummary()).rejects.toMatchObject({ kind: 'Network' });
    teardown();
  });
});