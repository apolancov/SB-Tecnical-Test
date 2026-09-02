import { runtimeConfiguration } from '../config/runtime';

export const ApiErrorKind = {
  Network: 'Network',
  Unauthorized: 'Unauthorized',
  Forbidden: 'Forbidden',
  NotFound: 'NotFound',
  Validation: 'Validation',
  Conflict: 'Conflict',
  Server: 'Server',
  Unknown: 'Unknown',
} as const;

export type ApiErrorKind = (typeof ApiErrorKind)[keyof typeof ApiErrorKind];

export interface ApiError {
  readonly kind: ApiErrorKind;
  readonly status: number;
  readonly message: string;
  readonly code: string | null;
}

export type TokenProvider = () => string | null;

export type UnauthorizedHandler = () => void;

interface ProblemDetails {
  readonly title?: string;
  readonly detail?: string;
  readonly type?: string;
  readonly status?: number;
}

interface RequestOptions {
  readonly method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  readonly path: string;
  readonly query?: Readonly<Record<string, string | number | undefined>>;
  readonly body?: unknown;
  readonly signal?: AbortSignal;
}

function buildUrl(
  path: string,
  query: RequestOptions['query'],
): string {
  const base = runtimeConfiguration.apiBaseUrl.replace(/\/+$/, '');
  const normalizedPath = path.startsWith('/') ? path : `/${path}`;
  let url = `${base}${normalizedPath}`;
  if (query) {
    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(query)) {
      if (value === undefined || value === null) {
        continue;
      }
      const stringValue = String(value);
      if (stringValue.length === 0) {
        continue;
      }
      params.append(key, stringValue);
    }
    const queryString = params.toString();
    if (queryString.length > 0) {
      url = `${url}?${queryString}`;
    }
  }
  return url;
}

function readProblemDetails(payload: unknown): ProblemDetails | null {
  if (payload === null || typeof payload !== 'object') {
    return null;
  }
  const candidate = payload as Record<string, unknown>;
  if (typeof candidate.title !== 'string' && typeof candidate.detail !== 'string') {
    return null;
  }
  return candidate as ProblemDetails;
}

function createApiError(
  kind: ApiErrorKind,
  status: number,
  message: string,
  code: string | null,
): ApiError {
  return { kind, status, message, code };
}

export class ApiClient {
  private readonly tokenProvider: TokenProvider;
  private readonly onUnauthorized: UnauthorizedHandler;

  constructor(tokenProvider: TokenProvider, onUnauthorized: UnauthorizedHandler) {
    this.tokenProvider = tokenProvider;
    this.onUnauthorized = onUnauthorized;
  }

  async request<TResponse>(options: RequestOptions): Promise<TResponse> {
    const { method, path, query, body, signal } = options;
    const url = buildUrl(path, query);

    const headers: Record<string, string> = {
      Accept: 'application/json',
    };

    if (body !== undefined) {
      headers['Content-Type'] = 'application/json';
    }

    const token = this.tokenProvider();
    if (token !== null && token.length > 0) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    let response: Response;
    try {
      response = await fetch(url, {
        method,
        headers,
        body: body === undefined ? undefined : JSON.stringify(body),
        signal: signal ?? null,
      });
    } catch (cause) {
      const message = cause instanceof Error ? cause.message : 'Network error';
      throw createApiError(ApiErrorKind.Network, 0, message, null);
    }

    if (response.status === 204) {
      return undefined as TResponse;
    }

    const contentType = response.headers.get('Content-Type') ?? '';
    const isJson = contentType.toLowerCase().includes('application/json');
    const payload: unknown = isJson
      ? await response.json().catch(() => null)
      : await response.text().catch(() => null);

    if (response.ok) {
      return payload as TResponse;
    }

    const problem = readProblemDetails(payload);
    const fallbackMessage = `Request failed with status ${response.status}.`;
    const message = problem?.detail ?? problem?.title ?? fallbackMessage;
    const code = problem?.type ?? null;

    if (response.status === 401) {
      this.onUnauthorized();
      throw createApiError(ApiErrorKind.Unauthorized, 401, message, code);
    }

    if (response.status === 403) {
      throw createApiError(ApiErrorKind.Forbidden, 403, message, code);
    }

    if (response.status === 404) {
      throw createApiError(ApiErrorKind.NotFound, 404, message, code);
    }

    if (response.status === 409) {
      throw createApiError(ApiErrorKind.Conflict, 409, message, code);
    }

    if (response.status === 400) {
      throw createApiError(ApiErrorKind.Validation, 400, message, code);
    }

    if (response.status >= 500) {
      throw createApiError(ApiErrorKind.Server, response.status, message, code);
    }

    throw createApiError(ApiErrorKind.Unknown, response.status, message, code);
  }

  get<TResponse>(
    path: string,
    query?: RequestOptions['query'],
    signal?: AbortSignal,
  ): Promise<TResponse> {
    return this.request<TResponse>({
      method: 'GET',
      path,
      query,
      signal,
    });
  }

  post<TResponse, TBody = unknown>(
    path: string,
    body: TBody,
    signal?: AbortSignal,
  ): Promise<TResponse> {
    return this.request<TResponse>({
      method: 'POST',
      path,
      body,
      signal,
    });
  }
}

export function isApiError(value: unknown): value is ApiError {
  if (value === null || typeof value !== 'object') {
    return false;
  }
  const candidate = value as Record<string, unknown>;
  return (
    typeof candidate.kind === 'string' &&
    typeof candidate.status === 'number' &&
    typeof candidate.message === 'string'
  );
}
