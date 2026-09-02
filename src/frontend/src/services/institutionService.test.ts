import { describe, expect, it, vi, beforeEach } from 'vitest';
import { ApiClient } from './api';
import { DefaultInstitutionService } from './institutionService';
import { DefaultInstitutionFilters } from '../hooks/useInstitutions';
import type {
  PaginatedResponse,
  Institution,
  InstitutionFilterOptions,
  InstitutionInput,
} from '../types/institution';

function buildClient(fetchMock: ReturnType<typeof vi.fn>): {
  client: ApiClient;
  service: DefaultInstitutionService;
} {
  const client = new ApiClient(() => null, () => undefined);
  void fetchMock;
  return { client, service: new DefaultInstitutionService(client) };
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

describe('institutionService.search', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  function teardown() {
    globalThis.fetch = originalFetch;
  }

  it('hits /api/institutions with page/pageSize and trimmed filters', async () => {
    fetchMock.mockResolvedValue(
      new Response(
        JSON.stringify({
          items: [],
          page: 1,
          pageSize: 20,
          totalItems: 0,
          totalPages: 0,
        } satisfies PaginatedResponse<Institution>),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ),
    );

    const { service } = buildClient(fetchMock);
    await service.search({
      page: 1,
      pageSize: 20,
      filters: { name: '  Acuario  ', category: '  ', statePower: '   ', sector: '' },
    });

    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/institutions');
    expect(url).toContain('page=1');
    expect(url).toContain('pageSize=20');
    expect(url).toContain('name=Acuario');
    expect(url).not.toContain('category=');
    expect(url).not.toContain('statePower=');
    expect(url).not.toContain('sector=');
    teardown();
  });

  it('returns the decoded payload', async () => {
    const payload: PaginatedResponse<Institution> = {
      items: [
        {
          id: 'guid',
          name: 'Acuario Nacional',
          category: 'Organismo',
          statePower: 'Poder Ejecutivo',
          sector: 'Medio Ambiente',
        },
      ],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
    };

    fetchMock.mockResolvedValue(jsonResponse(payload));

    const { service } = buildClient(fetchMock);
    const response = await service.search({
      page: 1,
      pageSize: 20,
      filters: DefaultInstitutionFilters,
    });

    expect(response).toEqual(payload);
    teardown();
  });

  it('propagates fetch failures as errors', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    const { service } = buildClient(fetchMock);
    await expect(
      service.search({
        page: 1,
        pageSize: 20,
        filters: DefaultInstitutionFilters,
      }),
    ).rejects.toMatchObject({ kind: 'Network' });
    teardown();
  });
});

describe('institutionService.getFilterOptions', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  function teardown() {
    globalThis.fetch = originalFetch;
  }

  it('hits /api/institutions/filter-options and returns the decoded payload', async () => {
    const payload: InstitutionFilterOptions = {
      categories: ['Ministerio', 'Universidad'],
      statePowers: ['Poder Ejecutivo'],
      sectors: ['Cultura', 'Educación'],
    };

    fetchMock.mockResolvedValue(jsonResponse(payload));

    const { service } = buildClient(fetchMock);
    const options = await service.getFilterOptions();

    expect(options).toEqual(payload);
    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/institutions/filter-options');
    teardown();
  });

  it('propagates fetch failures as errors', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    const { service } = buildClient(fetchMock);
    await expect(service.getFilterOptions()).rejects.toMatchObject({ kind: 'Network' });
    teardown();
  });
});

describe('institutionService mutations', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  function teardown() {
    globalThis.fetch = originalFetch;
  }

  const sample: Institution = {
    id: 'abc-123',
    name: 'Acuario Nacional',
    category: 'Organismo',
    statePower: 'Poder Ejecutivo',
    sector: 'Medio Ambiente',
  };

  const sampleInput: InstitutionInput = {
    name: sample.name,
    category: sample.category,
    statePower: sample.statePower,
    sector: sample.sector,
  };

  it('POSTs /api/institutions on create', async () => {
    fetchMock.mockResolvedValue(jsonResponse(sample));
    const { service } = buildClient(fetchMock);

    const created = await service.create(sampleInput);

    expect(created).toEqual(sample);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/institutions');
    expect(url).not.toContain('filter-options');
    expect(init.method).toBe('POST');
    expect(JSON.parse(init.body as string)).toEqual(sampleInput);
    teardown();
  });

  it('GETs /api/institutions/{id} on getById', async () => {
    fetchMock.mockResolvedValue(jsonResponse(sample));
    const { service } = buildClient(fetchMock);

    const result = await service.getById(sample.id);

    expect(result).toEqual(sample);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain(`/api/institutions/${encodeURIComponent(sample.id)}`);
    expect(init.method).toBe('GET');
    teardown();
  });

  it('PUTs /api/institutions/{id} on update', async () => {
    fetchMock.mockResolvedValue(jsonResponse(sample));
    const { service } = buildClient(fetchMock);

    const result = await service.update(sample.id, sampleInput);

    expect(result).toEqual(sample);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain(`/api/institutions/${encodeURIComponent(sample.id)}`);
    expect(init.method).toBe('PUT');
    expect(JSON.parse(init.body as string)).toEqual(sampleInput);
    teardown();
  });

  it('DELETEs /api/institutions/{id} on remove', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }));
    const { service } = buildClient(fetchMock);

    await expect(service.remove(sample.id)).resolves.toBeUndefined();

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain(`/api/institutions/${encodeURIComponent(sample.id)}`);
    expect(init.method).toBe('DELETE');
    teardown();
  });

  it('propagates fetch failures as errors on create', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    const { service } = buildClient(fetchMock);
    await expect(service.create(sampleInput)).rejects.toMatchObject({ kind: 'Network' });
    teardown();
  });
});
