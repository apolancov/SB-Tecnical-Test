import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AuditLogFiltersBar } from './AuditLogFiltersBar';
import { renderWithProviders } from '../test/testUtils';
import {
  AuditAction,
  AuditLogSortDirection,
  AuditLogSortField,
  AuditOutcome,
  EmptyAuditLogFilters,
} from '../types/audit';

describe('AuditLogFiltersBar', () => {
  it('calls onSubmit with the current filter values when the form is submitted', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();

    renderWithProviders(
      <AuditLogFiltersBar
        filters={{
          ...EmptyAuditLogFilters,
          search: 'admin',
        }}
        sortField={AuditLogSortField.Timestamp}
        sortDirection={AuditLogSortDirection.Descending}
        onSubmit={onSubmit}
      />,
    );

    await user.selectOptions(
      screen.getByTestId('auditlog-filter-action'),
      AuditAction.LoginFailed,
    );
    await user.selectOptions(
      screen.getByTestId('auditlog-filter-outcome'),
      AuditOutcome.Failure,
    );
    await user.clear(screen.getByTestId('auditlog-filter-search'));
    await user.type(screen.getByTestId('auditlog-filter-search'), 'login');
    await user.click(screen.getByTestId('auditlog-submit'));

    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        filters: expect.objectContaining({
          action: AuditAction.LoginFailed,
          outcome: AuditOutcome.Failure,
          search: 'login',
        }),
      }),
    );
  });

  it('passes the new sort field and direction to onSubmit', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();

    renderWithProviders(
      <AuditLogFiltersBar
        filters={EmptyAuditLogFilters}
        sortField={AuditLogSortField.Timestamp}
        sortDirection={AuditLogSortDirection.Descending}
        onSubmit={onSubmit}
      />,
    );

    await user.selectOptions(
      screen.getByTestId('auditlog-sort-field'),
      AuditLogSortField.Action,
    );
    await user.selectOptions(
      screen.getByTestId('auditlog-sort-direction'),
      AuditLogSortDirection.Ascending,
    );
    await user.click(screen.getByTestId('auditlog-submit'));

    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        sortField: AuditLogSortField.Action,
        sortDirection: AuditLogSortDirection.Ascending,
      }),
    );
  });

  it('resets the local state and emits empty filters when the reset button is clicked', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();

    renderWithProviders(
      <AuditLogFiltersBar
        filters={{
          ...EmptyAuditLogFilters,
          action: AuditAction.LoginFailed,
          search: 'foo',
        }}
        sortField={AuditLogSortField.Action}
        sortDirection={AuditLogSortDirection.Ascending}
        onSubmit={onSubmit}
      />,
    );

    await user.click(screen.getByTestId('auditlog-reset'));

    expect(onSubmit).toHaveBeenCalledWith({
      filters: { ...EmptyAuditLogFilters },
      sortField: AuditLogSortField.Timestamp,
      sortDirection: AuditLogSortDirection.Descending,
    });
  });

  it('disables all fields when disabled is true', () => {
    renderWithProviders(
      <AuditLogFiltersBar
        filters={EmptyAuditLogFilters}
        sortField={AuditLogSortField.Timestamp}
        sortDirection={AuditLogSortDirection.Descending}
        disabled
        onSubmit={vi.fn()}
      />,
    );

    expect(screen.getByTestId('auditlog-filter-search')).toBeDisabled();
    expect(screen.getByTestId('auditlog-filter-action')).toBeDisabled();
    expect(screen.getByTestId('auditlog-submit')).toBeDisabled();
  });
});