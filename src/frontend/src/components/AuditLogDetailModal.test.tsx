import { describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AuditLogDetailModal } from './AuditLogDetailModal';
import { renderWithProviders } from '../test/testUtils';
import {
  AuditAction,
  AuditOutcome,
  type AuditLogEntry,
} from '../types/audit';

const sampleEntry: AuditLogEntry = {
  id: '11111111-1111-1111-1111-111111111111',
  timestamp: '2026-09-02T10:00:00.000Z',
  action: AuditAction.RequestCreated,
  outcome: AuditOutcome.Success,
  entityType: 'Request',
  entityId: 'req-1',
  details: 'Request SOL-2026-0001 created.',
  ipAddress: '127.0.0.1',
  actorUserId: 'admin-id',
  actorUserName: 'admin',
};

describe('AuditLogDetailModal', () => {
  it('renders nothing when open is false', () => {
    renderWithProviders(
      <AuditLogDetailModal
        entry={sampleEntry}
        open={false}
        loading={false}
        error={null}
        onClose={vi.fn()}
      />,
    );

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('renders every field of the entry when open is true', () => {
    renderWithProviders(
      <AuditLogDetailModal
        entry={sampleEntry}
        open
        loading={false}
        error={null}
        onClose={vi.fn()}
      />,
    );

    const detail = screen.getByTestId('audit-log-detail');
    expect(detail).toHaveTextContent(/Solicitud creada/);
    expect(detail).toHaveTextContent(/Request/);
    expect(detail).toHaveTextContent(/req-1/);
    expect(detail).toHaveTextContent(/admin/);
    expect(detail).toHaveTextContent(/127\.0\.0\.1/);
    expect(detail).toHaveTextContent(/SOL-2026-0001 created/);
    expect(detail).toHaveTextContent(sampleEntry.id);
  });

  it('shows the loading state when loading is true', () => {
    renderWithProviders(
      <AuditLogDetailModal
        entry={sampleEntry}
        open
        loading
        error={null}
        onClose={vi.fn()}
      />,
    );

    expect(screen.getByTestId('audit-log-detail-loading')).toHaveTextContent(
      /cargando detalle/i,
    );
  });

  it('shows the error message when error is provided', () => {
    renderWithProviders(
      <AuditLogDetailModal
        entry={sampleEntry}
        open
        loading={false}
        error="No se pudo cargar el detalle."
        onClose={vi.fn()}
      />,
    );

    expect(screen.getByTestId('audit-log-detail-error')).toHaveTextContent(
      /no se pudo cargar/i,
    );
  });

  it('calls onClose when the close button is clicked', async () => {
    const onClose = vi.fn();
    const user = userEvent.setup();

    renderWithProviders(
      <AuditLogDetailModal
        entry={sampleEntry}
        open
        loading={false}
        error={null}
        onClose={onClose}
      />,
    );

    await user.click(screen.getByTestId('audit-log-detail-close'));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('calls onClose when the backdrop receives a click', async () => {
    const onClose = vi.fn();
    const user = userEvent.setup();

    renderWithProviders(
      <AuditLogDetailModal
        entry={sampleEntry}
        open
        loading={false}
        error={null}
        onClose={onClose}
      />,
    );

    const backdrop = screen.getByRole('presentation');
    await user.click(backdrop);
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('omits the actor and IP rows when they are null', () => {
    const entryWithoutActorOrIp: AuditLogEntry = {
      ...sampleEntry,
      actorUserId: null,
      actorUserName: null,
      ipAddress: null,
      details: null,
    };

    renderWithProviders(
      <AuditLogDetailModal
        entry={entryWithoutActorOrIp}
        open
        loading={false}
        error={null}
        onClose={vi.fn()}
      />,
    );

    expect(screen.queryByText('ID de usuario')).not.toBeInTheDocument();
    expect(screen.queryByText('Dirección IP')).not.toBeInTheDocument();
    expect(screen.queryByText('Detalles')).not.toBeInTheDocument();
  });

  it('closes when the Escape key is pressed', async () => {
    const onClose = vi.fn();
    const user = userEvent.setup();

    renderWithProviders(
      <AuditLogDetailModal
        entry={sampleEntry}
        open
        loading={false}
        error={null}
        onClose={onClose}
      />,
    );

    await user.keyboard('{Escape}');

    await waitFor(() => {
      expect(onClose).toHaveBeenCalled();
    });
  });
});