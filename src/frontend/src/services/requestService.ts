import type { ApiClient } from './api';
import type { PaginatedResponse } from '../types/pagination';
import {
  type AddCommentInput,
  type AssignRequestInput,
  type ChangeRequestStatusInput,
  type CreateRequestInput,
  type ReopenRequestInput,
  type RequestComment,
  type RequestDetail,
  type RequestFilters,
  type RequestQuery,
  type RequestRecord,
  type UpdateRequestInput,
} from '../types/request';

export interface RequestService {
  search(query: RequestQuery, signal?: AbortSignal): Promise<PaginatedResponse<RequestRecord>>;
  getById(id: string, signal?: AbortSignal): Promise<RequestDetail>;
  create(input: CreateRequestInput, signal?: AbortSignal): Promise<RequestRecord>;
  update(id: string, input: UpdateRequestInput, signal?: AbortSignal): Promise<RequestRecord>;
  changeStatus(
    id: string,
    input: ChangeRequestStatusInput,
    signal?: AbortSignal,
  ): Promise<RequestRecord>;
  assign(id: string, input: AssignRequestInput, signal?: AbortSignal): Promise<RequestRecord>;
  reopen(id: string, input: ReopenRequestInput, signal?: AbortSignal): Promise<RequestRecord>;
  addComment(
    id: string,
    input: AddCommentInput,
    signal?: AbortSignal,
  ): Promise<RequestComment>;
}

function buildFilters(filters: RequestFilters): Record<string, string | number | undefined> {
  return {
    status: filters.status ?? undefined,
    priority: filters.priority ?? undefined,
    areaId: filters.areaId && filters.areaId.length > 0 ? filters.areaId : undefined,
    requestTypeId:
      filters.requestTypeId && filters.requestTypeId.length > 0
        ? filters.requestTypeId
        : undefined,
    requesterId:
      filters.requesterId && filters.requesterId.length > 0 ? filters.requesterId : undefined,
    responsibleId:
      filters.responsibleId && filters.responsibleId.length > 0
        ? filters.responsibleId
        : undefined,
    fromDate: filters.fromDate && filters.fromDate.length > 0 ? filters.fromDate : undefined,
    toDate: filters.toDate && filters.toDate.length > 0 ? filters.toDate : undefined,
    code: filters.code.trim() === '' ? undefined : filters.code.trim(),
    search: filters.search.trim() === '' ? undefined : filters.search.trim(),
  };
}

export class DefaultRequestService implements RequestService {
  private readonly apiClient: ApiClient;

  constructor(apiClient: ApiClient) {
    this.apiClient = apiClient;
  }

  search(query: RequestQuery, signal?: AbortSignal): Promise<PaginatedResponse<RequestRecord>> {
    return this.apiClient.get<PaginatedResponse<RequestRecord>>(
      '/api/solicitudes',
      {
        page: query.page,
        pageSize: query.pageSize,
        sortBy: query.sortField,
        sortDirection: query.sortDirection,
        ...buildFilters(query.filters),
      },
      signal,
    );
  }

  getById(id: string, signal?: AbortSignal): Promise<RequestDetail> {
    return this.apiClient.get<RequestDetail>(
      `/api/solicitudes/${encodeURIComponent(id)}`,
      undefined,
      signal,
    );
  }

  create(input: CreateRequestInput, signal?: AbortSignal): Promise<RequestRecord> {
    return this.apiClient.post<RequestRecord, CreateRequestInput>(
      '/api/solicitudes',
      input,
      signal,
    );
  }

  update(
    id: string,
    input: UpdateRequestInput,
    signal?: AbortSignal,
  ): Promise<RequestRecord> {
    return this.apiClient.request<RequestRecord>({
      method: 'PATCH',
      path: `/api/solicitudes/${encodeURIComponent(id)}`,
      body: input,
      signal,
    });
  }

  changeStatus(
    id: string,
    input: ChangeRequestStatusInput,
    signal?: AbortSignal,
  ): Promise<RequestRecord> {
    return this.apiClient.request<RequestRecord>({
      method: 'PATCH',
      path: `/api/solicitudes/${encodeURIComponent(id)}/estado`,
      body: input,
      signal,
    });
  }

  assign(
    id: string,
    input: AssignRequestInput,
    signal?: AbortSignal,
  ): Promise<RequestRecord> {
    return this.apiClient.request<RequestRecord>({
      method: 'PATCH',
      path: `/api/solicitudes/${encodeURIComponent(id)}/asignacion`,
      body: input,
      signal,
    });
  }

  reopen(
    id: string,
    input: ReopenRequestInput,
    signal?: AbortSignal,
  ): Promise<RequestRecord> {
    return this.apiClient.request<RequestRecord>({
      method: 'POST',
      path: `/api/solicitudes/${encodeURIComponent(id)}/reapertura`,
      body: input,
      signal,
    });
  }

  addComment(
    id: string,
    input: AddCommentInput,
    signal?: AbortSignal,
  ): Promise<RequestComment> {
    return this.apiClient.request<RequestComment>({
      method: 'POST',
      path: `/api/solicitudes/${encodeURIComponent(id)}/comentarios`,
      body: input,
      signal,
    });
  }
}