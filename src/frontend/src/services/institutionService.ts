import type { ApiClient } from './api';
import type {
  Institution,
  InstitutionFilterOptions,
  InstitutionFilters,
  InstitutionInput,
  PaginatedResponse,
} from '../types/institution';

export interface InstitutionQueryParameters {
  readonly page: number;
  readonly pageSize: number;
  readonly filters: InstitutionFilters;
}

export interface InstitutionService {
  search(parameters: InstitutionQueryParameters, signal?: AbortSignal): Promise<PaginatedResponse<Institution>>;
  getFilterOptions(signal?: AbortSignal): Promise<InstitutionFilterOptions>;
  getById(id: string, signal?: AbortSignal): Promise<Institution>;
  create(input: InstitutionInput, signal?: AbortSignal): Promise<Institution>;
  update(id: string, input: InstitutionInput, signal?: AbortSignal): Promise<Institution>;
  remove(id: string, signal?: AbortSignal): Promise<void>;
}

export class DefaultInstitutionService implements InstitutionService {
  private readonly apiClient: ApiClient;

  constructor(apiClient: ApiClient) {
    this.apiClient = apiClient;
  }

  async search(
    parameters: InstitutionQueryParameters,
    signal?: AbortSignal,
  ): Promise<PaginatedResponse<Institution>> {
    const { page, pageSize, filters } = parameters;
    return this.apiClient.get<PaginatedResponse<Institution>>(
      '/api/institutions',
      {
        page,
        pageSize,
        name: filters.name.trim() === '' ? undefined : filters.name.trim(),
        category: filters.category.trim() === '' ? undefined : filters.category.trim(),
        statePower: filters.statePower.trim() === '' ? undefined : filters.statePower.trim(),
        sector: filters.sector.trim() === '' ? undefined : filters.sector.trim(),
      },
      signal,
    );
  }

  async getFilterOptions(signal?: AbortSignal): Promise<InstitutionFilterOptions> {
    return this.apiClient.get<InstitutionFilterOptions>(
      '/api/institutions/filter-options',
      undefined,
      signal,
    );
  }

  getById(id: string, signal?: AbortSignal): Promise<Institution> {
    return this.apiClient.get<Institution>(
      `/api/institutions/${encodeURIComponent(id)}`,
      undefined,
      signal,
    );
  }

  create(input: InstitutionInput, signal?: AbortSignal): Promise<Institution> {
    return this.apiClient.post<Institution, InstitutionInput>(
      '/api/institutions',
      input,
      signal,
    );
  }

  update(id: string, input: InstitutionInput, signal?: AbortSignal): Promise<Institution> {
    return this.apiClient.request<Institution>({
      method: 'PUT',
      path: `/api/institutions/${encodeURIComponent(id)}`,
      body: input,
      signal,
    });
  }

  async remove(id: string, signal?: AbortSignal): Promise<void> {
    await this.apiClient.request<void>({
      method: 'DELETE',
      path: `/api/institutions/${encodeURIComponent(id)}`,
      signal,
    });
  }
}
