import { useMemo, useState, type ReactElement, type ReactNode } from 'react';
import { render } from '@testing-library/react';
import type { ServiceContainer } from '../services/container';
import { buildServiceContainer } from '../services/container';
import { AuthProvider } from '../hooks/AuthContext';
import { ServiceProvider } from '../hooks/ServiceContext';
import { readStoredAuthentication, type AuthenticationState } from '../services/authStorage';

export interface RenderWithProvidersOptions {
  readonly initialAuthentication?: AuthenticationState | null;
  readonly services?: ServiceContainer;
}

export interface RenderWithProvidersResult {
  readonly services: ServiceContainer;
}

export function buildTestServices(
  initialAuthentication: AuthenticationState | null = null,
): ServiceContainer {
  return buildServiceContainer({ initialAuthentication });
}

export function renderWithProviders(
  ui: ReactElement,
  options: RenderWithProvidersOptions = {},
): RenderWithProvidersResult {
  const services = options.services ?? buildTestServices(options.initialAuthentication ?? null);
  const initialAuthentication =
    options.initialAuthentication !== undefined
      ? options.initialAuthentication
      : readStoredAuthentication();

  function Wrapper({ children }: { children: ReactNode }) {
    const [authentication, setAuthentication] = useState<AuthenticationState | null>(
      initialAuthentication,
    );
    const onAuthenticationChange = useMemo(() => setAuthentication, []);
    return (
      <ServiceProvider services={services}>
        <AuthProvider
          services={services}
          authentication={authentication}
          onAuthenticationChange={onAuthenticationChange}
        >
          {children}
        </AuthProvider>
      </ServiceProvider>
    );
  }

  render(ui, { wrapper: Wrapper });
  return { services };
}
