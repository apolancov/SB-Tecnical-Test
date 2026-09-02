import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiClient } from './api';
import { DefaultAuditLogService } from './auditLogService';
import {
  AuditAction,
  AuditLogSortDirection,
  AuditLogSortField,
  AuditOutcome,
  EmptyAuditLogFilters,
  type AuditLogEntry,
  type AuditLogQuery,
} from '../types/audit';
import type { PaginatedResponse } from '../types/pagination';

function buildService(fetchMock: ReturnType<typeof vi.fn>): DefaultAuditLogService {
  const client = new ApiClient(() => null, () => undefined);
  void fetchMock;
  return new DefaultAuditLogService(client);
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

describe('auditLogService.search', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  function buildQuery(
    overrides: Partial<AuditLogQuery> = {},
  ): AuditLogQuery {
    return {
      page: 1,
      pageSize: 20,
      filters: EmptyAuditLogFilters,
      sortField: AuditLogSortField.Timestamp,
      sortDirection: AuditLogSortDirection.Descending,
      ...overrides,
    };
  }

  it('hits /api/auditoria with page, pageSize and the supported filters', async () => {
    const payload: PaginatedResponse<AuditLogEntry> = {
      items: [],
      page: 1,
      pageSize: 20,
      totalItems: 0,
      totalPages: 0,
    };
    fetchMock.mockResolvedValue(jsonResponse(payload));

    const service = buildService(fetchMock);
    await service.search(
      buildQuery({
        filters: {
          actorUserId: ' 11111111-1111-1111-1111-111111111111 ',
          action: AuditAction.LoginFailed,
          outcome: AuditOutcome.Failure,
          entityType: 'User',
          fromDate: '2026-09-01',
          toDate: '2026-09-02',
          search: 'login',
        },
        sortField: AuditLogSortField.Action,
        sortDirection: AuditLogSortDirection.Ascending,
      }),
    );

    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/auditoria');
    expect(url).toContain('page=1');
    expect(url).toContain('pageSize=20');
    expect(url).toContain('actorUserId=11111111-1111-1111-1111-111111111111');
    expect(url).toContain('action=LoginFailed');
    expect(url).toContain('outcome=Failure');
    expect(url).toContain('entityType=User');
    expect(url).toContain('fromDate=2026-09-01');
    expect(url).toContain('toDate=2026-09-02');
    expect(url).toContain('search=login');
    expect(url).toContain('sortBy=Action');
    expect(url).toContain('sortDirection=Ascending');
  });

  it('omits empty or whitespace filters from the query string', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 }),
    );

    const service = buildService(fetchMock);
    await service.search(
      buildQuery({
        filters: {
          actorUserId: '   ',
          action: '',
          outcome: '',
          entityType: '',
          fromDate: '',
          toDate: '',
          search: '   ',
        },
      }),
    );

    const [url] = fetchMock.mock.calls[0] as [string];
    expect(url).not.toContain('actorUserId=');
    expect(url).not.toContain('action=');
    expect(url).not.toContain('outcome=');
    expect(url).not.toContain('entityType=');
    expect(url).not.toContain('fromDate=');
    expect(url).not.toContain('toDate=');
    expect(url).not.toContain('search=');
  });

  it('omits the empty-user guid from the query string', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 }),
    );

    const service = buildService(fetchMock);
    await service.search(
      buildQuery({
        filters: {
          ...EmptyAuditLogFilters,
          actorUserId: '00000000-0000-0000-0000-000000000000',
        },
      }),
    );

    const [url] = fetchMock.mock.calls[0] as [string];
    expect(url).not.toContain('actorUserId=');
  });

  it('returns the decoded payload', async () => {
    const payload: PaginatedResponse<AuditLogEntry> = {
      items: [
        {
          id: 'guid-1',
          timestamp: '2026-09-02T10:00:00Z',
          action: AuditAction.LoginSucceeded,
          outcome: AuditOutcome.Success,
          entityType: 'User',
          entityId: 'user-1',
          details: 'Login',
          ipAddress: '127.0.0.1',
          actorUserId: 'user-1',
          actorUserName: 'admin',
        },
      ],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
    };
    fetchMock.mockResolvedValue(jsonResponse(payload));

    const service = buildService(fetchMock);
    const response = await service.search(buildQuery());

    expect(response).toEqual(payload);
  });

  it('propagates fetch failures as Network errors', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    const service = buildService(fetchMock);

    await expect(service.search(buildQuery())).rejects.toMatchObject({
      kind: 'Network',
    });
  });

  it('propagates 403 responses as Forbidden errors', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ title: 'Forbidden', detail: 'denied', type: 'auditlog.forbidden' }, 403),
    );
    const service = buildService(fetchMock);

    await expect(service.search(buildQuery())).rejects.toMatchObject({
      kind: 'Forbidden',
      status: 403,
    });
  });
});

describe('auditLogService.getById', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it('hits /api/auditoria/{id} with the encoded id', async () => {
    const entry: AuditLogEntry = {
      id: '11111111-1111-1111-1111-111111111111',
      timestamp: '2026-09-02T10:00:00Z',
      action: AuditAction.RequestCreated,
      outcome: AuditOutcome.Success,
      entityType: 'Request',
      entityId: 'req-1',
      details: null,
      ipAddress: null,
      actorUserId: 'user-1',
      actorUserName: 'admin',
    };
    fetchMock.mockResolvedValue(jsonResponse(entry));

    const service = buildService(fetchMock);
    const response = await service.getById(entry.id);

    const [url] = fetchMock.mock.calls[0] as [string];
    expect(url).toContain('/api/auditoria/11111111-1111-1111-1111-111111111111');
    expect(response).toEqual(entry);
  });

  it('propagates 404 responses as NotFound errors', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ title: 'NotFound', detail: 'missing' }, 404),
    );
    const service = buildService(fetchMock);

    await expect(service.getById('missing-id')).rejects.toMatchObject({
      kind: 'NotFound',
    });
  });
});