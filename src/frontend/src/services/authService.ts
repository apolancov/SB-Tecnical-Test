import type { ApiClient } from './api';
import type { AuthenticationResponse, LoginRequest } from '../types/auth';
import {
  clearStoredAuthentication,
  readStoredAuthentication,
  writeStoredAuthentication,
  type AuthenticationState,
} from './authStorage';

export interface AuthenticationService {
  login(request: LoginRequest): Promise<AuthenticationResponse>;
  logout(): void;
  restore(): AuthenticationState | null;
  current(): AuthenticationState | null;
}

export class DefaultAuthenticationService implements AuthenticationService {
  private currentState: AuthenticationState | null;
  private readonly apiClient: ApiClient;

  constructor(apiClient: ApiClient) {
    this.apiClient = apiClient;
    this.currentState = readStoredAuthentication();
  }

  async login(request: LoginRequest): Promise<AuthenticationResponse> {
    const response = await this.apiClient.post<AuthenticationResponse, LoginRequest>(
      '/api/auth/login',
      request,
    );
    const state: AuthenticationState = {
      accessToken: response.accessToken,
      expiresAt: response.expiresAt,
      user: response.user,
    };
    writeStoredAuthentication(state);
    this.currentState = state;
    return response;
  }

  logout(): void {
    clearStoredAuthentication();
    this.currentState = null;
  }

  restore(): AuthenticationState | null {
    const restored = readStoredAuthentication();
    this.currentState = restored;
    return restored;
  }

  current(): AuthenticationState | null {
    return this.currentState;
  }
}

export function clearAuthentication(): void {
  clearStoredAuthentication();
}
