import { describe, expect, it } from 'vitest';
import {
  clearStoredAuthentication,
  readStoredAuthentication,
  writeStoredAuthentication,
  type AuthenticationState,
} from './authStorage';

const baseState: AuthenticationState = {
  accessToken: 'token',
  expiresAt: new Date(Date.now() + 60_000).toISOString(),
  user: {
    id: '11111111-1111-1111-1111-111111111111',
    username: 'admin',
    email: 'admin@example.local',
    role: 'Admin',
  },
};

describe('authStorage', () => {
  it('returns null when no session has been stored', () => {
    expect(readStoredAuthentication()).toBeNull();
  });

  it('persists and reads an authentication state', () => {
    writeStoredAuthentication(baseState);
    const restored = readStoredAuthentication();
    expect(restored).toEqual(baseState);
  });

  it('discards sessions whose expiresAt is in the past', () => {
    const expired: AuthenticationState = {
      ...baseState,
      expiresAt: new Date(Date.now() - 1_000).toISOString(),
    };
    writeStoredAuthentication(expired);
    expect(readStoredAuthentication()).toBeNull();
    expect(sessionStorage.getItem('sb.auth.session.v1')).toBeNull();
  });

  it('discards malformed payloads instead of throwing', () => {
    sessionStorage.setItem('sb.auth.session.v1', 'not-json');
    expect(readStoredAuthentication()).toBeNull();
    expect(sessionStorage.getItem('sb.auth.session.v1')).toBeNull();
  });

  it('rejects payloads that omit required fields', () => {
    sessionStorage.setItem('sb.auth.session.v1', JSON.stringify({ accessToken: 'x' }));
    expect(readStoredAuthentication()).toBeNull();
  });

  it('clears the stored session on demand', () => {
    writeStoredAuthentication(baseState);
    clearStoredAuthentication();
    expect(readStoredAuthentication()).toBeNull();
  });
});
