import { describe, expect, it, vi, beforeEach } from 'vitest';
import { act, renderHook, waitFor } from '@testing-library/react';
import { useAuditLog } from './useAuditLog';
import {
  AuditAction,
  AuditLogSortDirection,
  AuditLogSortField,
  AuditOutcome,
  type AuditLogEntry,
} from '../types/audit';
import type { PaginatedResponse } from '../types/pagination';
import type { DefaultAuditLogService } from '../services/auditLogService';

const sampleEntry: AuditLogEntry = {
  id: '11111111-1111-1111-1111-111111111111',
  timestamp: '2026-09-02T10:00:00.000Z',
  action: AuditAction.LoginSucceeded,
  outcome: AuditOutcome.Success,
  entityType: 'User',
  entityId: 'user-1',
  details: 'Login OK',
  ipAddress: '127.0.0.1',
  actorUserId: 'user-1',
  actorUserName: 'admin',
};

describe('useAuditLog', () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
  });

  function buildService(): DefaultAuditLogService {
    return {
      search: fetchMock as unknown as DefaultAuditLogService['search'],
      getById: vi.fn() as unknown as DefaultAuditLogService['getById'],
    } as unknown as DefaultAuditLogService;
  }

  it('fetches the first page on mount with default filters', async () => {
    const payload: PaginatedResponse<AuditLogEntry> = {
      items: [sampleEntry],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
    };
    fetchMock.mockResolvedValue(payload);

    const service = buildService();
    const { result } = renderHook(() => useAuditLog({ service }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.entries.length).toBe(1);
    expect(result.current.entries[0]?.action).toBe(AuditAction.LoginSucceeded);
    expect(result.current.totalItems).toBe(1);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('passes the current filter and sort values to the service', async () => {
    fetchMock.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 20,
      totalItems: 0,
      totalPages: 0,
    });

    const service = buildService();
    const { result } = renderHook(() =>
      useAuditLog({
        service,
        initialFilters: {
          action: AuditAction.LoginFailed,
          outcome: AuditOutcome.Failure,
          search: 'admin',
          entityType: 'User',
        },
        initialSortField: AuditLogSortField.Action,
        initialSortDirection: AuditLogSortDirection.Ascending,
      }),
    );

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    const query = fetchMock.mock.calls[0]?.[0];
    expect(query.filters.action).toBe(AuditAction.LoginFailed);
    expect(query.filters.outcome).toBe(AuditOutcome.Failure);
    expect(query.filters.search).toBe('admin');
    expect(query.filters.entityType).toBe('User');
    expect(query.sortField).toBe(AuditLogSortField.Action);
    expect(query.sortDirection).toBe(AuditLogSortDirection.Ascending);
  });

  it('captures thrown errors', async () => {
    fetchMock.mockRejectedValue(new Error('boom'));

    const service = buildService();
    const { result } = renderHook(() => useAuditLog({ service }));

    await waitFor(() => {
      expect(result.current.error).not.toBeNull();
    });

    expect(result.current.entries).toEqual([]);
  });

  it('refetches when refresh is called', async () => {
    fetchMock.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 20,
      totalItems: 0,
      totalPages: 0,
    });

    const service = buildService();
    const { result } = renderHook(() => useAuditLog({ service }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(fetchMock).toHaveBeenCalledTimes(1);

    await act(async () => {
      result.current.refresh();
    });

    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThanOrEqual(2);
    });
  });

  it('normalises filters by trimming whitespace', async () => {
    fetchMock.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 20,
      totalItems: 0,
      totalPages: 0,
    });

    const service = buildService();
    const { result } = renderHook(() =>
      useAuditLog({
        service,
        initialFilters: {
          actorUserId: '  22222222-2222-2222-2222-222222222222  ',
          entityType: '  Request  ',
          search: '   ',
        },
      }),
    );

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.filters.actorUserId).toBe(
      '22222222-2222-2222-2222-222222222222',
    );
    expect(result.current.filters.entityType).toBe('Request');
    expect(result.current.filters.search).toBe('');
  });

  it('resets the filters back to the initial values when resetFilters is called', async () => {
    fetchMock.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 20,
      totalItems: 0,
      totalPages: 0,
    });

    const service = buildService();
    const { result } = renderHook(() =>
      useAuditLog({
        service,
        initialFilters: {
          action: AuditAction.LoginFailed,
        },
      }),
    );

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      result.current.setFilters({
        actorUserId: '',
        action: AuditAction.InstitutionDeleted,
        outcome: '',
        entityType: '',
        fromDate: '',
        toDate: '',
        search: '',
      });
    });

    expect(result.current.filters.action).toBe(AuditAction.InstitutionDeleted);

    await act(async () => {
      result.current.resetFilters();
    });

    await waitFor(() => {
      expect(result.current.filters.action).toBe(AuditAction.LoginFailed);
    });
  });
});