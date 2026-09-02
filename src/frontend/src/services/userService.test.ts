import { describe, expect, it, vi, beforeEach } from 'vitest';
import { ApiClient } from './api';
import { DefaultUserService } from './userService';
import type { PaginatedResponse } from '../types/institution';
import type {
  User,
  UserInput,
  UserUpdateInput,
  UserFilters,
} from '../types/user';

const DefaultUserFilters: UserFilters = {
  username: '',
  email: '',
  role: null,
  isActive: null,
};

function buildClient(fetchMock: ReturnType<typeof vi.fn>): {
  client: ApiClient;
  service: DefaultUserService;
} {
  const client = new ApiClient(() => null, () => undefined);
  void fetchMock;
  return { client, service: new DefaultUserService(client) };
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

describe('userService.search', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  function teardown() {
    globalThis.fetch = originalFetch;
  }

  it('hits /api/users with page/pageSize and trimmed filters', async () => {
    fetchMock.mockResolvedValue(
      new Response(
        JSON.stringify({
          items: [],
          page: 1,
          pageSize: 20,
          totalItems: 0,
          totalPages: 0,
        } satisfies PaginatedResponse<User>),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ),
    );

    const { service } = buildClient(fetchMock);
    await service.search({
      page: 1,
      pageSize: 20,
      filters: {
        username: '  admin  ',
        email: '  ',
        role: null,
        isActive: null,
      },
    });

    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/users');
    expect(url).toContain('page=1');
    expect(url).toContain('pageSize=20');
    expect(url).toContain('username=admin');
    expect(url).not.toContain('email=');
    expect(url).not.toContain('role=');
    expect(url).not.toContain('isActive=');
    teardown();
  });

  it('serializes role and isActive filters when set', async () => {
    fetchMock.mockResolvedValue(jsonResponse({
      items: [],
      page: 1,
      pageSize: 20,
      totalItems: 0,
      totalPages: 0,
    } satisfies PaginatedResponse<User>));

    const { service } = buildClient(fetchMock);
    await service.search({
      page: 1,
      pageSize: 20,
      filters: {
        username: '',
        email: '',
        role: 'Admin',
        isActive: false,
      },
    });

    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('role=Admin');
    expect(url).toContain('isActive=false');
    teardown();
  });

  it('returns the decoded payload', async () => {
    const payload: PaginatedResponse<User> = {
      items: [
        {
          id: 'guid-1',
          username: 'admin',
          email: 'admin@example.local',
          role: 'Admin',
          isActive: true,
          createdAt: '2026-01-01T00:00:00Z',
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
      filters: DefaultUserFilters,
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
        filters: DefaultUserFilters,
      }),
    ).rejects.toMatchObject({ kind: 'Network' });
    teardown();
  });
});

describe('userService mutations', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  function teardown() {
    globalThis.fetch = originalFetch;
  }

  const sample: User = {
    id: 'abc-123',
    username: 'admin',
    email: 'admin@example.local',
    role: 'Admin',
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
  };

  const sampleInput: UserInput = {
    username: sample.username,
    email: sample.email,
    password: 'NewPassword123!',
    role: sample.role,
    isActive: sample.isActive,
  };

  it('POSTs /api/users on create', async () => {
    fetchMock.mockResolvedValue(jsonResponse(sample));
    const { service } = buildClient(fetchMock);

    const created = await service.create(sampleInput);

    expect(created).toEqual(sample);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/users');
    expect(init.method).toBe('POST');
    expect(JSON.parse(init.body as string)).toEqual(sampleInput);
    teardown();
  });

  it('GETs /api/users/{id} on getById', async () => {
    fetchMock.mockResolvedValue(jsonResponse(sample));
    const { service } = buildClient(fetchMock);

    const result = await service.getById(sample.id);

    expect(result).toEqual(sample);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain(`/api/users/${encodeURIComponent(sample.id)}`);
    expect(init.method).toBe('GET');
    teardown();
  });

  it('PUTs /api/users/{id} on update', async () => {
    fetchMock.mockResolvedValue(jsonResponse(sample));
    const { service } = buildClient(fetchMock);

    const update: UserUpdateInput = {
      username: sample.username,
      email: sample.email,
      role: sample.role,
      isActive: false,
    };
    const result = await service.update(sample.id, update);

    expect(result).toEqual(sample);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain(`/api/users/${encodeURIComponent(sample.id)}`);
    expect(init.method).toBe('PUT');
    expect(JSON.parse(init.body as string)).toEqual(update);
    teardown();
  });

  it('PUTs /api/users/{id}/password on changePassword', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }));
    const { service } = buildClient(fetchMock);

    await expect(
      service.changePassword(sample.id, 'NewPassword123!'),
    ).resolves.toBeUndefined();

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain(`/api/users/${encodeURIComponent(sample.id)}/password`);
    expect(init.method).toBe('PUT');
    expect(JSON.parse(init.body as string)).toEqual({ newPassword: 'NewPassword123!' });
    teardown();
  });

  it('DELETEs /api/users/{id} on remove', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }));
    const { service } = buildClient(fetchMock);

    await expect(service.remove(sample.id)).resolves.toBeUndefined();

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain(`/api/users/${encodeURIComponent(sample.id)}`);
    expect(init.method).toBe('DELETE');
    teardown();
  });
});
