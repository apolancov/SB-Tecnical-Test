'use client';

import type { FormEvent } from 'react';
import { useState } from 'react';
import { AutocompleteSelect } from './AutocompleteSelect';
import type { InstitutionFilterOptions, InstitutionFilters } from '../types/institution';

export interface InstitutionFiltersBarProps {
  readonly filters: InstitutionFilters;
  readonly onSubmit: (next: InstitutionFilters) => void;
  readonly options?: InstitutionFilterOptions;
  readonly optionsLoading?: boolean;
  readonly optionsError?: string | null;
  readonly disabled?: boolean;
}

const emptyFilters: InstitutionFilters = {
  name: '',
  category: '',
  statePower: '',
  sector: '',
};

const defaultFilterOptions: InstitutionFilterOptions = {
  categories: [],
  statePowers: [],
  sectors: [],
};

export function InstitutionFiltersBar({
  filters,
  onSubmit,
  options = defaultFilterOptions,
  optionsLoading = false,
  optionsError = null,
  disabled = false,
}: InstitutionFiltersBarProps) {
  const [name, setName] = useState<string>(filters.name);
  const [category, setCategory] = useState<string>(filters.category);
  const [statePower, setStatePower] = useState<string>(filters.statePower);
  const [sector, setSector] = useState<string>(filters.sector);

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    onSubmit({ name, category, statePower, sector });
  };

  const handleReset = () => {
    setName(emptyFilters.name);
    setCategory(emptyFilters.category);
    setStatePower(emptyFilters.statePower);
    setSector(emptyFilters.sector);
    onSubmit(emptyFilters);
  };

  const comboboxDisabled = disabled || (optionsLoading && optionsError !== null);

  return (
    <form
      onSubmit={handleSubmit}
      aria-label="Filtrar instituciones"
      className="filters"
    >
      <div className="filters__row">
        <label className="field">
          <span className="field__label">Nombre</span>
          <input
            type="text"
            value={name}
            onChange={(event) => setName(event.target.value)}
            disabled={disabled}
            autoComplete="off"
            data-testid="filter-name"
          />
        </label>
        <AutocompleteSelect
          id="filter-category"
          label="Categoría"
          value={category}
          options={options.categories}
          onChange={setCategory}
          disabled={comboboxDisabled}
          loading={optionsLoading}
          placeholder="Escribe o selecciona una categoría"
          testId="filter-category"
        />
        <AutocompleteSelect
          id="filter-state-power"
          label="Poder del Estado"
          value={statePower}
          options={options.statePowers}
          onChange={setStatePower}
          disabled={comboboxDisabled}
          loading={optionsLoading}
          placeholder="Escribe o selecciona un poder del estado"
          testId="filter-state-power"
        />
        <AutocompleteSelect
          id="filter-sector"
          label="Sector"
          value={sector}
          options={options.sectors}
          onChange={setSector}
          disabled={comboboxDisabled}
          loading={optionsLoading}
          placeholder="Escribe o selecciona un sector"
          testId="filter-sector"
        />
      </div>
      {optionsError !== null && (
        <p className="filters__options-error" role="status">
          {optionsError}
        </p>
      )}
      <div className="filters__actions">
        <button type="submit" className="button button--primary" disabled={disabled}>
          Buscar
        </button>
        <button
          type="button"
          className="button"
          onClick={handleReset}
          disabled={disabled}
        >
          Restablecer
        </button>
      </div>
    </form>
  );
}
