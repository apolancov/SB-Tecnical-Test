'use client';

import type { Institution } from '../types/institution';
import { InstitutionList } from './InstitutionList';

export interface InstitutionListContainerProps {
  readonly institutions: ReadonlyArray<Institution>;
  readonly loading: boolean;
  readonly error: string | null;
  readonly onRetry?: () => void;
  readonly onEdit?: (institution: Institution) => void;
  readonly onDelete?: (institution: Institution) => void;
}

export function InstitutionListContainer(props: InstitutionListContainerProps) {
  const { institutions, loading, error, onRetry, onEdit, onDelete } = props;

  if (loading) {
    return (
      <div role="status" aria-live="polite" className="state state--loading">
        Cargando instituciones...
      </div>
    );
  }

  if (error !== null) {
    return (
      <div role="alert" className="state state--error">
        <p>{error}</p>
        {onRetry !== undefined && (
          <button type="button" onClick={onRetry} className="button">
            Reintentar
          </button>
        )}
      </div>
    );
  }

  if (institutions.length === 0) {
    return (
      <div role="status" className="state state--empty">
        No se encontraron instituciones.
      </div>
    );
  }

  return <InstitutionList institutions={institutions} onEdit={onEdit} onDelete={onDelete} />;
}
