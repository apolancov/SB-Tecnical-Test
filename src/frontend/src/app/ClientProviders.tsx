'use client';

import { useEffect, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { useRouter } from 'next/navigation';
import { ServiceProvider } from '../hooks/ServiceContext';
import { AuthProvider } from '../hooks/AuthContext';
import { buildServiceContainer, type ServiceContainer } from '../services/container';
import { readStoredAuthentication, type AuthenticationState } from '../services/authStorage';

export interface ClientProvidersProps {
  readonly children: ReactNode;
}

export function ClientProviders({ children }: ClientProvidersProps) {
  const router = useRouter();
  const routerRef = useRef(router);
  routerRef.current = router;

  const [authentication, setAuthentication] =
    useState<AuthenticationState | null>(() => readStoredAuthentication());
  const setAuthenticationRef = useRef(setAuthentication);
  setAuthenticationRef.current = setAuthentication;

  const [services] = useState<ServiceContainer>(() =>
    buildServiceContainer({
      initialAuthentication: readStoredAuthentication(),
      navigateToLogin: () => {
        setAuthenticationRef.current(null);
        routerRef.current.replace('/login');
      },
    }),
  );

  const [bootDone, setBootDone] = useState(false);

  useEffect(() => {
    setBootDone(true);
  }, []);

  if (!bootDone) {
    return (
      <div className="boot-loading" role="status" aria-live="polite">
        <div className="state state--loading">Cargando...</div>
      </div>
    );
  }

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
}
