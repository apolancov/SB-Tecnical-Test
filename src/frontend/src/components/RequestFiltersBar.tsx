'use client';

import type { FormEvent } from 'react';
import { useState } from 'react';
import type { AreaRecord, RequestTypeRecord } from '../types/catalog';
import {
  RequestPriority,
  RequestSortDirection,
  RequestSortField,
  RequestStatus,
  type RequestFilters,
} from '../types/request';
import {
  requestPriorityOptions,
  requestStatusOptions,
} from './RequestBadges';

export interface RequestFiltersBarProps {
  readonly filters: RequestFilters;
  readonly sortField: RequestSortField;
  readonly sortDirection: RequestSortDirection;
  readonly onSubmit: (next: {
    readonly filters: RequestFilters;
    readonly sortField: RequestSortField;
    readonly sortDirection: RequestSortDirection;
  }) => void;
  readonly areas?: ReadonlyArray<AreaRecord>;
  readonly requestTypes?: ReadonlyArray<RequestTypeRecord>;
  readonly areasLoading?: boolean;
  readonly requestTypesLoading?: boolean;
  readonly disabled?: boolean;
}

const emptyFilters: RequestFilters = {
  status: null,
  priority: null,
  areaId: null,
  requestTypeId: null,
  requesterId: null,
  responsibleId: null,
  fromDate: null,
  toDate: null,
  code: '',
  search: '',
};

function dateInputValue(value: string | null): string {
  if (value === null || value.length === 0) {
    return '';
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '';
  }
  const year = date.getUTCFullYear();
  const month = String(date.getUTCMonth() + 1).padStart(2, '0');
  const day = String(date.getUTCDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function parseDateInput(value: string): string | null {
  if (value.length === 0) {
    return null;
  }
  const date = new Date(`${value}T00:00:00.000Z`);
  if (Number.isNaN(date.getTime())) {
    return null;
  }
  return date.toISOString();
}

export function RequestFiltersBar({
  filters,
  sortField,
  sortDirection,
  onSubmit,
  areas,
  requestTypes,
  areasLoading = false,
  requestTypesLoading = false,
  disabled = false,
}: RequestFiltersBarProps) {
  const [status, setStatus] = useState<RequestStatus | ''>(
    filters.status ?? '',
  );
  const [priority, setPriority] = useState<RequestPriority | ''>(
    filters.priority ?? '',
  );
  const [areaId, setAreaId] = useState<string>(filters.areaId ?? '');
  const [requestTypeId, setRequestTypeId] = useState<string>(
    filters.requestTypeId ?? '',
  );
  const [code, setCode] = useState<string>(filters.code);
  const [search, setSearch] = useState<string>(filters.search);
  const [fromDate, setFromDate] = useState<string>(dateInputValue(filters.fromDate));
  const [toDate, setToDate] = useState<string>(dateInputValue(filters.toDate));
  const [sortFieldValue, setSortFieldValue] = useState<RequestSortField>(sortField);
  const [sortDirectionValue, setSortDirectionValue] = useState<RequestSortDirection>(
    sortDirection,
  );

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    onSubmit({
      filters: {
        status: status === '' ? null : (status as RequestStatus),
        priority: priority === '' ? null : (priority as RequestPriority),
        areaId: areaId.length === 0 ? null : areaId,
        requestTypeId: requestTypeId.length === 0 ? null : requestTypeId,
        requesterId: null,
        responsibleId: null,
        fromDate: parseDateInput(fromDate),
        toDate: parseDateInput(toDate),
        code,
        search,
      },
      sortField: sortFieldValue,
      sortDirection: sortDirectionValue,
    });
  };

  const handleReset = () => {
    setStatus('');
    setPriority('');
    setAreaId('');
    setRequestTypeId('');
    setCode('');
    setSearch('');
    setFromDate('');
    setToDate('');
    setSortFieldValue(RequestSortField.CreatedAt);
    setSortDirectionValue(RequestSortDirection.Descending);
    onSubmit({
      filters: emptyFilters,
      sortField: RequestSortField.CreatedAt,
      sortDirection: RequestSortDirection.Descending,
    });
  };

  const areaOptions = (areas ?? []).map((area) => ({ id: area.id, name: area.name }));
  const requestTypeOptions = (requestTypes ?? []).map((type) => ({
    id: type.id,
    name: type.name,
  }));

  return (
    <form
      onSubmit={handleSubmit}
      aria-label="Filtrar solicitudes"
      className="filters"
    >
      <div className="filters__row">
        <label className="field">
          <span className="field__label">Código</span>
          <input
            type="text"
            value={code}
            onChange={(event) => setCode(event.target.value)}
            disabled={disabled}
            autoComplete="off"
            data-testid="filter-request-code"
          />
        </label>
        <label className="field">
          <span className="field__label">Buscar</span>
          <input
            type="text"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            disabled={disabled}
            autoComplete="off"
            data-testid="filter-request-search"
          />
        </label>
        <label className="field">
          <span className="field__label">Estado</span>
          <select
            value={status}
            onChange={(event) => setStatus(event.target.value as RequestStatus | '')}
            disabled={disabled}
            data-testid="filter-request-status"
          >
            <option value="">Todos</option>
            {requestStatusOptions().map((option) => (
              <option key={option.value} value={option.value}>
                {option.name}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span className="field__label">Prioridad</span>
          <select
            value={priority}
            onChange={(event) => setPriority(event.target.value as RequestPriority | '')}
            disabled={disabled}
            data-testid="filter-request-priority"
          >
            <option value="">Todas</option>
            {requestPriorityOptions().map((option) => (
              <option key={option.value} value={option.value}>
                {option.name}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span className="field__label">Área</span>
          <select
            value={areaId}
            onChange={(event) => setAreaId(event.target.value)}
            disabled={disabled || areasLoading}
            data-testid="filter-request-area"
          >
            <option value="">Todas</option>
            {areaOptions.map((option) => (
              <option key={`area-${option.id}`} value={option.id}>
                {option.name}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span className="field__label">Tipo de solicitud</span>
          <select
            value={requestTypeId}
            onChange={(event) => setRequestTypeId(event.target.value)}
            disabled={disabled || requestTypesLoading}
            data-testid="filter-request-type"
          >
            <option value="">Todos</option>
            {requestTypeOptions.map((option) => (
              <option key={`type-${option.id}`} value={option.id}>
                {option.name}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span className="field__label">Desde</span>
          <input
            type="date"
            value={fromDate}
            onChange={(event) => setFromDate(event.target.value)}
            disabled={disabled}
            data-testid="filter-request-from"
          />
        </label>
        <label className="field">
          <span className="field__label">Hasta</span>
          <input
            type="date"
            value={toDate}
            onChange={(event) => setToDate(event.target.value)}
            disabled={disabled}
            data-testid="filter-request-to"
          />
        </label>
        <label className="field">
          <span className="field__label">Ordenar por</span>
          <select
            value={sortFieldValue}
            onChange={(event) => setSortFieldValue(event.target.value as RequestSortField)}
            disabled={disabled}
            data-testid="filter-request-sort-field"
          >
            <option value={RequestSortField.CreatedAt}>Fecha de creación</option>
            <option value={RequestSortField.Code}>Código</option>
            <option value={RequestSortField.Title}>Título</option>
            <option value={RequestSortField.Priority}>Prioridad</option>
            <option value={RequestSortField.Status}>Estado</option>
            <option value={RequestSortField.DueDate}>Fecha compromiso</option>
          </select>
        </label>
        <label className="field">
          <span className="field__label">Dirección</span>
          <select
            value={sortDirectionValue}
            onChange={(event) =>
              setSortDirectionValue(event.target.value as RequestSortDirection)
            }
            disabled={disabled}
            data-testid="filter-request-sort-direction"
          >
            <option value={RequestSortDirection.Ascending}>Ascendente</option>
            <option value={RequestSortDirection.Descending}>Descendente</option>
          </select>
        </label>
      </div>
      <div className="filters__actions">
        <button
          type="submit"
          className="button button--primary"
          disabled={disabled}
          data-testid="filter-request-submit"
        >
          Aplicar filtros
        </button>
        <button
          type="button"
          className="button"
          onClick={handleReset}
          disabled={disabled}
          data-testid="filter-request-reset"
        >
          Limpiar
        </button>
      </div>
    </form>
  );
}