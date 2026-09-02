import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiClient, ApiErrorKind, isApiError } from './api';

describe('ApiClient', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  function jsonResponse(body: unknown, status = 200, contentType = 'application/json'): Response {
    return new Response(JSON.stringify(body), {
      status,
      headers: { 'Content-Type': contentType },
    });
  }

  function emptyResponse(status: number): Response {
    return new Response(null, { status });
  }

  it('attaches bearer token when a token provider returns a value', async () => {
    fetchMock.mockResolvedValue(jsonResponse({ items: [] }));

    const client = new ApiClient(() => 'jwt-token', () => undefined);
    await client.get('/api/example');

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(init.method).toBe('GET');
    const headers = init.headers as Record<string, string>;
    expect(headers['Authorization']).toBe('Bearer jwt-token');
    expect(headers['Accept']).toBe('application/json');
  });

  it('omits the Authorization header when no token is available', async () => {
    fetchMock.mockResolvedValue(jsonResponse({ ok: true }));

    const client = new ApiClient(() => null, () => undefined);
    await client.get('/api/example');

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Record<string, string>;
    expect(headers['Authorization']).toBeUndefined();
  });

  it('serializes JSON bodies and sets Content-Type', async () => {
    fetchMock.mockResolvedValue(jsonResponse({ ok: true }));

    const client = new ApiClient(() => null, () => undefined);
    await client.post('/api/example', { hello: 'world' });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Record<string, string>;
    expect(headers['Content-Type']).toBe('application/json');
    expect(init.body).toBe(JSON.stringify({ hello: 'world' }));
  });

  it('appends only non-empty query parameters', async () => {
    fetchMock.mockResolvedValue(jsonResponse({ items: [] }));

    const client = new ApiClient(() => null, () => undefined);
    await client.get('/api/example', { page: 1, pageSize: 10, name: '', filter: undefined });

    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('page=1');
    expect(url).toContain('pageSize=10');
    expect(url).not.toContain('name=');
    expect(url).not.toContain('filter=');
  });

  it('clears the session and surfaces Unauthorized on 401', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ title: 'Auth failed.', detail: 'Invalid token.' }, 401),
    );

    const onUnauthorized = vi.fn();
    const client = new ApiClient(() => 'jwt-token', onUnauthorized);

    await expect(client.get('/api/secure')).rejects.toMatchObject({
      kind: ApiErrorKind.Unauthorized,
      status: 401,
    });
    expect(onUnauthorized).toHaveBeenCalledOnce();
  });

  it('does not invoke the unauthorized callback on 403', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ title: 'Forbidden', detail: 'No role.' }, 403),
    );

    const onUnauthorized = vi.fn();
    const client = new ApiClient(() => 'jwt-token', onUnauthorized);

    await expect(client.get('/api/secure')).rejects.toMatchObject({
      kind: ApiErrorKind.Forbidden,
      status: 403,
    });
    expect(onUnauthorized).not.toHaveBeenCalled();
  });

  it('classifies 400 responses as Validation errors', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ title: 'Invalid request.' }, 400),
    );

    const client = new ApiClient(() => null, () => undefined);
    await expect(client.get('/api/x')).rejects.toMatchObject({
      kind: ApiErrorKind.Validation,
      status: 400,
    });
  });

  it('classifies 5xx responses as Server errors', async () => {
    fetchMock.mockResolvedValue(emptyResponse(503));

    const client = new ApiClient(() => null, () => undefined);
    await expect(client.get('/api/x')).rejects.toMatchObject({
      kind: ApiErrorKind.Server,
      status: 503,
    });
  });

  it('maps network failures to ApiErrorKind.Network', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));

    const client = new ApiClient(() => null, () => undefined);
    try {
      await client.get('/api/x');
      throw new Error('Expected request to throw.');
    } catch (error) {
      expect(isApiError(error)).toBe(true);
      if (isApiError(error)) {
        expect(error.kind).toBe(ApiErrorKind.Network);
        expect(error.status).toBe(0);
      }
    }
  });

  it('returns undefined for 204 No Content responses', async () => {
    fetchMock.mockResolvedValue(emptyResponse(204));

    const client = new ApiClient(() => null, () => undefined);
    const result = await client.get<undefined>('/api/x');
    expect(result).toBeUndefined();
  });
});
