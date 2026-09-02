'use client';

import { useAuthContext } from './AuthContext';

export function useAuth() {
  const { isAuthenticated, user, login, logout, forceSignOut } = useAuthContext();
  return {
    isAuthenticated,
    user,
    login,
    logout,
    forceSignOut,
  } as const;
}
