import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ClientProviders } from './ClientProviders';
import { useAuth } from '../hooks/useAuth';
import { useService } from '../hooks/ServiceContext';
import {
  getMockRouter,
  resetNavigationMocks,
  setMockPathname,
} from '../test/nextNavigationState';

function readSessionFromStorage() {
  const raw = sessionStorage.getItem('sb.auth.session.v1');
  return raw === null ? null : (JSON.parse(raw) as Record<string, unknown>);
}

function buildAuthenticatedState() {
  return {
    accessToken: 'jwt',
    expiresAt: new Date(Date.now() + 60_000).toISOString(),
    user: {
      id: '11111111-1111-1111-1111-111111111111',
      username: 'admin',
      email: 'admin@example.local',
      role: 'Admin' as const,
    },
  };
}

function AuthProbe({ onReady }: { onReady?: () => void }) {
  const auth = useAuth();
  const services = useService();
  onReady?.();
  return (
    <div>
      <span data-testid="is-authenticated">{String(auth.isAuthenticated)}</span>
      <span data-testid="username">{auth.user?.username ?? 'anonymous'}</span>
      <button
        type="button"
        onClick={() => {
          void auth.login({ username: 'admin', password: 'AdminPass123!' });
        }}
        data-testid="login-button"
      >
        login
      </button>
      <span data-testid="token">{services.getAccessToken() ?? 'none'}</span>
    </div>
  );
}

describe('ClientProviders', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
    setMockPathname('/institutions');
    resetNavigationMocks();
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    resetNavigationMocks();
    sessionStorage.clear();
    localStorage.clear();
  });

  function jsonResponse(body: unknown, status = 200): Response {
    return new Response(JSON.stringify(body), {
      status,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  it('renders the loading state until the boot effect reads sessionStorage', async () => {
    sessionStorage.setItem(
      'sb.auth.session.v1',
      JSON.stringify(buildAuthenticatedState()),
    );

    render(
      <ClientProviders>
        <AuthProbe />
      </ClientProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated').textContent).toBe('true');
    });
    expect(screen.getByTestId('username').textContent).toBe('admin');
    expect(screen.getByTestId('token').textContent).toBe('jwt');
  });

  it('reflects a logged-out state when sessionStorage is empty', async () => {
    render(
      <ClientProviders>
        <AuthProbe />
      </ClientProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated').textContent).toBe('false');
    });
    expect(screen.getByTestId('username').textContent).toBe('anonymous');
    expect(screen.getByTestId('token').textContent).toBe('none');
  });

  it('drives the persisted session into the service container after boot', async () => {
    sessionStorage.setItem(
      'sb.auth.session.v1',
      JSON.stringify(buildAuthenticatedState()),
    );

    render(
      <ClientProviders>
        <AuthProbe />
      </ClientProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId('token').textContent).toBe('jwt');
    });
  });

  it('preserves the persisted session in sessionStorage across a full reload cycle', async () => {
    sessionStorage.setItem(
      'sb.auth.session.v1',
      JSON.stringify(buildAuthenticatedState()),
    );
    const beforeReload = sessionStorage.getItem('sb.auth.session.v1');
    expect(beforeReload).not.toBeNull();

    const { unmount } = render(
      <ClientProviders>
        <AuthProbe />
      </ClientProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated').textContent).toBe('true');
    });

    const afterBoot = sessionStorage.getItem('sb.auth.session.v1');
    expect(afterBoot).toBe(beforeReload);

    unmount();
    sessionStorage.setItem(
      'sb.auth.session.v1',
      JSON.stringify(buildAuthenticatedState()),
    );

    render(
      <ClientProviders>
        <AuthProbe />
      </ClientProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated').textContent).toBe('true');
    });
    expect(screen.getByTestId('token').textContent).toBe('jwt');
    expect(sessionStorage.getItem('sb.auth.session.v1')).not.toBeNull();
  });

  it('redirects to /login and clears the session when the API returns 401', async () => {
    sessionStorage.setItem(
      'sb.auth.session.v1',
      JSON.stringify(buildAuthenticatedState()),
    );

    fetchMock.mockResolvedValue(
      jsonResponse({ title: 'Unauthorized' }, 401),
    );

    let servicesRef: ReturnType<typeof useService> | null = null;
    function Probe() {
      const services = useService();
      servicesRef = services;
      return null;
    }

    render(
      <ClientProviders>
        <Probe />
      </ClientProviders>,
    );

    await waitFor(() => {
      expect(servicesRef).not.toBeNull();
    });

    await act(async () => {
      try {
        await servicesRef!.apiClient.get<unknown>('/api/institutions');
} catch {
    }
    });

    expect(sessionStorage.getItem('sb.auth.session.v1')).toBeNull();
    expect(getMockRouter().replace).toHaveBeenCalledWith('/login');
  });

  it('persists a fresh JWT and exposes it through the service container after login()', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({
        accessToken: 'fresh-jwt',
        tokenType: 'Bearer',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        user: {
          id: '11111111-1111-1111-1111-111111111111',
          username: 'admin',
          email: 'admin@example.local',
          role: 'Admin',
        },
      }),
    );

    const user = userEvent.setup();
    render(
      <ClientProviders>
        <AuthProbe />
      </ClientProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated').textContent).toBe('false');
    });

    await user.click(screen.getByTestId('login-button'));

    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated').textContent).toBe('true');
    });
    expect(screen.getByTestId('token').textContent).toBe('fresh-jwt');

    const stored = readSessionFromStorage();
    expect(stored).not.toBeNull();
    expect((stored as { accessToken: string }).accessToken).toBe('fresh-jwt');
  });
});
