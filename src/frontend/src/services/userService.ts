import type { ApiClient } from './api';
import type {
  User,
  UserInput,
  UserQuery,
  UserUpdateInput,
} from '../types/user';
import type { PaginatedResponse } from '../types/pagination';

export interface UserService {
  search(parameters: UserQuery, signal?: AbortSignal): Promise<PaginatedResponse<User>>;
  getById(id: string, signal?: AbortSignal): Promise<User>;
  create(input: UserInput, signal?: AbortSignal): Promise<User>;
  update(id: string, input: UserUpdateInput, signal?: AbortSignal): Promise<User>;
  changePassword(id: string, newPassword: string, signal?: AbortSignal): Promise<void>;
  remove(id: string, signal?: AbortSignal): Promise<void>;
}

export class DefaultUserService implements UserService {
  private readonly apiClient: ApiClient;

  constructor(apiClient: ApiClient) {
    this.apiClient = apiClient;
  }

  async search(parameters: UserQuery, signal?: AbortSignal): Promise<PaginatedResponse<User>> {
    const { page, pageSize, filters } = parameters;
    return this.apiClient.get<PaginatedResponse<User>>(
      '/api/users',
      {
        page,
        pageSize,
        username: filters.username.trim() === '' ? undefined : filters.username.trim(),
        email: filters.email.trim() === '' ? undefined : filters.email.trim(),
        role: filters.role ?? undefined,
        isActive:
          filters.isActive === null ? undefined : filters.isActive ? 'true' : 'false',
      },
      signal,
    );
  }

  getById(id: string, signal?: AbortSignal): Promise<User> {
    return this.apiClient.get<User>(
      `/api/users/${encodeURIComponent(id)}`,
      undefined,
      signal,
    );
  }

  create(input: UserInput, signal?: AbortSignal): Promise<User> {
    return this.apiClient.post<User, UserInput>(
      '/api/users',
      input,
      signal,
    );
  }

  update(id: string, input: UserUpdateInput, signal?: AbortSignal): Promise<User> {
    return this.apiClient.request<User>({
      method: 'PUT',
      path: `/api/users/${encodeURIComponent(id)}`,
      body: input,
      signal,
    });
  }

  async changePassword(id: string, newPassword: string, signal?: AbortSignal): Promise<void> {
    await this.apiClient.request<void>({
      method: 'PUT',
      path: `/api/users/${encodeURIComponent(id)}/password`,
      body: { newPassword },
      signal,
    });
  }

  async remove(id: string, signal?: AbortSignal): Promise<void> {
    await this.apiClient.request<void>({
      method: 'DELETE',
      path: `/api/users/${encodeURIComponent(id)}`,
      signal,
    });
  }
}
