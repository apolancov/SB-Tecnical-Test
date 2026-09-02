import { UserRole, type AuthenticatedUser } from '../types/auth';

export interface AuthenticationState {
  readonly accessToken: string;
  readonly expiresAt: string;
  readonly user: AuthenticatedUser;
}

const StorageKey = 'sb.auth.session.v1';
const SupportedRoles = new Set<string>(Object.values(UserRole));

function hasSessionStorage(): boolean {
  return typeof window !== 'undefined' && typeof window.sessionStorage !== 'undefined';
}

function isAuthenticationState(value: unknown): value is AuthenticationState {
  if (value === null || typeof value !== 'object') {
    return false;
  }
  const candidate = value as Record<string, unknown>;
  if (typeof candidate.accessToken !== 'string' || candidate.accessToken.length === 0) {
    return false;
  }
  if (typeof candidate.expiresAt !== 'string' || candidate.expiresAt.length === 0) {
    return false;
  }
  if (candidate.user === null || typeof candidate.user !== 'object') {
    return false;
  }
  const user = candidate.user as Record<string, unknown>;
  return (
    typeof user.id === 'string' &&
    typeof user.username === 'string' &&
    typeof user.email === 'string' &&
    typeof user.role === 'string' &&
    SupportedRoles.has(user.role)
  );
}

export function readStoredAuthentication(): AuthenticationState | null {
  if (!hasSessionStorage()) {
    return null;
  }
  try {
    const raw = window.sessionStorage.getItem(StorageKey);
    if (raw === null) {
      return null;
    }
    const parsed: unknown = JSON.parse(raw);
    if (!isAuthenticationState(parsed)) {
      window.sessionStorage.removeItem(StorageKey);
      return null;
    }
    const expiry = Date.parse(parsed.expiresAt);
    if (Number.isNaN(expiry) || expiry <= Date.now()) {
      window.sessionStorage.removeItem(StorageKey);
      return null;
    }
    return parsed;
  } catch {
    try {
      window.sessionStorage.removeItem(StorageKey);
    } catch {
    }
    return null;
  }
}

export function writeStoredAuthentication(state: AuthenticationState): void {
  if (!hasSessionStorage()) {
    return;
  }
  window.sessionStorage.setItem(StorageKey, JSON.stringify(state));
}

export function clearStoredAuthentication(): void {
  if (!hasSessionStorage()) {
    return;
  }
  window.sessionStorage.removeItem(StorageKey);
}
