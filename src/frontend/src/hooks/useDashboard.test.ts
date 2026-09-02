import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { useDashboard } from '../hooks/useDashboard';
import { EmptyDashboardSummary } from '../types/dashboard';
import type { DefaultDashboardService } from '../services/dashboardService';

describe('useDashboard', () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
  });

  function buildService(): DefaultDashboardService {
    return { getSummary: fetchMock } as unknown as DefaultDashboardService;
  }

  it('fetches the dashboard summary on mount', async () => {
    const payload = {
      ...EmptyDashboardSummary,
      totalRequests: 7,
      overdueRequests: 1,
    };
    fetchMock.mockResolvedValue(payload);

    const service = buildService();
    const { result } = renderHook(() => useDashboard({ service }));

    await waitFor(() => {
      expect(result.current.summary.totalRequests).toBe(7);
    });
    expect(result.current.summary.overdueRequests).toBe(1);
    expect(result.current.loading).toBe(false);
  });

  it('captures thrown errors', async () => {
    fetchMock.mockRejectedValue(new Error('boom'));
    const service = buildService();
    const { result } = renderHook(() => useDashboard({ service }));

    await waitFor(() => {
      expect(result.current.error).not.toBeNull();
    });
    expect(result.current.error).toMatch(/inténtalo de nuevo/i);
  });
});