import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import ProtectedLayout from './layout';
import {
  getMockRouter,
  resetNavigationMocks,
  setMockPathname,
  setMockSearchParams,
} from '../../test/nextNavigationState';
import { buildTestServices, renderWithProviders } from '../../test/testUtils';

function ProtectedPage() {
  return <div data-testid="protected-content">Protected content</div>;
}

function renderProtected(
  isAuthenticated: boolean,
  pathname = '/institutions',
  searchParams = '',
) {
  setMockPathname(pathname);
  setMockSearchParams(searchParams);
  return renderWithProviders(<ProtectedLayout><ProtectedPage /></ProtectedLayout>, {
    services: buildTestServices(
      isAuthenticated
        ? {
            accessToken: 'jwt',
            expiresAt: new Date(Date.now() + 60_000).toISOString(),
            user: {
              id: '1',
              username: 'admin',
              email: 'admin@example.local',
              role: 'Admin',
            },
          }
        : null,
    ),
    initialAuthentication: isAuthenticated
      ? {
          accessToken: 'jwt',
          expiresAt: new Date(Date.now() + 60_000).toISOString(),
          user: {
            id: '1',
            username: 'admin',
            email: 'admin@example.local',
            role: 'Admin',
          },
        }
      : null,
  });
}

describe('ProtectedLayout', () => {
  beforeEach(() => {
    resetNavigationMocks();
    setMockPathname('/institutions');
    setMockSearchParams('');
  });

  afterEach(() => {
    resetNavigationMocks();
  });

  it('redirects unauthenticated visitors to /login preserving the destination', async () => {
    renderProtected(false, '/institutions', 'page=2');

    await waitFor(() => {
      expect(getMockRouter().replace).toHaveBeenCalled();
    });
    expect(getMockRouter().replace).toHaveBeenCalledWith(
      '/login?from=%2Finstitutions%3Fpage%3D2',
    );
    expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument();
  });

  it('redirects when there are no search params', async () => {
    renderProtected(false, '/institutions', '');

    await waitFor(() => {
      expect(getMockRouter().replace).toHaveBeenCalledWith('/login?from=%2Finstitutions');
    });
  });

  it('renders the protected page for authenticated users', async () => {
    renderProtected(true, '/institutions', '');

    expect(await screen.findByTestId('protected-content')).toBeInTheDocument();
    expect(getMockRouter().replace).not.toHaveBeenCalled();
  });
});
