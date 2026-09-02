'use client';

import { useState, type FormEvent } from 'react';
import {
  AuditAction,
  AuditLogSortField,
  AuditLogSortDirection,
  AuditOutcome,
  type AuditLogFilters,
} from '../types/audit';

export interface AuditLogFiltersBarProps {
  readonly filters: AuditLogFilters;
  readonly sortField: AuditLogSortField;
  readonly sortDirection: AuditLogSortDirection;
  readonly disabled?: boolean;
  readonly onSubmit: (next: {
    readonly filters: AuditLogFilters;
    readonly sortField: AuditLogSortField;
    readonly sortDirection: AuditLogSortDirection;
  }) => void;
}

const ActionOptions: ReadonlyArray<{ value: AuditAction; label: string }> = [
  { value: AuditAction.LoginSucceeded, label: 'Inicio de sesión exitoso' },
  { value: AuditAction.LoginFailed, label: 'Inicio de sesión fallido' },
  { value: AuditAction.RequestCreated, label: 'Solicitud creada' },
  { value: AuditAction.RequestUpdated, label: 'Solicitud actualizada' },
  { value: AuditAction.RequestAssigned, label: 'Solicitud asignada' },
  { value: AuditAction.RequestStatusChanged, label: 'Estado de solicitud cambiado' },
  { value: AuditAction.RequestReopened, label: 'Solicitud reabierta' },
  { value: AuditAction.RequestCommentAdded, label: 'Comentario en solicitud' },
  { value: AuditAction.InstitutionCreated, label: 'Institución creada' },
  { value: AuditAction.InstitutionUpdated, label: 'Institución actualizada' },
  { value: AuditAction.InstitutionDeleted, label: 'Institución eliminada' },
  { value: AuditAction.AuthorizationDenied, label: 'Autorización denegada' },
];

const OutcomeOptions: ReadonlyArray<{ value: AuditOutcome; label: string }> = [
  { value: AuditOutcome.Success, label: 'Éxito' },
  { value: AuditOutcome.Failure, label: 'Fallo' },
  { value: AuditOutcome.Denied, label: 'Denegado' },
];

const SortFieldOptions: ReadonlyArray<{ value: AuditLogSortField; label: string }> = [
  { value: AuditLogSortField.Timestamp, label: 'Fecha' },
  { value: AuditLogSortField.Action, label: 'Acción' },
  { value: AuditLogSortField.Outcome, label: 'Resultado' },
  { value: AuditLogSortField.EntityType, label: 'Tipo de entidad' },
  { value: AuditLogSortField.ActorUserName, label: 'Usuario' },
];

const DefaultFilters: AuditLogFilters = {
  actorUserId: '',
  action: '',
  outcome: '',
  entityType: '',
  fromDate: '',
  toDate: '',
  search: '',
};

export function AuditLogFiltersBar({
  filters,
  sortField,
  sortDirection,
  disabled = false,
  onSubmit,
}: AuditLogFiltersBarProps) {
  const [actorUserId, setActorUserId] = useState<string>(filters.actorUserId);
  const [action, setAction] = useState<AuditAction | ''>(filters.action);
  const [outcome, setOutcome] = useState<AuditOutcome | ''>(filters.outcome);
  const [entityType, setEntityType] = useState<string>(filters.entityType);
  const [fromDate, setFromDate] = useState<string>(filters.fromDate);
  const [toDate, setToDate] = useState<string>(filters.toDate);
  const [search, setSearch] = useState<string>(filters.search);
  const [currentSortField, setCurrentSortField] = useState<AuditLogSortField>(sortField);
  const [currentSortDirection, setCurrentSortDirection] = useState<AuditLogSortDirection>(
    sortDirection,
  );

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    onSubmit({
      filters: {
        actorUserId: actorUserId.trim(),
        action,
        outcome,
        entityType: entityType.trim(),
        fromDate: fromDate.trim(),
        toDate: toDate.trim(),
        search: search.trim(),
      },
      sortField: currentSortField,
      sortDirection: currentSortDirection,
    });
  };

  const handleReset = () => {
    setActorUserId(DefaultFilters.actorUserId);
    setAction(DefaultFilters.action);
    setOutcome(DefaultFilters.outcome);
    setEntityType(DefaultFilters.entityType);
    setFromDate(DefaultFilters.fromDate);
    setToDate(DefaultFilters.toDate);
    setSearch(DefaultFilters.search);
    setCurrentSortField(AuditLogSortField.Timestamp);
    setCurrentSortDirection(AuditLogSortDirection.Descending);
    onSubmit({
      filters: { ...DefaultFilters },
      sortField: AuditLogSortField.Timestamp,
      sortDirection: AuditLogSortDirection.Descending,
    });
  };

  return (
    <form
      onSubmit={handleSubmit}
      aria-label="Filtrar bitácora de auditoría"
      className="filters"
    >
      <div className="filters__row">
        <label className="field">
          <span className="field__label">Búsqueda</span>
          <input
            type="search"
            value={search}
            placeholder="Detalles, identificador o usuario"
            onChange={(event) => setSearch(event.target.value)}
            disabled={disabled}
            data-testid="auditlog-filter-search"
          />
        </label>
        <label className="field">
          <span className="field__label">ID de usuario</span>
          <input
            type="text"
            value={actorUserId}
            placeholder="00000000-0000-0000-0000-000000000000"
            onChange={(event) => setActorUserId(event.target.value)}
            disabled={disabled}
            data-testid="auditlog-filter-actor-user-id"
          />
        </label>
        <label className="field">
          <span className="field__label">Acción</span>
          <select
            value={action}
            onChange={(event) => setAction(event.target.value as AuditAction | '')}
            disabled={disabled}
            data-testid="auditlog-filter-action"
          >
            <option value="">Todas</option>
            {ActionOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span className="field__label">Resultado</span>
          <select
            value={outcome}
            onChange={(event) => setOutcome(event.target.value as AuditOutcome | '')}
            disabled={disabled}
            data-testid="auditlog-filter-outcome"
          >
            <option value="">Todos</option>
            {OutcomeOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      </div>
      <div className="filters__row">
        <label className="field">
          <span className="field__label">Tipo de entidad</span>
          <input
            type="text"
            value={entityType}
            placeholder="Request, Institution, User, ..."
            onChange={(event) => setEntityType(event.target.value)}
            disabled={disabled}
            data-testid="auditlog-filter-entity-type"
          />
        </label>
        <label className="field">
          <span className="field__label">Desde</span>
          <input
            type="date"
            value={fromDate}
            onChange={(event) => setFromDate(event.target.value)}
            disabled={disabled}
            data-testid="auditlog-filter-from-date"
          />
        </label>
        <label className="field">
          <span className="field__label">Hasta</span>
          <input
            type="date"
            value={toDate}
            onChange={(event) => setToDate(event.target.value)}
            disabled={disabled}
            data-testid="auditlog-filter-to-date"
          />
        </label>
        <label className="field">
          <span className="field__label">Ordenar por</span>
          <select
            value={currentSortField}
            onChange={(event) => setCurrentSortField(event.target.value as AuditLogSortField)}
            disabled={disabled}
            data-testid="auditlog-sort-field"
          >
            {SortFieldOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span className="field__label">Dirección</span>
          <select
            value={currentSortDirection}
            onChange={(event) =>
              setCurrentSortDirection(event.target.value as AuditLogSortDirection)
            }
            disabled={disabled}
            data-testid="auditlog-sort-direction"
          >
            <option value={AuditLogSortDirection.Descending}>Descendente</option>
            <option value={AuditLogSortDirection.Ascending}>Ascendente</option>
          </select>
        </label>
      </div>
      <div className="filters__actions">
        <button
          type="submit"
          className="button button--primary"
          disabled={disabled}
          data-testid="auditlog-submit"
        >
          Buscar
        </button>
        <button
          type="button"
          className="button"
          onClick={handleReset}
          disabled={disabled}
          data-testid="auditlog-reset"
        >
          Restablecer
        </button>
      </div>
    </form>
  );
}