import { describe, expect, it, beforeEach } from 'vitest';
import { ApiClient } from './api';
import { DefaultRequestService } from './requestService';
import {
  CommentVisibility,
  RequestPriority,
  RequestSortDirection,
  RequestSortField,
  RequestStatus,
  type RequestDetail,
  type RequestRecord,
} from '../types/request';
import type { PaginatedResponse } from '../types/pagination';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

function buildClient(fetchMock: ReturnType<typeof vi.fn>): DefaultRequestService {
  const client = new ApiClient(() => null, () => undefined);
  void fetchMock;
  return new DefaultRequestService(client);
}

describe('requestService', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  function teardown() {
    globalThis.fetch = originalFetch;
  }

  const sample: RequestRecord = {
    id: '11111111-1111-1111-1111-111111111111',
    code: 'SOL-2026-0001',
    title: 'Reparación urgente',
    description: 'La bomba se rompió.',
    status: RequestStatus.Submitted,
    priority: RequestPriority.High,
    createdAt: '2026-09-01T10:00:00.000Z',
    dueDate: '2026-09-30T00:00:00.000Z',
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

  it('searches /api/solicitudes with normalized filters', async () => {
    const payload: PaginatedResponse<RequestRecord> = {
      items: [sample],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
    };
    fetchMock.mockResolvedValue(jsonResponse(payload));

    const service = buildClient(fetchMock);
    await service.search({
      page: 1,
      pageSize: 20,
      filters: {
        status: RequestStatus.InReview,
        priority: null,
        areaId: '',
        requestTypeId: '',
        requesterId: '',
        responsibleId: '',
        fromDate: '',
        toDate: '',
        code: '  SOL-001 ',
        search: '  urgente  ',
      },
      sortField: RequestSortField.CreatedAt,
      sortDirection: RequestSortDirection.Descending,
    });

    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/solicitudes');
    expect(url).toContain('status=InReview');
    expect(url).not.toContain('priority=');
    expect(url).not.toContain('areaId=');
    expect(url).toContain('code=SOL-001');
    expect(url).toContain('search=urgente');
    expect(url).toContain('sortBy=CreatedAt');
    expect(url).toContain('sortDirection=Descending');
    teardown();
  });

  it('encodes the id when reading a request', async () => {
    const detail: RequestDetail = {
      ...sample,
      requester: {
        id: sample.requesterId,
        username: 'juan',
        email: 'juan@example.local',
        role: 'Solicitante',
      },
      responsible: null,
      area: { id: sample.areaId, name: 'Mantenimiento' },
      requestType: { id: sample.requestTypeId, name: 'Incidente' },
      statusHistory: [],
      comments: [],
    };
    fetchMock.mockResolvedValue(jsonResponse(detail));

    const service = buildClient(fetchMock);
    const result = await service.getById(sample.id);

    expect(result.id).toBe(sample.id);
    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain(`/api/solicitudes/${encodeURIComponent(sample.id)}`);
    teardown();
  });

  it('creates a request via POST', async () => {
    fetchMock.mockResolvedValue(jsonResponse(sample));

    const service = buildClient(fetchMock);
    const created = await service.create({
      title: 'Reparación urgente',
      description: 'La bomba se rompió.',
      priority: RequestPriority.High,
      areaId: sample.areaId,
      requestTypeId: sample.requestTypeId,
      dueDate: sample.dueDate,
      evidenceUrl: null,
    });

    expect(created).toEqual(sample);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/solicitudes');
    expect(init.method).toBe('POST');
    expect(JSON.parse(init.body as string)).toMatchObject({
      title: 'Reparación urgente',
      priority: 'High',
      areaId: sample.areaId,
      requestTypeId: sample.requestTypeId,
    });
    teardown();
  });

  it('updates a request via PATCH', async () => {
    fetchMock.mockResolvedValue(jsonResponse(sample));

    const service = buildClient(fetchMock);
    await service.update(sample.id, {
      title: 'Reparación actualizada',
      description: 'Detalles nuevos.',
      priority: RequestPriority.Critical,
      dueDate: null,
      evidenceUrl: 'https://example.com/x',
    });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain(`/api/solicitudes/${encodeURIComponent(sample.id)}`);
    expect(init.method).toBe('PATCH');
    expect(JSON.parse(init.body as string)).toMatchObject({
      title: 'Reparación actualizada',
      priority: 'Critical',
      evidenceUrl: 'https://example.com/x',
    });
    teardown();
  });

  it('changes status via PATCH /estado', async () => {
    fetchMock.mockResolvedValue(jsonResponse(sample));

    const service = buildClient(fetchMock);
    await service.changeStatus(sample.id, {
      newStatus: RequestStatus.InReview,
      comment: 'En revisión',
    });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain(`/api/solicitudes/${encodeURIComponent(sample.id)}/estado`);
    expect(init.method).toBe('PATCH');
    teardown();
  });

  it('assigns a responsible via PATCH /asignacion', async () => {
    fetchMock.mockResolvedValue(jsonResponse(sample));

    const service = buildClient(fetchMock);
    await service.assign(sample.id, {
      responsibleUserId: '55555555-5555-5555-5555-555555555555',
      comment: '',
    });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain(`/api/solicitudes/${encodeURIComponent(sample.id)}/asignacion`);
    expect(init.method).toBe('PATCH');
    teardown();
  });

  it('reopens a request via POST /reapertura', async () => {
    fetchMock.mockResolvedValue(jsonResponse(sample));

    const service = buildClient(fetchMock);
    await service.reopen(sample.id, {
      targetStatus: RequestStatus.InProgress,
      comment: 'Reabrir para correcciones',
    });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain(`/api/solicitudes/${encodeURIComponent(sample.id)}/reapertura`);
    expect(init.method).toBe('POST');
    teardown();
  });

  it('adds a comment via POST /comentarios', async () => {
    const comment = {
      id: 'comment-1',
      text: 'Excelente atención.',
      visibility: CommentVisibility.Requester,
      date: '2026-09-01T12:00:00.000Z',
      authorId: sample.requesterId,
      authorUsername: 'juan',
    };
    fetchMock.mockResolvedValue(jsonResponse(comment));

    const service = buildClient(fetchMock);
    const result = await service.addComment(sample.id, {
      text: 'Excelente atención.',
      visibility: CommentVisibility.Requester,
    });

    expect(result).toEqual(comment);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain(`/api/solicitudes/${encodeURIComponent(sample.id)}/comentarios`);
    expect(init.method).toBe('POST');
    teardown();
  });

  it('propagates network failures as errors', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    const service = buildClient(fetchMock);
    await expect(
      service.search({
        page: 1,
        pageSize: 20,
        filters: {
          status: null,
          priority: null,
          areaId: null,
          requestTypeId: null,
          requesterId: null,
          responsibleId: null,
          fromDate: null,
          toDate: null,
          code: '',
          search: '',
        },
        sortField: RequestSortField.CreatedAt,
        sortDirection: RequestSortDirection.Descending,
      }),
    ).rejects.toMatchObject({ kind: 'Network' });
    teardown();
  });
});