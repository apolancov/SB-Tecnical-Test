'use client';

import { createContext, useContext, useMemo, useRef } from 'react';
import type { ReactNode } from 'react';
import type { LoginRequest, AuthenticatedUser } from '../types/auth';
import type { ServiceContainer } from '../services/container';
import type { AuthenticationState } from '../services/authStorage';

export interface AuthContextValue {
  readonly isAuthenticated: boolean;
  readonly user: AuthenticatedUser | null;
  readonly accessToken: string | null;
  login(request: LoginRequest): Promise<void>;
  logout(): void;
  forceSignOut(): void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export interface AuthProviderProps {
  readonly services: ServiceContainer;
  readonly authentication: AuthenticationState | null;
  readonly onAuthenticationChange: (next: AuthenticationState | null) => void;
  readonly children: ReactNode;
}

export function AuthProvider({
  services,
  authentication,
  onAuthenticationChange,
  children,
}: AuthProviderProps) {
  const servicesRef = useRef(services);
  servicesRef.current = services;

  const onAuthChangeRef = useRef(onAuthenticationChange);
  onAuthChangeRef.current = onAuthenticationChange;

  const value = useMemo<AuthContextValue>(() => {
    const persist = (next: AuthenticationState | null) => {
      servicesRef.current.setAuthentication(next);
      onAuthChangeRef.current(next);
    };

    return {
      isAuthenticated: authentication !== null,
      user: authentication?.user ?? null,
      accessToken: authentication?.accessToken ?? null,
      async login(request: LoginRequest): Promise<void> {
        const response = await servicesRef.current.authenticationService.login(request);
        const next: AuthenticationState = {
          accessToken: response.accessToken,
          expiresAt: response.expiresAt,
          user: response.user,
        };
        persist(next);
      },
      logout(): void {
        servicesRef.current.authenticationService.logout();
        persist(null);
      },
      forceSignOut(): void {
        servicesRef.current.clearAuthentication();
        persist(null);
      },
    };
  }, [authentication]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuthContext(): AuthContextValue {
  const value = useContext(AuthContext);
  if (value === null) {
    throw new Error('useAuthContext must be used within an AuthProvider.');
  }
  return value;
}
