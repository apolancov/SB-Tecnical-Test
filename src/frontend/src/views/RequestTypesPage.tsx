'use client';

import { useService } from '../hooks/ServiceContext';
import { useCatalogRequestTypes } from '../hooks/useCatalogLookups';

export function RequestTypesPage() {
  const { catalogService } = useService();
  const { requestTypes, loading, error, refresh } = useCatalogRequestTypes({
    service: catalogService,
  });

  return (
    <section className="page" data-testid="request-types-page">
      <header className="page__header">
        <div className="page__header-row">
          <div>
            <h1 className="page__title">Tipos de solicitud</h1>
            <p className="page__subtitle">Catálogo de tipos de solicitud activos.</p>
          </div>
        </div>
      </header>

      <div className="state state--empty">
        <p>
          La creación y edición de tipos de solicitud aún no está disponible desde el frontend.
        </p>
        <p className="field__hint">
          El backend expone únicamente el listado de tipos de solicitud activos. Las
          operaciones de alta, baja y modificación se incorporarán cuando se sumen los
          endpoints correspondientes.
        </p>
      </div>

      {loading ? (
        <div role="status" className="state state--loading">
          Cargando tipos de solicitud...
        </div>
      ) : error !== null ? (
        <div role="alert" className="state state--error">
          <p>{error}</p>
          <button type="button" className="button" onClick={refresh}>
            Reintentar
          </button>
        </div>
      ) : requestTypes.length === 0 ? (
        <div
          role="status"
          className="state state--empty"
          data-testid="request-types-empty"
        >
          No hay tipos de solicitud activos registrados.
        </div>
      ) : (
        <table
          className="request-table"
          aria-label="Tipos de solicitud activos"
          data-testid="request-types-table"
        >
          <thead>
            <tr>
              <th scope="col">Nombre</th>
              <th scope="col">Descripción</th>
              <th scope="col">Estado</th>
            </tr>
          </thead>
          <tbody>
            {requestTypes.map((type) => (
              <tr key={type.id} data-testid={`request-types-row-${type.id}`}>
                <td>{type.name}</td>
                <td>{type.description.length === 0 ? '—' : type.description}</td>
                <td>
                  <span className="badge badge--status-Resolved">Activo</span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}