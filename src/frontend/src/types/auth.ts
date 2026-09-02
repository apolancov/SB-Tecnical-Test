export const UserRole = {
  Admin: 'Admin',
  User: 'User',
  Analista: 'Analista',
  Solicitante: 'Solicitante',
} as const;

export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export interface LoginRequest {
  readonly username: string;
  readonly password: string;
}

export interface AuthenticatedUser {
  readonly id: string;
  readonly username: string;
  readonly email: string;
  readonly role: UserRole;
}

export interface AuthenticationResponse {
  readonly accessToken: string;
  readonly tokenType: string;
  readonly expiresAt: string;
  readonly user: AuthenticatedUser;
}

export function isStaffRole(role: UserRole): boolean {
  return role === UserRole.Admin || role === UserRole.Analista;
}

export function isAdministrativeRole(role: UserRole): boolean {
  return role === UserRole.Admin;
}