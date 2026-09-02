'use client';

import type { ReactNode } from 'react';
import { createContext, useContext } from 'react';
import type { ServiceContainer } from '../services/container';

const ServiceContext = createContext<ServiceContainer | null>(null);

export interface ServiceProviderProps {
  readonly services: ServiceContainer;
  readonly children: ReactNode;
}

export function ServiceProvider({ services, children }: ServiceProviderProps) {
  return <ServiceContext.Provider value={services}>{children}</ServiceContext.Provider>;
}

export function useService(): ServiceContainer {
  const value = useContext(ServiceContext);
  if (value === null) {
    throw new Error('useService must be used within a ServiceProvider.');
  }
  return value;
}
