import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { InstitutionForm } from './InstitutionForm';
import type {
  InstitutionFilterOptions,
  InstitutionInput,
} from '../types/institution';

const sampleValue: InstitutionInput = {
  name: 'Sample Name',
  category: 'Sample Category',
  statePower: 'Poder Ejecutivo',
  sector: 'Sample Sector',
};

const sampleFilterOptions: InstitutionFilterOptions = {
  categories: ['Ministerio', 'Universidad'],
  statePowers: ['Poder Ejecutivo', 'Poder Legislativo'],
  sectors: ['Hacienda', 'Cultura'],
};

describe('InstitutionForm', () => {
  it('renders the four labeled inputs with the initial value', () => {
    render(
      <InstitutionForm
        initialValue={sampleValue}
        submitLabel="Guardar"
        onSubmit={() => undefined}
      />,
    );

    expect(screen.getByLabelText(/nombre/i)).toHaveValue('Sample Name');
    expect(screen.getByLabelText(/categor/i)).toHaveValue('Sample Category');
    expect(screen.getByLabelText(/poder/i)).toHaveValue('Poder Ejecutivo');
    expect(screen.getByLabelText(/sector/i)).toHaveValue('Sample Sector');
    expect(screen.getByRole('button', { name: /guardar/i })).toBeInTheDocument();
  });

  it('submits a trimmed payload and calls onSubmit', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();

    render(
      <InstitutionForm
        initialValue={{ name: '', category: '', statePower: '', sector: '' }}
        submitLabel="Guardar"
        onSubmit={onSubmit}
      />,
    );

    await user.type(screen.getByLabelText(/nombre/i), '  Padded Name  ');
    await user.type(screen.getByLabelText(/categor/i), '  Padded Category ');
    await user.type(screen.getByLabelText(/poder/i), '  Poder Ejecutivo ');
    await user.type(screen.getByLabelText(/sector/i), ' Sector  ');

    await user.click(screen.getByRole('button', { name: /guardar/i }));

    expect(onSubmit).toHaveBeenCalledWith({
      name: 'Padded Name',
      category: 'Padded Category',
      statePower: 'Poder Ejecutivo',
      sector: 'Sector',
    });
  });

  it('shows local validation errors and prevents submission when fields are blank', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();

    render(
      <InstitutionForm
        initialValue={{ name: '', category: '', statePower: '', sector: '' }}
        submitLabel="Guardar"
        onSubmit={onSubmit}
      />,
    );

    await user.click(screen.getByRole('button', { name: /guardar/i }));

    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getAllByRole('alert')).not.toHaveLength(0);
    expect(screen.getByText(/el nombre es obligatorio/i)).toBeInTheDocument();
  });

  it('surfaces external field errors when provided', () => {
    render(
      <InstitutionForm
        initialValue={{ name: 'A', category: 'B', statePower: 'C', sector: 'D' }}
        errors={{ name: 'Nombre duplicado.' }}
        submitLabel="Guardar"
        onSubmit={() => undefined}
      />,
    );

    expect(screen.getByText('Nombre duplicado.')).toBeInTheDocument();
  });

  it('renders the cancel button and invokes onCancel when provided', async () => {
    const onCancel = vi.fn();
    const user = userEvent.setup();

    render(
      <InstitutionForm
        initialValue={sampleValue}
        submitLabel="Guardar"
        onSubmit={() => undefined}
        onCancel={onCancel}
      />,
    );

    await user.click(screen.getByRole('button', { name: /cancelar/i }));

    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  it('disables inputs and submit when disabled', () => {
    render(
      <InstitutionForm
        initialValue={sampleValue}
        submitLabel="Guardar"
        onSubmit={() => undefined}
        disabled
      />,
    );

    expect(screen.getByLabelText(/nombre/i)).toBeDisabled();
    expect(screen.getByRole('button', { name: /guardar/i })).toBeDisabled();
  });

  it('lets the user pick a classification from the autocomplete options', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();

    render(
      <InstitutionForm
        initialValue={{ name: 'A', category: '', statePower: '', sector: '' }}
        submitLabel="Guardar"
        onSubmit={onSubmit}
        filterOptions={sampleFilterOptions}
        filterOptionsLoading={false}
      />,
    );

    const categoryInput = screen.getByLabelText(/categor/i);
    await user.click(categoryInput);
    await user.click(screen.getByRole('option', { name: 'Ministerio' }));

    expect(categoryInput).toHaveValue('Ministerio');
  });

  it('renders the classification fields as comboboxes backed by the filter options', () => {
    render(
      <InstitutionForm
        initialValue={sampleValue}
        submitLabel="Guardar"
        onSubmit={() => undefined}
        filterOptions={sampleFilterOptions}
      />,
    );

    expect(screen.getByLabelText(/categor/i)).toHaveAttribute('role', 'combobox');
    expect(screen.getByLabelText(/poder/i)).toHaveAttribute('role', 'combobox');
    expect(screen.getByLabelText(/sector/i)).toHaveAttribute('role', 'combobox');
  });

  it('sets aria-invalid on the classification combobox when an error is provided', () => {
    render(
      <InstitutionForm
        initialValue={sampleValue}
        submitLabel="Guardar"
        onSubmit={() => undefined}
        errors={{ category: 'La categoría es obligatoria.' }}
        filterOptions={sampleFilterOptions}
      />,
    );

    expect(screen.getByLabelText(/categor/i)).toHaveAttribute('aria-invalid', 'true');
    expect(screen.getByTestId('institution-form-category-error')).toHaveTextContent(
      'La categoría es obligatoria.',
    );
  });
});
