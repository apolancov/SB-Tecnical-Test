import type { ApiClient } from './api';
import type { DashboardSummary } from '../types/dashboard';

export interface DashboardService {
  getSummary(signal?: AbortSignal): Promise<DashboardSummary>;
}

export class DefaultDashboardService implements DashboardService {
  private readonly apiClient: ApiClient;

  constructor(apiClient: ApiClient) {
    this.apiClient = apiClient;
  }

  getSummary(signal?: AbortSignal): Promise<DashboardSummary> {
    return this.apiClient.get<DashboardSummary>(
      '/api/dashboard/resumen',
      undefined,
      signal,
    );
  }
}