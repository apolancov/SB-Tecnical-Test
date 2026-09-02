export interface Institution {
  readonly id: string;
  readonly name: string;
  readonly category: string;
  readonly statePower: string;
  readonly sector: string;
}

export interface InstitutionInput {
  readonly name: string;
  readonly category: string;
  readonly statePower: string;
  readonly sector: string;
}

export interface PaginatedResponse<T> {
  readonly items: ReadonlyArray<T>;
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  readonly totalPages: number;
}

export interface InstitutionFilters {
  readonly name: string;
  readonly category: string;
  readonly statePower: string;
  readonly sector: string;
}

export interface InstitutionQuery {
  readonly page: number;
  readonly pageSize: number;
  readonly filters: InstitutionFilters;
}

export interface InstitutionFilterOptions {
  readonly categories: ReadonlyArray<string>;
  readonly statePowers: ReadonlyArray<string>;
  readonly sectors: ReadonlyArray<string>;
}

export const EmptyInstitutionInput: InstitutionInput = {
  name: '',
  category: '',
  statePower: '',
  sector: '',
};
