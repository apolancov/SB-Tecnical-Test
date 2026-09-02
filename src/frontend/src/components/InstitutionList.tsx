'use client';

import type { Institution } from '../types/institution';

export interface InstitutionListProps {
  readonly institutions: ReadonlyArray<Institution>;
  readonly onEdit?: (institution: Institution) => void;
  readonly onDelete?: (institution: Institution) => void;
}

export function InstitutionList({
  institutions,
  onEdit,
  onDelete,
}: InstitutionListProps) {
  const hasActions = onEdit !== undefined || onDelete !== undefined;

  return (
    <table className="institution-table" aria-label="Instituciones gubernamentales">
      <thead>
        <tr>
          <th scope="col">Nombre</th>
          <th scope="col">Categoría</th>
          <th scope="col">Poder del Estado</th>
          <th scope="col">Sector</th>
          {hasActions && (
            <th scope="col" className="institution-table__actions-column">
              Acciones
            </th>
          )}
        </tr>
      </thead>
      <tbody>
        {institutions.map((institution) => (
          <tr key={institution.id} data-testid={`institution-row-${institution.id}`}>
            <td>{institution.name}</td>
            <td>{institution.category}</td>
            <td>{institution.statePower}</td>
            <td>{institution.sector}</td>
            {hasActions && (
              <td className="institution-table__actions">
                {onEdit !== undefined && (
                  <button
                    type="button"
                    className="button button--small"
                    onClick={() => onEdit(institution)}
                    data-testid={`institution-edit-${institution.id}`}
                    aria-label={`Editar ${institution.name}`}
                  >
                    Editar
                  </button>
                )}
                {onDelete !== undefined && (
                  <button
                    type="button"
                    className="button button--small button--danger"
                    onClick={() => onDelete(institution)}
                    data-testid={`institution-delete-${institution.id}`}
                    aria-label={`Eliminar ${institution.name}`}
                  >
                    Eliminar
                  </button>
                )}
              </td>
            )}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
