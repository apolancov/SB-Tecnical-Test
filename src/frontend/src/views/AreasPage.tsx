'use client';

import { useService } from '../hooks/ServiceContext';
import { useCatalogAreas } from '../hooks/useCatalogLookups';

export function AreasPage() {
  const { catalogService } = useService();
  const { areas, loading, error, refresh } = useCatalogAreas({ service: catalogService });

  return (
    <section className="page" data-testid="areas-page">
      <header className="page__header">
        <div className="page__header-row">
          <div>
            <h1 className="page__title">Áreas</h1>
            <p className="page__subtitle">Catálogo de áreas registradas.</p>
          </div>
        </div>
      </header>

      <div className="state state--empty">
        <p>
          La creación y edición de áreas aún no está disponible desde el frontend.
        </p>
        <p className="field__hint">
          El backend expone únicamente el listado de áreas activas. Las operaciones
          de alta, baja y modificación se incorporarán cuando se sumen los endpoints
          correspondientes.
        </p>
      </div>

      {loading ? (
        <div role="status" className="state state--loading">
          Cargando áreas activas...
        </div>
      ) : error !== null ? (
        <div role="alert" className="state state--error">
          <p>{error}</p>
          <button type="button" className="button" onClick={refresh}>
            Reintentar
          </button>
        </div>
      ) : areas.length === 0 ? (
        <div role="status" className="state state--empty" data-testid="areas-empty">
          No hay áreas activas registradas.
        </div>
      ) : (
        <table
          className="request-table"
          aria-label="Áreas activas"
          data-testid="areas-table"
        >
          <thead>
            <tr>
              <th scope="col">Nombre</th>
              <th scope="col">Estado</th>
            </tr>
          </thead>
          <tbody>
            {areas.map((area) => (
              <tr key={area.id} data-testid={`areas-row-${area.id}`}>
                <td>{area.name}</td>
                <td>
                  <span className="badge badge--status-Resolved">Activa</span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}