import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import { screen, waitFor } from '@testing-library/react';
import { LoginPage } from '../../views/LoginPage';
import {
  resetNavigationMocks,
} from '../nextNavigationState';
import { buildTestServices, renderWithProviders } from '../testUtils';

describe('LoginPage', () => {
  const originalFetch = globalThis.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof fetch;
    resetNavigationMocks();
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    resetNavigationMocks();
  });

  function jsonResponse(body: unknown, status = 200): Response {
    return new Response(JSON.stringify(body), {
      status,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  it('rejects submission with empty required fields', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await user.click(screen.getByTestId('submit-button'));

    expect(await screen.findByTestId('validation-error')).toHaveTextContent(
      /obligatorios/i,
    );
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('disables the submit button while authenticating', async () => {
    let resolveResponse: ((value: Response) => void) | null = null;
    fetchMock.mockImplementation(
      () =>
        new Promise<Response>((resolve) => {
          resolveResponse = resolve;
        }),
    );

    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await user.type(screen.getByTestId('username-input'), 'admin');
    await user.type(screen.getByTestId('password-input'), 'AdminPass123!');
    await user.click(screen.getByTestId('submit-button'));

    await waitFor(() => {
      expect(screen.getByTestId('submit-button')).toBeDisabled();
    });

    expect(typeof resolveResponse).toBe('function');
    (resolveResponse as unknown as (value: Response) => void)(
      jsonResponse({
        accessToken: 'jwt',
        tokenType: 'Bearer',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        user: { id: '1', username: 'admin', email: 'admin@example.local', role: 'Admin' },
      }),
    );

    await waitFor(() => {
      expect(screen.queryByTestId('submit-button')).not.toBeInTheDocument();
    });
  });

  it('logs in successfully on valid credentials, clears sensitive form state, and navigates', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({
        accessToken: 'jwt',
        tokenType: 'Bearer',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        user: { id: '1', username: 'admin', email: 'admin@example.local', role: 'Admin' },
      }),
    );

    const user = userEvent.setup();
    const services = buildTestServices();
    renderWithProviders(<LoginPage />, { services });

    await user.type(screen.getByTestId('username-input'), 'admin');
    await user.type(screen.getByTestId('password-input'), 'AdminPass123!');
    await user.click(screen.getByTestId('submit-button'));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce();
    });

    expect(sessionStorage.getItem('sb.auth.session.v1')).not.toBeNull();
    expect(services.getAccessToken()).toBe('jwt');
  });

  it('displays a user-friendly error on invalid credentials', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse(
        { title: 'Authentication failed.', detail: 'Invalid username or password.' },
        401,
      ),
    );

    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await user.type(screen.getByTestId('username-input'), 'admin');
    await user.type(screen.getByTestId('password-input'), 'wrong-password');
    await user.click(screen.getByTestId('submit-button'));

    const error = await screen.findByTestId('auth-error');
    expect(error).toHaveTextContent(/usuario o contraseña inválidos/i);
    expect(screen.getByTestId('submit-button')).not.toBeDisabled();
  });

  it('does not expose backend exception details in the error message', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse(
        { title: 'Server', detail: 'NpgsqlException: connection refused' },
        500,
      ),
    );

    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await user.type(screen.getByTestId('username-input'), 'admin');
    await user.type(screen.getByTestId('password-input'), 'AdminPass123!');
    await user.click(screen.getByTestId('submit-button'));

    const error = await screen.findByTestId('auth-error');
    expect(error.textContent).not.toContain('NpgsqlException');
  });

  it('redirects authenticated users away from /login', async () => {
    const initialAuth = {
      accessToken: 'jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { id: '1', username: 'admin', email: 'admin@example.local', role: 'Admin' as const },
    };

    renderWithProviders(<LoginPage />, {
      initialAuthentication: initialAuth,
    });

    expect(screen.queryByTestId('username-input')).not.toBeInTheDocument();
  });
});
