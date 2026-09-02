'use client';

import type { FormEvent } from 'react';
import { useState } from 'react';
import { AutocompleteSelect } from './AutocompleteSelect';
import type {
  InstitutionFilterOptions,
  InstitutionInput,
} from '../types/institution';

export interface InstitutionFormErrors {
  readonly name?: string;
  readonly category?: string;
  readonly statePower?: string;
  readonly sector?: string;
}

export interface InstitutionFormProps {
  readonly initialValue: InstitutionInput;
  readonly errors?: InstitutionFormErrors;
  readonly disabled?: boolean;
  readonly submitLabel: string;
  readonly onSubmit: (value: InstitutionInput) => void;
  readonly onCancel?: () => void;
  readonly testIdPrefix?: string;
  readonly filterOptions?: InstitutionFilterOptions;
  readonly filterOptionsLoading?: boolean;
}

const emptyFilterOptions: InstitutionFilterOptions = {
  categories: [],
  statePowers: [],
  sectors: [],
};

function isBlank(value: string): boolean {
  return value.trim().length === 0;
}

export function InstitutionForm({
  initialValue,
  errors,
  disabled = false,
  submitLabel,
  onSubmit,
  onCancel,
  testIdPrefix = 'institution-form',
  filterOptions = emptyFilterOptions,
  filterOptionsLoading = false,
}: InstitutionFormProps) {
  const [name, setName] = useState<string>(initialValue.name);
  const [category, setCategory] = useState<string>(initialValue.category);
  const [statePower, setStatePower] = useState<string>(initialValue.statePower);
  const [sector, setSector] = useState<string>(initialValue.sector);
  const [showLocalErrors, setShowLocalErrors] = useState<boolean>(false);

  const trimmedName = name.trim();
  const trimmedCategory = category.trim();
  const trimmedStatePower = statePower.trim();
  const trimmedSector = sector.trim();

  const localErrors: InstitutionFormErrors = {
    name: isBlank(name) ? 'El nombre es obligatorio.' : undefined,
    category: isBlank(category) ? 'La categoría es obligatoria.' : undefined,
    statePower: isBlank(statePower) ? 'El poder del estado es obligatorio.' : undefined,
    sector: isBlank(sector) ? 'El sector es obligatorio.' : undefined,
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (
      isBlank(name) ||
      isBlank(category) ||
      isBlank(statePower) ||
      isBlank(sector)
    ) {
      setShowLocalErrors(true);
      return;
    }

    onSubmit({
      name: trimmedName,
      category: trimmedCategory,
      statePower: trimmedStatePower,
      sector: trimmedSector,
    });
  };

  const showError = (field: keyof InstitutionFormErrors): string | undefined => {
    if (showLocalErrors && localErrors[field]) {
      return localErrors[field];
    }
    return errors?.[field];
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="institution-form"
      aria-label="Formulario de institución"
      noValidate
    >
      <div className="institution-form__row">
        <label className="field">
          <span className="field__label">Nombre</span>
          <input
            type="text"
            value={name}
            onChange={(event) => setName(event.target.value)}
            disabled={disabled}
            autoComplete="off"
            data-testid={`${testIdPrefix}-name`}
            aria-invalid={showError('name') ? 'true' : 'false'}
          />
          {showError('name') && (
            <span className="field__error" role="alert" data-testid={`${testIdPrefix}-name-error`}>
              {showError('name')}
            </span>
          )}
        </label>
      </div>
      <div className="institution-form__row">
        <AutocompleteSelect
          id={`${testIdPrefix}-category`}
          label="Categoría"
          value={category}
          options={filterOptions.categories}
          onChange={setCategory}
          disabled={disabled}
          loading={filterOptionsLoading}
          placeholder="Escribe o selecciona una categoría"
          invalid={showError('category') !== undefined}
          testId={`${testIdPrefix}-category`}
        />
        {showError('category') && (
          <span className="field__error" role="alert" data-testid={`${testIdPrefix}-category-error`}>
            {showError('category')}
          </span>
        )}
        <AutocompleteSelect
          id={`${testIdPrefix}-state-power`}
          label="Poder del Estado"
          value={statePower}
          options={filterOptions.statePowers}
          onChange={setStatePower}
          disabled={disabled}
          loading={filterOptionsLoading}
          placeholder="Escribe o selecciona un poder del estado"
          invalid={showError('statePower') !== undefined}
          testId={`${testIdPrefix}-state-power`}
        />
        {showError('statePower') && (
          <span className="field__error" role="alert" data-testid={`${testIdPrefix}-state-power-error`}>
            {showError('statePower')}
          </span>
        )}
        <AutocompleteSelect
          id={`${testIdPrefix}-sector`}
          label="Sector"
          value={sector}
          options={filterOptions.sectors}
          onChange={setSector}
          disabled={disabled}
          loading={filterOptionsLoading}
          placeholder="Escribe o selecciona un sector"
          invalid={showError('sector') !== undefined}
          testId={`${testIdPrefix}-sector`}
        />
        {showError('sector') && (
          <span className="field__error" role="alert" data-testid={`${testIdPrefix}-sector-error`}>
            {showError('sector')}
          </span>
        )}
      </div>
      <div className="institution-form__actions">
        <button
          type="submit"
          className="button button--primary"
          disabled={disabled}
          data-testid={`${testIdPrefix}-submit`}
        >
          {submitLabel}
        </button>
        {onCancel !== undefined && (
          <button
            type="button"
            className="button"
            onClick={onCancel}
            disabled={disabled}
            data-testid={`${testIdPrefix}-cancel`}
          >
            Cancelar
          </button>
        )}
      </div>
    </form>
  );
}