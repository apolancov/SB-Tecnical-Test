import { describe, expect, it, vi, beforeEach } from 'vitest';
import { act, renderHook, waitFor } from '@testing-library/react';
import { useRequests } from '../hooks/useRequests';
import {
  RequestSortDirection,
  RequestSortField,
  RequestStatus,
  type RequestRecord,
} from '../types/request';
import type { PaginatedResponse } from '../types/pagination';
import type { DefaultRequestService } from '../services/requestService';

const sample: RequestRecord = {
  id: '11111111-1111-1111-1111-111111111111',
  code: 'SOL-2026-0001',
  title: 'Reparación',
  description: 'Detalle',
  status: RequestStatus.Submitted,
  priority: 'Medium',
  createdAt: '2026-09-01T10:00:00.000Z',
  dueDate: null,
  evidenceUrl: null,
  closedAt: null,
  areaId: '22222222-2222-2222-2222-222222222222',
  area: 'Mantenimiento',
  requestTypeId: '33333333-3333-3333-3333-333333333333',
  requestType: 'Incidente',
  requesterId: '44444444-4444-4444-4444-444444444444',
  requesterUsername: 'juan',
  requesterEmail: 'juan@example.local',
  responsibleId: null,
  responsibleUsername: null,
  responsibleEmail: null,
};

describe('useRequests', () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
  });

  function buildService(): DefaultRequestService {
    const stub = {
      search: fetchMock as unknown as DefaultRequestService['search'],
    } as unknown as DefaultRequestService;
    return stub;
  }

  it('fetches the first page on mount', async () => {
    const payload: PaginatedResponse<RequestRecord> = {
      items: [sample],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
    };
    fetchMock.mockResolvedValue(payload);

    const service = buildService();
    const { result } = renderHook(() => useRequests({ service }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.requests.length).toBe(1);
    expect(result.current.requests[0]?.code).toBe('SOL-2026-0001');
    expect(result.current.totalItems).toBe(1);
  });

  it('resets the page when the search filter changes', async () => {
    const page1: PaginatedResponse<RequestRecord> = {
      items: [sample],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 2,
    };
    const page2: PaginatedResponse<RequestRecord> = {
      items: [],
      page: 2,
      pageSize: 20,
      totalItems: 1,
      totalPages: 2,
    };
    fetchMock.mockResolvedValueOnce(page1);
    fetchMock.mockResolvedValueOnce(page2);
    fetchMock.mockResolvedValue(page2);

    const service = buildService();
    const { result } = renderHook(() => useRequests({ service }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    act(() => {
      result.current.setPage(2);
    });

    await waitFor(() => {
      expect(result.current.page).toBe(2);
    });

    act(() => {
      result.current.setSearch('urgente');
    });

    expect(result.current.filters.search).toBe('urgente');
  });

  it('captures thrown errors', async () => {
    fetchMock.mockRejectedValue(new Error('boom'));

    const service = buildService();
    const { result } = renderHook(() => useRequests({ service }));

    await waitFor(() => {
      expect(result.current.error).not.toBeNull();
    });
    expect(result.current.error).toMatch(/inténtalo/i);
  });

  it('sorts by createdAt descending by default', async () => {
    const payload: PaginatedResponse<RequestRecord> = {
      items: [],
      page: 1,
      pageSize: 20,
      totalItems: 0,
      totalPages: 0,
    };
    fetchMock.mockResolvedValue(payload);

    const service = buildService();
    const { result } = renderHook(() => useRequests({ service }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.sortField).toBe(RequestSortField.CreatedAt);
    expect(result.current.sortDirection).toBe(RequestSortDirection.Descending);
  });
});