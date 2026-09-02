'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import type { ReactNode } from 'react';
import {
  UserRole,
  isAdministrativeRole,
  isStaffRole,
  type UserRole as UserRoleType,
} from '../types/auth';
import { useAuth } from '../hooks/useAuth';
import { Logo } from './Logo';

export interface AppShellProps {
  readonly children: ReactNode;
}

interface NavItem {
  readonly href: string;
  readonly label: string;
  readonly testId: string;
  readonly isActive: (pathname: string) => boolean;
  readonly isAllowed: (role: UserRoleType) => boolean;
}

const Navigation: ReadonlyArray<NavItem> = [
  {
    href: '/dashboard',
    label: 'Dashboard',
    testId: 'nav-dashboard',
    isActive: (pathname) =>
      pathname === '/dashboard' || pathname === '/dashboard/',
    isAllowed: () => true,
  },
  {
    href: '/requests',
    label: 'Solicitudes',
    testId: 'nav-requests',
    isActive: (pathname) =>
      pathname === '/requests' ||
      pathname === '/requests/' ||
      (pathname?.startsWith('/requests/') ?? false),
    isAllowed: () => true,
  },
  {
    href: '/requests/new',
    label: 'Nueva solicitud',
    testId: 'nav-new-request',
    isActive: (pathname) =>
      pathname === '/requests/new' || pathname === '/requests/new/',
    isAllowed: (role) => role === UserRole.Solicitante || isStaffRole(role),
  },
  {
    href: '/users',
    label: 'Usuarios',
    testId: 'nav-users',
    isActive: (pathname) =>
      pathname === '/users' ||
      pathname === '/users/' ||
      (pathname?.startsWith('/users/') ?? false),
    isAllowed: (role) => isAdministrativeRole(role),
  },
  {
    href: '/areas',
    label: 'Áreas',
    testId: 'nav-areas',
    isActive: (pathname) =>
      pathname === '/areas' ||
      pathname === '/areas/' ||
      (pathname?.startsWith('/areas/') ?? false),
    isAllowed: (role) => isAdministrativeRole(role),
  },
  {
    href: '/request-types',
    label: 'Tipos de solicitud',
    testId: 'nav-request-types',
    isActive: (pathname) =>
      pathname === '/request-types' ||
      pathname === '/request-types/' ||
      (pathname?.startsWith('/request-types/') ?? false),
    isAllowed: (role) => isAdministrativeRole(role),
  },
  {
    href: '/audit-log',
    label: 'Bitácora de auditoría',
    testId: 'nav-audit-log',
    isActive: (pathname) =>
      pathname === '/audit-log' ||
      pathname === '/audit-log/' ||
      (pathname?.startsWith('/audit-log/') ?? false),
    isAllowed: (role) => role === UserRole.Admin,
  },
];

function describeRole(role: UserRoleType): string {
  switch (role) {
    case UserRole.Admin:
      return 'Administrador';
    case UserRole.Analista:
      return 'Analista';
    case UserRole.Solicitante:
      return 'Solicitante';
    case UserRole.User:
      return 'Usuario';
    default:
      return role;
  }
}

export function AppShell({ children }: AppShellProps) {
  const { user, logout } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const role: UserRoleType | null = user?.role ?? null;

  const handleLogout = () => {
    logout();
    router.replace('/login');
  };

  const visibleNavigation =
    role === null
      ? []
      : Navigation.filter((item) => item.isAllowed(role));

  return (
    <div className="app-shell">
      <header className="app-shell__header">
        <div className="app-shell__brand">
          <Link href="/dashboard" className="app-shell__title" data-testid="nav-home">
            <Logo className="app-shell__logo" size="medium" alt="SB Client" />
            <span>SB Client</span>
          </Link>
        </div>
        <nav aria-label="Principal" className="app-shell__nav">
          {visibleNavigation.map((item) => {
            const active = item.isActive(pathname ?? '/dashboard');
            return (
              <Link
                key={item.href}
                href={item.href}
                className={`app-shell__link${active ? ' active' : ''}`}
                data-testid={item.testId}
              >
                {item.label}
              </Link>
            );
          })}
        </nav>
        <div className="app-shell__user">
          {user !== null && (
            <span className="app-shell__user-info">
              <span className="app-shell__username" data-testid="current-user">
                {user.username}
              </span>
              <span className="app-shell__role" data-testid="current-role">
                {describeRole(user.role)}
              </span>
            </span>
          )}
          <button
            type="button"
            className="button"
            onClick={handleLogout}
            data-testid="logout-button"
          >
            Cerrar sesión
          </button>
        </div>
      </header>
      <main className="app-shell__main">{children}</main>
    </div>
  );
}