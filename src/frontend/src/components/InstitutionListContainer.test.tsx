import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { InstitutionList } from './InstitutionList';
import { InstitutionListContainer } from './InstitutionListContainer';
import { renderWithProviders } from '../test/testUtils';
import type { Institution } from '../types/institution';

const sampleInstitutions: ReadonlyArray<Institution> = [
  {
    id: '1',
    name: 'Acuario Nacional',
    category: 'Organismo',
    statePower: 'Poder Ejecutivo',
    sector: 'Medio Ambiente',
  },
  {
    id: '2',
    name: 'Archivo General',
    category: 'Organismo',
    statePower: 'Poder Ejecutivo',
    sector: 'Cultura',
  },
];

describe('InstitutionList', () => {
  it('renders a row per institution with the four required columns', () => {
    renderWithProviders(<InstitutionList institutions={sampleInstitutions} />);

    expect(screen.getByRole('table', { name: /instituciones/i })).toBeInTheDocument();
    expect(screen.getByText('Acuario Nacional')).toBeInTheDocument();
    expect(screen.getByText('Archivo General')).toBeInTheDocument();
    expect(screen.getAllByRole('columnheader').map((header) => header.textContent)).toEqual([
      'Nombre',
      'Categoría',
      'Poder del Estado',
      'Sector',
    ]);
  });

  it('renders no rows when the list is empty', () => {
    renderWithProviders(<InstitutionList institutions={[]} />);
    expect(screen.queryAllByRole('row')).toHaveLength(1);
  });

  it('renders action buttons only when callbacks are provided', async () => {
    const onEdit = vi.fn();
    const onDelete = vi.fn();
    const user = userEvent.setup();

    renderWithProviders(
      <InstitutionList
        institutions={sampleInstitutions}
        onEdit={onEdit}
        onDelete={onDelete}
      />,
    );

    expect(
      screen.getByRole('button', { name: /editar acuario nacional/i }),
    ).toBeInTheDocument();

    await user.click(
      screen.getByRole('button', { name: /editar acuario nacional/i }),
    );
    expect(onEdit).toHaveBeenCalledWith(sampleInstitutions[0]);

    await user.click(
      screen.getByRole('button', { name: /eliminar archivo general/i }),
    );
    expect(onDelete).toHaveBeenCalledWith(sampleInstitutions[1]);
  });
});

describe('InstitutionListContainer', () => {
  it('shows a loading state while data is being fetched', () => {
    renderWithProviders(
      <InstitutionListContainer institutions={[]} loading error={null} />,
    );

    const loading = screen.getByRole('status');
    expect(loading).toHaveTextContent(/cargando instituciones/i);
  });

  it('shows an empty state when there are no results and not loading', () => {
    renderWithProviders(
      <InstitutionListContainer institutions={[]} loading={false} error={null} />,
    );

    const empty = screen.getByRole('status');
    expect(empty).toHaveTextContent(/no se encontraron instituciones/i);
  });

  it('shows an error state when fetching fails', () => {
    renderWithProviders(
      <InstitutionListContainer
        institutions={[]}
        loading={false}
        error="No se pudieron cargar las instituciones."
      />,
    );

    const errorBox = screen.getByRole('alert');
    expect(errorBox).toHaveTextContent(/no se pudieron cargar las instituciones/i);
  });

  it('renders the list in the success state', () => {
    renderWithProviders(
      <InstitutionListContainer
        institutions={sampleInstitutions}
        loading={false}
        error={null}
      />,
    );

    expect(screen.getByText('Acuario Nacional')).toBeInTheDocument();
    expect(screen.getByText('Archivo General')).toBeInTheDocument();
  });

  it('forwards edit and delete callbacks to the underlying list', async () => {
    const onEdit = vi.fn();
    const onDelete = vi.fn();
    const user = userEvent.setup();

    renderWithProviders(
      <InstitutionListContainer
        institutions={sampleInstitutions}
        loading={false}
        error={null}
        onEdit={onEdit}
        onDelete={onDelete}
      />,
    );

    await user.click(
      screen.getByRole('button', { name: /editar acuario nacional/i }),
    );
    expect(onEdit).toHaveBeenCalledWith(sampleInstitutions[0]);
  });
});
