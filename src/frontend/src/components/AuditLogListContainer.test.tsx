import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AuditLogList } from './AuditLogList';
import { AuditLogListContainer } from './AuditLogListContainer';
import { renderWithProviders } from '../test/testUtils';
import {
  AuditAction,
  AuditOutcome,
  type AuditLogEntry,
} from '../types/audit';

const sampleEntries: ReadonlyArray<AuditLogEntry> = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    timestamp: '2026-09-02T10:00:00.000Z',
    action: AuditAction.LoginSucceeded,
    outcome: AuditOutcome.Success,
    entityType: 'User',
    entityId: 'user-1',
    details: 'Login OK',
    ipAddress: '127.0.0.1',
    actorUserId: 'user-1',
    actorUserName: 'admin',
  },
  {
    id: '22222222-2222-2222-2222-222222222222',
    timestamp: '2026-09-02T11:00:00.000Z',
    action: AuditAction.AuthorizationDenied,
    outcome: AuditOutcome.Denied,
    entityType: 'AdminOnly',
    entityId: 'GET /api/auditoria',
    details: 'Acceso denegado',
    ipAddress: '10.0.0.1',
    actorUserId: 'user-2',
    actorUserName: 'guest',
  },
];

describe('AuditLogList', () => {
  it('renders one row per entry with the required columns', () => {
    renderWithProviders(<AuditLogList entries={sampleEntries} />);

    expect(
      screen.getByRole('table', { name: /bitácora de auditoría/i }),
    ).toBeInTheDocument();

    const headers = screen
      .getAllByRole('columnheader')
      .map((header) => header.textContent);
    expect(headers).toEqual([
      'Fecha',
      'Acción',
      'Resultado',
      'Entidad',
      'Usuario',
      'IP',
    ]);
  });

  it('renders an empty body when no entries are provided', () => {
    renderWithProviders(<AuditLogList entries={[]} />);
    const rows = screen.queryAllByRole('row');
    expect(rows).toHaveLength(1);
  });

  it('forwards the click on the detail button to the onSelect callback', async () => {
    const user = userEvent.setup();
    const target = sampleEntries[1];
    if (target === undefined) {
      throw new Error('expected at least one sample entry');
    }

    let selected: AuditLogEntry | null = null;
    renderWithProviders(
      <AuditLogList
        entries={sampleEntries}
        onSelect={(entry) => {
          selected = entry;
        }}
      />,
    );

    await user.click(screen.getByTestId(`audit-log-detail-${target.id}`));
    expect(selected).toEqual(target);
  });

  it('hides the detail column when onSelect is not provided', () => {
    renderWithProviders(<AuditLogList entries={sampleEntries} />);

    expect(screen.queryByText('Detalle')).not.toBeInTheDocument();
    expect(
      screen.queryByTestId(`audit-log-detail-${sampleEntries[0]?.id ?? ''}`),
    ).not.toBeInTheDocument();
  });
});

describe('AuditLogListContainer', () => {
  it('shows a loading state while data is being fetched', () => {
    renderWithProviders(
      <AuditLogListContainer entries={[]} loading error={null} />,
    );

    const loading = screen.getByRole('status');
    expect(loading).toHaveTextContent(/cargando bitácora/i);
  });

  it('shows an empty state when there are no results', () => {
    renderWithProviders(
      <AuditLogListContainer entries={[]} loading={false} error={null} />,
    );

    const empty = screen.getByRole('status');
    expect(empty).toHaveTextContent(/no hay entradas/i);
  });

  it('shows an error state when fetching fails', () => {
    renderWithProviders(
      <AuditLogListContainer
        entries={[]}
        loading={false}
        error="No se pudo cargar la bitácora de auditoría."
      />,
    );

    const alert = screen.getByRole('alert');
    expect(alert).toHaveTextContent(/no se pudo cargar la bitácora/i);
  });

  it('renders the list in the success state', () => {
    renderWithProviders(
      <AuditLogListContainer
        entries={sampleEntries}
        loading={false}
        error={null}
      />,
    );

    expect(screen.getByText('Inicio de sesión exitoso')).toBeInTheDocument();
    expect(screen.getByText('Autorización denegada')).toBeInTheDocument();
  });
});