import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useState } from 'react';
import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';
import { ServiceProvider } from './ServiceContext';
import { AuthProvider, useAuthContext } from './AuthContext';
import { useAuth } from './useAuth';
import { buildTestServices } from '../test/testUtils';
import type { ServiceContainer } from '../services/container';
import type { AuthenticationState } from '../services/authStorage';

interface WrapperOptions {
  readonly services: ServiceContainer;
  readonly initial: AuthenticationState | null;
}

function buildWrapper({ services, initial }: WrapperOptions) {
  return function Wrapper({ children }: { children: ReactNode }) {
    const [authentication, setAuthentication] = useState<AuthenticationState | null>(initial);
    return (
      <ServiceProvider services={services}>
        <AuthProvider
          services={services}
          authentication={authentication}
          onAuthenticationChange={setAuthentication}
        >
          {children}
        </AuthProvider>
      </ServiceProvider>
    );
  };
}

describe('useAuth', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  function jsonResponse(body: unknown): Response {
    return new Response(JSON.stringify(body), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  it('exposes isAuthenticated=false before login', () => {
    const services = buildTestServices();
    const { result } = renderHook(() => useAuth(), {
      wrapper: buildWrapper({ services, initial: null }),
    });
    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
  });

  it('logs in, stores the JWT and flips isAuthenticated to true', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({
        accessToken: 'jwt',
        tokenType: 'Bearer',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        user: { id: '1', username: 'admin', email: 'admin@example.local', role: 'Admin' },
      }),
    );

    const services = buildTestServices();
    const { result } = renderHook(() => useAuth(), {
      wrapper: buildWrapper({ services, initial: null }),
    });

    await act(async () => {
      await result.current.login({ username: 'admin', password: 'AdminPass123!' });
    });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user?.role).toBe('Admin');
    expect(sessionStorage.getItem('sb.auth.session.v1')).not.toBeNull();
  });

  it('clears the session on logout', async () => {
    const initial: AuthenticationState = {
      accessToken: 'jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { id: '1', username: 'admin', email: 'admin@example.local', role: 'Admin' },
    };

    const services = buildTestServices(initial);
    const { result } = renderHook(() => useAuth(), {
      wrapper: buildWrapper({ services, initial }),
    });

    expect(result.current.isAuthenticated).toBe(true);

    act(() => {
      result.current.logout();
    });

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
    expect(sessionStorage.getItem('sb.auth.session.v1')).toBeNull();
  });

  it('exposes a forceSignOut action used after a 401 from the API', async () => {
    const initial: AuthenticationState = {
      accessToken: 'jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { id: '1', username: 'admin', email: 'admin@example.local', role: 'Admin' },
    };

    const services = buildTestServices(initial);
    const { result } = renderHook(() => useAuthContext(), {
      wrapper: buildWrapper({ services, initial }),
    });

    act(() => {
      result.current.forceSignOut();
    });

    expect(result.current.isAuthenticated).toBe(false);
  });

  it('starts authenticated when initialAuthentication reflects a persisted session', () => {
    const initial: AuthenticationState = {
      accessToken: 'jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { id: '1', username: 'admin', email: 'admin@example.local', role: 'Admin' },
    };

    const services = buildTestServices(initial);
    const { result } = renderHook(() => useAuthContext(), {
      wrapper: buildWrapper({ services, initial }),
    });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user?.username).toBe('admin');
    expect(result.current.accessToken).toBe('jwt');
  });
});
