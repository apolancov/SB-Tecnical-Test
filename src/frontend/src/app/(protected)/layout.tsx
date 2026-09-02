'use client';

import { useEffect } from 'react';
import type { ReactNode } from 'react';
import { usePathname, useRouter, useSearchParams } from 'next/navigation';
import { useAuth } from '../../hooks/useAuth';
import { AppShell } from '../../components/AppShell';

export interface ProtectedLayoutProps {
  readonly children: ReactNode;
}

export default function ProtectedLayout({ children }: ProtectedLayoutProps) {
  const { isAuthenticated } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  useEffect(() => {
    if (!isAuthenticated) {
      const search = searchParams?.toString() ?? '';
      const currentPath = pathname ?? '/dashboard';
      const from = search.length > 0 ? `${currentPath}?${search}` : currentPath;
      router.replace(
        `/login?from=${encodeURIComponent(from)}`,
      );
    }
  }, [isAuthenticated, router, pathname, searchParams]);

  if (!isAuthenticated) {
    return (
      <div className="boot-loading" role="status" aria-live="polite">
        <div className="state state--loading">Cargando...</div>
      </div>
    );
  }

  return <AppShell>{children}</AppShell>;
}
