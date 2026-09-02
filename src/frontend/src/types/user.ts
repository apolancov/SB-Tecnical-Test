import type { UserRole } from './auth';

export interface User {
  readonly id: string;
  readonly username: string;
  readonly email: string;
  readonly role: UserRole;
  readonly isActive: boolean;
  readonly createdAt: string;
}

export interface UserInput {
  readonly username: string;
  readonly email: string;
  readonly password: string;
  readonly role: UserRole;
  readonly isActive: boolean;
}

export interface UserUpdateInput {
  readonly username: string;
  readonly email: string;
  readonly role: UserRole;
  readonly isActive: boolean;
}

export interface UserFilters {
  readonly username: string;
  readonly email: string;
  readonly role: UserRole | null;
  readonly isActive: boolean | null;
}

export interface UserQuery {
  readonly page: number;
  readonly pageSize: number;
  readonly filters: UserFilters;
}

export const EmptyUserInput: UserInput = {
  username: '',
  email: '',
  password: '',
  role: 'User',
  isActive: true,
};

export const DefaultUserFilters: UserFilters = {
  username: '',
  email: '',
  role: null,
  isActive: null,
};
