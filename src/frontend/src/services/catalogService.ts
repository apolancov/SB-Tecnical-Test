import type { ApiClient } from './api';
import type { AreaRecord, RequestTypeRecord } from '../types/catalog';

export interface StaffCandidate {
  readonly id: string;
  readonly username: string;
  readonly email: string;
  readonly role: string;
}

export interface CatalogService {
  listAreas(signal?: AbortSignal): Promise<ReadonlyArray<AreaRecord>>;
  listRequestTypes(signal?: AbortSignal): Promise<ReadonlyArray<RequestTypeRecord>>;
  listStaffCandidates(signal?: AbortSignal): Promise<ReadonlyArray<StaffCandidate>>;
}

export class DefaultCatalogService implements CatalogService {
  private readonly apiClient: ApiClient;

  constructor(apiClient: ApiClient) {
    this.apiClient = apiClient;
  }

  listAreas(signal?: AbortSignal): Promise<ReadonlyArray<AreaRecord>> {
    return this.apiClient.get<ReadonlyArray<AreaRecord>>(
      '/api/catalogos/areas',
      undefined,
      signal,
    );
  }

  listRequestTypes(signal?: AbortSignal): Promise<ReadonlyArray<RequestTypeRecord>> {
    return this.apiClient.get<ReadonlyArray<RequestTypeRecord>>(
      '/api/catalogos/tipos-solicitud',
      undefined,
      signal,
    );
  }

  listStaffCandidates(signal?: AbortSignal): Promise<ReadonlyArray<StaffCandidate>> {
    return this.apiClient.get<ReadonlyArray<StaffCandidate>>(
      '/api/catalogos/responsables',
      undefined,
      signal,
    );
  }
}